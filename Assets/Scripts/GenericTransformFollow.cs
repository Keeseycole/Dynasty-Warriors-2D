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

    public int currentPointIndex = 0;
    private Rigidbody2D rb;
    public bool isMoving = false;

    // --- AI SYSTEM CONNECTIONS ---
    private MonoBehaviour musouAIComponent;

    [Header("Tactical Settings")]
    [Tooltip("ForcedMarch = ignore all threats.\nEngageOnSight = break path if an enemy is detected nearby.")]
    public PathPriority pathPriority = PathPriority.EngageOnSight;

    [Header("Formations Grid Matrix")]
    [Tooltip("The side-to-side and row spacing gap between marching soldiers.")]
    public float platoonSpacing = 1.3f;
    [Tooltip("How many columns wide the marching platoon should be.")]
    public int platoonColumns = 3;

    [Header("Dynamic Proximity Plow Settings")]
    [Tooltip("How close the player needs to be before the general activates his physical snowplow force.")]
    public float playerPlowRange = 8f;
    [Tooltip("The range in front of the unit where it will forcefully push grunts out of its way.")]
    public float plowRadius = 1.5f;
    [Tooltip("How violently standard grunts are physically thrown out of the marching lane.")]
    public float plowPushForce = 12f;

    private Collider2D generalCollider;
    private Transform playerTransform;
    private int armyLayersMask;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        generalCollider = GetComponent<Collider2D>();

        // Automatically search for your custom unit AI components on this exact object
        musouAIComponent = GetComponent<MusouUnit>() as MonoBehaviour;
        if (musouAIComponent == null)
        {
            // Dynamic fallback checking parent layers if you use nested prefab structural folders
            musouAIComponent = GetComponentInParent<MusouUnit>() as MonoBehaviour;
        }

        // Cache your layer mask search variables early
        armyLayersMask = LayerMask.GetMask( "Ally");
    }

    private void Start()
    {
        if (pathPoints.Count > 0)
        {
            StartFollowing();
        }

        FindPlayerTarget();
    }

    /// <summary>
    /// Uses standard Unity tags to locate the player character transform coordinates.
    /// </summary>
    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        // 1. Check your C# script variable first. If the script pathing is off, stop!
        if (!isMoving || pathPoints == null || currentPointIndex >= pathPoints.Count)
        {
            Animator unitAnim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (unitAnim != null) unitAnim.SetBool("isMoving", false);
            return;
        }

        if (playerTransform == null) FindPlayerTarget();

        Vector2 myPos = transform.position;

        // ========================================================================
        // 🟩 THE STRATEGIC COMBAT ENGAGEMENT BRAKE (FULL OFF-SCREEN + DIRECT MEMORY SQUAD FLASH):
        // 1. Scenario A: Unit is close by and has acquired a live combat target.
        // 2. Scenario B: Unit is far away / off-screen (currentTarget is null). 
        //    Runs a wide, lightweight background grid lookup using activeUnits to 
        //    intercept opposing columns out of view!
        // ========================================================================
        if (pathPriority == PathPriority.EngageOnSight)
        {
            MusouUnit baseUnit = GetComponent<MusouUnit>() ?? GetComponentInParent<MusouUnit>();
            if (baseUnit != null)
            {
                bool shouldHaltForCombat = false;

                if (baseUnit.currentTarget != null)
                {
                    shouldHaltForCombat = true;
                }
                else
                {
                    // Swaps team parameters cleanly: Enemies seek AllySide; Allies seek EnemySide
                    MusouUnit.Team rivalTeam = (baseUnit.unitTeam == MusouUnit.Team.EnemySide) ?
                                               MusouUnit.Team.PlayerSide : MusouUnit.Team.EnemySide;

                    if (BattlefieldManager.Instance != null && BattlefieldManager.Instance.activeUnits != null)
                    {
                        var rivalUnitsList = BattlefieldManager.Instance.activeUnits;

                        // Widen our off-screen scanning buffer by 1.5x so they don't miss long-distance lane cross-cuts!
                        float offScreenRadius = baseUnit.detectionRange * 1.5f;
                        float tacticalBrakeRadiusSqr = offScreenRadius * offScreenRadius;

                        for (int i = 0; i < rivalUnitsList.Count; i++)
                        {
                            if (rivalUnitsList[i] != null && rivalUnitsList[i].currentState != EnemyState.Death)
                            {
                                // Safe from team validation bugs: Checks if the unit is actually on the rival team
                                if (rivalUnitsList[i].unitTeam == rivalTeam)
                                {
                                    float distanceSqr = ((Vector2)rivalUnitsList[i].transform.position - myPos).sqrMagnitude;
                                    if (distanceSqr <= tacticalBrakeRadiusSqr)
                                    {
                                        // Assign direct component transform reference and shake them into a Walk state!
                                        baseUnit.currentTarget = rivalUnitsList[i].transform;
                                        baseUnit.ChangeState(EnemyState.Walk);

                                        shouldHaltForCombat = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // If combat is detected anywhere in range, lock down movement physics instantly
                if (shouldHaltForCombat)
                {
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }

                    Animator enemyAnim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
                    if (enemyAnim != null)
                    {
                        enemyAnim.SetBool("isMoving", false);
                    }

                    // ========================================================================
                    // 🟩 THE RE-ENGINEERED PERSISTENT LIST BROADCASTER (FIXED FOR ENEMIES):
                    // Bypasses the dynamic combined activeUnits list entirely! By reading your 
                    // manager's native, unchanging 'playerSideUnits' and 'enemySideUnits' arrays
                    // directly, we guarantee references sync up flawlessly for off-screen enemies!
                    // ========================================================================
                    if (BattlefieldManager.Instance != null)
                    {
                        // 1. Select the specific data column based on this unit's true faction alignment
                        var myFactionTargetList = (baseUnit.unitTeam == MusouUnit.Team.EnemySide) ?
                                                   BattlefieldManager.Instance.enemySideUnits :
                                                   BattlefieldManager.Instance.playerSideUnits;

                        // 2. Identify the master commanding reference object
                        MusouUnit trueCommanderRef = (baseUnit.myLeader != null) ? baseUnit.myLeader : baseUnit;

                        // 3. Loop straight through the native list buffer cleanly with zero scope desyncs!
                        for (int i = 0; i < myFactionTargetList.Count; i++)
                        {
                            MusouUnit platoonCandidate = myFactionTargetList[i];
                            if (platoonCandidate != null && platoonCandidate.currentState != EnemyState.Death)
                            {
                                // Flash the unit if they are the commander, OR if their leader reference matches
                                if (platoonCandidate == trueCommanderRef || platoonCandidate.myLeader == trueCommanderRef)
                                {
                                    Health teammateHealth = platoonCandidate.GetComponent<Health>() ?? platoonCandidate.GetComponentInParent<Health>();
                                    if (teammateHealth != null)
                                    {
                                        teammateHealth.ForceMinimapFlash(); // ◄── FORCES OFF-SCREEN ENEMIES ON LIVE!
                                    }
                                }
                            }
                        }
                    }

                    return; // EXIT EARLY! Stand ground and halt pathing to fight background battles!
                }
            }
        }

        Vector2 targetWaypointPos = pathPoints[currentPointIndex].position;
        Vector2 rawMoveDirection = (targetWaypointPos - myPos).normalized;

        bool playerIsClose = false;
        if (playerTransform != null)
        {
            float distToPlayerSqr = ((Vector2)playerTransform.position - myPos).sqrMagnitude;
            playerIsClose = distToPlayerSqr <= (playerPlowRange * playerPlowRange);
        }

        if (playerIsClose)
        {
            if (generalCollider != null) generalCollider.excludeLayers = 0;
            SnowplowObstacleTroops(rawMoveDirection);
        }
        else
        {
            if (generalCollider != null) generalCollider.excludeLayers = armyLayersMask;
        }

        // Smoothly drive straight down its travel lines if no enemy is blockading us
        if (rb != null)
        {
            rb.linearVelocity = rawMoveDirection * movementSpeed;

            // ========================================================================
            // 🟩 THE ANIMATOR FORCE SYNC (FIXED):
            // We use explicit string queries to set the parameter to true, ensuring
            // it passes directly down to whichever child object holds your animations!
            // ========================================================================
            Animator unitAnim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (unitAnim != null)
            {
                unitAnim.SetBool("isMoving", true); // ◄── FORCIBLY SET ANIMATOR PARAMETER
            }
        }

        float distanceToPointSqr = (targetWaypointPos - myPos).sqrMagnitude;
        if (distanceToPointSqr < arrivalThreshold * arrivalThreshold)
        {
            AdvanceToNextPoint();
        }
    }


    /// <summary>
    /// Forcefully displaces any standard grunt soldiers blockading our travel path,
    /// ensuring this high-value unit never stops or slows down while marching near the player!
    /// </summary>
    private void SnowplowObstacleTroops(Vector2 moveDirection)
    {
        Vector2 myPos = transform.position;

        // Sweep a small circle directly ahead of our moving face frame
        Collider2D[] trappedTroops = Physics2D.OverlapCircleAll(myPos + (moveDirection * 0.4f), plowRadius, armyLayersMask);

        for (int i = 0; i < trappedTroops.Length; i++)
        {
            if (trappedTroops[i].gameObject == gameObject) continue;

            // EXCLUSION CHECK: Never push yourself or high officers/commanders out of the way!
            MusouUnit troopUnit = trappedTroops[i].GetComponent<MusouUnit>() ?? trappedTroops[i].GetComponentInParent<MusouUnit>();
            if (troopUnit != null)
            {
                if (troopUnit.isOfficer || troopUnit.isStageCommander) continue;
            }

            // THE PHYSICAL DISPLACEMENT IMPULSE:
            Rigidbody2D gruntRb = trappedTroops[i].GetComponent<Rigidbody2D>() ?? trappedTroops[i].GetComponentInChildren<Rigidbody2D>();
            if (gruntRb != null && gruntRb.simulated)
            {
                Vector2 gruntPos = trappedTroops[i].transform.position;
                Vector2 lateralShoveDir = Vector2.Perpendicular(moveDirection);

                // Determine whether the grunt sits slightly more to our left or right side to push outward
                float sideDot = Vector2.Dot(gruntPos - myPos, lateralShoveDir);
                if (sideDot < 0) lateralShoveDir = -lateralShoveDir;

                // Deliver physics impulse vector to push grunt out of the direct lane
                gruntRb.linearVelocity = (lateralShoveDir * 0.7f + moveDirection * 0.3f).normalized * plowPushForce;
            }
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
        if (unitIndex < 0) return rawWaypointPosition;

        int row = unitIndex / platoonColumns;
        int col = unitIndex % platoonColumns;

        float centeredColOffset = (col - (platoonColumns - 1) / 2f) * platoonSpacing;
        float rowOffset = -row * platoonSpacing;

        Vector2 myPosition = transform.position;
        Vector2 movementHeading = (rawWaypointPosition - myPosition).normalized;

        Vector2 rightVector = new Vector2(-movementHeading.y, movementHeading.x);

        float worldOffsetX = (centeredColOffset * rightVector.x) + (rowOffset * movementHeading.x);
        float worldOffsetY = (centeredColOffset * rightVector.y) + (rowOffset * movementHeading.y);

        return rawWaypointPosition + new Vector2(worldOffsetX, worldOffsetY);
    }
}