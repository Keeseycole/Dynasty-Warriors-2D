using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GenericTransformFollower : MonoBehaviour
{
    
    public enum PathPriority { ForcedMarch, EngageOnSight }

    [Header("Movement Settings")]
    public float movementSpeed = 3f;
    public float arrivalThreshold = 0.5f;
    public bool loopPath = false;

    [Header("Visual Blueprint Route")]
    public List<Transform> pathPoints = new List<Transform>();

    private int currentPointIndex = 0;
    private Rigidbody2D rb;
    private bool isMoving = false;

    // --- 🔥 NEW AI SYSTEM CONNECTIONS ---
    private MonoBehaviour musouAIComponent;

    [Header("Tactical Settings")]
    [Tooltip("ForcedMarch = ignore all threats.\nEngageOnSight = break path if an enemy is detected nearby.")]
    public PathPriority pathPriority = PathPriority.EngageOnSight;

    [Header("Formations Grid Matrix")]
    [Tooltip("The side-to-side and row spacing gap between marching soldiers.")]
    public float platoonSpacing = 1.3f;
    [Tooltip("How many columns wide the marching platoon should be.")]
    public int platoonColumns = 3;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // Automatically search for your custom unit AI components on this exact object
        musouAIComponent = GetComponent<MusouUnit>() as MonoBehaviour;
        if (musouAIComponent == null)
        {
            // Dynamic fallback checking parent layers if you use nested prefab structural folders
            musouAIComponent = GetComponentInParent<MusouUnit>() as MonoBehaviour;
        }
    }

    private void Start()
    {
        if (pathPoints.Count > 0)
        {
            StartFollowing();
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving || pathPoints.Count == 0) return;

        Transform currentTargetPoint = pathPoints[currentPointIndex];
        if (currentTargetPoint == null) return;

        MusouUnit musouUnit = musouAIComponent as MusouUnit;

        // ========================================================================
        // 🔥 THE ENGAGE-ON-SIGHT INTERCEPT GATE:
        // If we are looking for a fight and the leader's scanning loop successfully
        // locks onto an enemy target, immediately step down and pause the path march!
        // ========================================================================
        if (pathPriority == PathPriority.EngageOnSight && musouUnit != null && musouUnit.currentTarget != null)
        {
            // Release the busy flag so SquadLeader.CheckDistance() and BrainTick() can run
            musouUnit.isBusy = false;
            return; // Exit FixedUpdate early, giving full control to the combat AI!
        }

        // ========================================================================
        // NO ENEMIES DETECTED: Resume Safe Macro-Path Travel
        // ========================================================================
        if (musouUnit != null)
        {
            musouUnit.isBusy = true;
            musouUnit.currentState = EnemyState.Walk;
        }

        Vector2 targetPosition = currentTargetPoint.position;
        Vector2 currentPosition = rb.position;
        Vector2 moveDirection = (targetPosition - currentPosition).normalized;

        float distanceToTarget = Vector2.Distance(currentPosition, targetPosition);
        if (distanceToTarget <= arrivalThreshold)
        {
            AdvanceToNextPoint();
            return;
        }

        rb.linearVelocity = moveDirection * movementSpeed;

        // Drive the running animations smoothly along the vector path paths
        if (musouUnit != null)
        {
            // If using Option A (Direct Animator updates):
            if (musouUnit.animator != null)
            {
                musouUnit.animator.SetBool("isMoving", true);
                musouUnit.animator.SetBool("isStrafing", false);
                musouUnit.animator.SetFloat("moveX", moveDirection.x);
                musouUnit.animator.SetFloat("moveY", moveDirection.y);
            }
            musouUnit.ChangeAnim(moveDirection);

            // NOTE: If you went with Option B, you can just call:
            // musouUnit.UpdatePathingAnimation(moveDirection);
        }
    }

    private void AdvanceToNextPoint()
    {
        currentPointIndex++;

        if (currentPointIndex >= pathPoints.Count)
        {
            if (loopPath)
            {
                currentPointIndex = 0;
            }
            else
            {
                StopFollowing();
            }
        }
    }

    public void StartFollowing()
    {
        isMoving = true;
    }

    public void StopFollowing()
    {
        isMoving = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Release the AI lock so the Squad Leader wakes back up completely
        MusouUnit musouUnit = musouAIComponent as MusouUnit;
        if (musouUnit != null)
        {
            musouUnit.isBusy = false;
            musouUnit.StopMoving(); // Cleanly resets animator values back to idle safely
        }
    }

    public void AssignNewPath(List<Transform> newPoints, bool loop)
    {
        pathPoints = new List<Transform>(newPoints);
        loopPath = loop;
        currentPointIndex = 0;
        StartFollowing();
    }

    private void OnDrawGizmosSelected()
    {
        if (pathPoints == null || pathPoints.Count == 0) return;

        Gizmos.color = Color.green;

        if (pathPoints[0] != null)
        {
            Gizmos.DrawLine(transform.position, pathPoints[0].position);
            Gizmos.DrawWireSphere(pathPoints[0].position, 0.3f);
        }

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                Gizmos.DrawWireSphere(pathPoints[i + 1].position, 0.3f);
            }
        }
    }

    public Vector2 CalculateFormedWaypointTarget(Vector2 rawWaypointPosition, int unitIndex)
    {
        // 1. If this unit has no index or is a standalone leader, go straight to the center pivot
        if (unitIndex < 0) return rawWaypointPosition;

        // 2. Calculate its exact Row and Column index based on its position in the group array list
        int row = unitIndex / platoonColumns;
        int col = unitIndex % platoonColumns;

        // 3. Center the columns so the platoon is distributed evenly left and right
        float centeredColOffset = (col - (platoonColumns - 1) / 2f) * platoonSpacing;

        // 4. Push subsequent rows backward behind the front line
        float rowOffset = -row * platoonSpacing;

        // 5. Apply the directional heading so the formation rotates dynamically as they turn corners!
        Vector2 myPosition = transform.position;
        Vector2 movementHeading = (rawWaypointPosition - myPosition).normalized;

        // Generate a true perpendicular 90-degree right vector for column spacing splits
        Vector2 rightVector = new Vector2(-movementHeading.y, movementHeading.x);

        // Transform the grid offsets into the world space along their current travel path!
        float worldOffsetX = (centeredColOffset * rightVector.x) + (rowOffset * movementHeading.x);
        float worldOffsetY = (centeredColOffset * rightVector.y) + (rowOffset * movementHeading.y);

        return rawWaypointPosition + new Vector2(worldOffsetX, worldOffsetY);
    }
}
