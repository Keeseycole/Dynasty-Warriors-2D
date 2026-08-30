using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class SquadLeader : MusouUnit
{
    [Header("Squad Management (Manual Assignment)")]
    [Tooltip("Manually drag and drop your grunts or squad leaders into this list array!")]
    public List<MusouUnit> squadMembers = new List<MusouUnit>();

    [Tooltip("The spacing grid gap between grunts in rows")]
    public float spacing = 1.2f;

    [Header("Combat Settings")]
    public float engageDistance = 8f;
    private bool squadEngaged = false;
    private float engageDistanceSqr;

    private Dictionary<MusouUnit, Vector2> memberOffsets = new Dictionary<MusouUnit, Vector2>();

    [Header("Tactical Waypoint Pathing Queue")]
    public List<Transform> pathWaypoints = new List<Transform>();
    public int currentWaypointIndex = 0;
    protected bool isWaitingForSquad = false;

    [Tooltip("The parent GameObject that physically holds all your waypoint transforms as children.")]
    public GameObject PathContainer;

    private void Awake()
    {
        InitializeSquad();
    }

    public override void Start()
    {
        base.Start();
        engageDistanceSqr = engageDistance * engageDistance;

        // ========================================================================
        // 🔥 THE COMBAT ACTUATION RESURRECTION:
        // Do NOT cancel the baseline target acquisition routine! Leaders must actively 
        // run target scanning cycles so they can populate currentTarget and fight.
        // ========================================================================
        InvokeRepeating("FindNearestTarget", 0.25f, 0.5f);
    }

    private void FixedUpdate()
    {

        CheckDistance();
    }

    private void Update()
    {
        // 🔥 THE COMPREHENSIVE UPDATE GUEST GATE:
        // If the generic path follower has claimed authority over this unit,
        // exit Update immediately! This stops any faction or mission logic 
        // from overriding animator parameters or forcing idle states.
        if (isBusy) return;

        if (playerTransform == null) return;

        if (this.unitTeam == Team.PlayerSide || currentMission == AIMission.CaptureBase || currentMission == AIMission.AttackCommander)
        {
            if (squadEngaged) SetSquadMode(false);
            return;
        }

        Vector2 leaderPos = (rb != null) ? rb.position : (Vector2)transform.position;
        Vector2 deltaPlayer = (Vector2)playerTransform.position - leaderPos;
        float distToPlayerSqr = deltaPlayer.sqrMagnitude;

        if (distToPlayerSqr <= engageDistanceSqr && !squadEngaged)
        {
            SetSquadMode(true);
        }
        else if (distToPlayerSqr > (engageDistance + 4f) * (engageDistance + 4f) && squadEngaged)
        {
            SetSquadMode(false);
        }

        if (squadEngaged)
        {
            BroadcastTarget(playerTransform);
        }
    }

    public void InitializeSquad()
    {
        memberOffsets.Clear();

        int totalMembers = squadMembers.Count;
        if (totalMembers == 0) return;

        int maxColumns = 3;

        for (int i = 0; i < totalMembers; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member == null) continue;

            member.myLeader = this;
            member.squadIndex = i;

            int currentRow = i / maxColumns;
            int totalRows = Mathf.CeilToInt((float)totalMembers / maxColumns);

            int unitsInThisRow = maxColumns;
            if (currentRow == totalRows - 1)
            {
                int remainder = totalMembers % maxColumns;
                if (remainder != 0) unitsInThisRow = remainder;
            }

            int currentCol = i % maxColumns;

            float xOffset = (currentCol - (unitsInThisRow - 1) / 2f) * spacing;
            float yOffset = -(currentRow + 1) * spacing;

            memberOffsets[member] = new Vector2(xOffset, yOffset);
        }
    }

    public override void CheckDistance()
    {
        // 🔥 THE COMPLETE INTERCEPT INTERLOCK:
        if (animator == null || animator.GetBool("isHit") || currentState == EnemyState.Stagger || isBusy) return;

        // ========================================================================
        // 🟩 THE MASTER PATHING GUARD (FIXED SYSTEM INTEGRATION):
        // If our companion GenericTransformFollower script is actively driving this unit 
        // toward a map waypoint node, force 'isMoving' to stay true and exit early!
        // This completely kills the frame-by-frame fallback resets that were locking you in Idle.
        // ========================================================================
        var pathFollower = GetComponent<GenericTransformFollower>() ?? GetComponentInParent<GenericTransformFollower>();
        if (pathFollower != null && pathFollower.enabled && pathFollower.isMoving && currentTarget == null)
        {
            animator.SetBool("isMoving", true);

            // Sync directional blend tree arrows seamlessly on the march
            Vector2 myPos = transform.position;
            if (pathFollower.pathPoints != null && pathFollower.currentPointIndex < pathFollower.pathPoints.Count)
            {
                Vector2 nextHeading = ((Vector2)pathFollower.pathPoints[pathFollower.currentPointIndex].position - myPos).normalized;
                animator.SetFloat("moveX", nextHeading.x);
                animator.SetFloat("moveY", nextHeading.y);
            }
            return; // ◄── EXIT SAFELY! Let the pathfollower handle the movement frames uninterrupted.
        }

        // 1. COMBAT TRACKING
        if (currentTarget != null)
        {
            Health targetHealth = currentTarget.GetComponent<Health>();
            PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
            bool isDead = (targetHealth != null && targetHealth.currentHealth <= 0) ||
                          (playerHealth != null && playerHealth.currentHealth <= 0);

            if (isDead)
            {
                currentTarget = null;
                StopMoving();
                return;
            }

            if (myLeader != null && combatOffset != Vector2.zero)
            {
                Vector2 targetPhysicalPos = currentTarget.position;
                Vector2 myPhysicalPos = rb != null ? rb.position : (Vector2)transform.position;

                // Base destination from our circular flanking ring matrix
                Vector2 baseCombatDestination = targetPhysicalPos + combatOffset;

                // ========================================================================
                // 🔥 THE LOCAL NEIGHBOR SEPARATION RADAR:
                // Sweeps a small local ring to find nearby troops. If someone is crowding 
                // our personal space, calculate a gentle push vector to push us outward!
                // ========================================================================
                Vector2 separationForce = Vector2.zero;
                float personalSpaceRadius = 1.0f; // ◄── Adjust this to make units stand farther apart!
                int neighborCount = 0;

                // Fast circle overlap sweep to find neighboring bodies
                Collider2D[] closeColliders = Physics2D.OverlapCircleAll(myPhysicalPos, personalSpaceRadius);
                for (int i = 0; i < closeColliders.Length; i++)
                {
                    // Ensure we are only calculating pushback against OTHER units, not ourselves or walls
                    if (closeColliders[i].gameObject != gameObject && closeColliders[i].GetComponent<MusouUnit>() != null)
                    {
                        Vector2 neighborPos = closeColliders[i].transform.position;
                        Vector2 pushDir = myPhysicalPos - neighborPos;
                        float distance = pushDir.magnitude;

                        if (distance > 0.001f)
                        {
                            // The closer they are, the harder they push away to establish padding
                            separationForce += (pushDir.normalized / distance);
                            neighborCount++;
                        }
                    }
                }

                // If crowded, blend our separation push forces directly into our target direction vector
                if (neighborCount > 0)
                {
                    baseCombatDestination += separationForce.normalized * 1.5f;
                }

                // Move to our beautifully adjusted, crowd-free combat position coordinate
                float distToTargetCoordinateSqr = (baseCombatDestination - myPhysicalPos).sqrMagnitude;
                float arrivalPadding = 0.2f;

                if (distToTargetCoordinateSqr > arrivalPadding * arrivalPadding)
                {
                    MoveTowards(baseCombatDestination, false);
                    animator.SetBool("isMoving", true);
                }
                else
                {
                    if (myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude > 0.1f)
                    {
                        rb.linearVelocity = myLeader.rb.linearVelocity;
                        animator.SetBool("isMoving", true);
                    }
                    else
                    {
                        // Stop moving but preserve ground stabilization to eliminate jitter
                        rb.linearVelocity = Vector2.zero;
                        animator.SetBool("isMoving", false);
                    }
                }
                return;
            }

            base.CheckDistance();
            return;
        }

        // ========================================================================
        // 🟩 ANTI-CORNER STICKING GRUNT PLATOON ROUTING
        // Uses a fast local raycast check. If a wall tile blocks a grunt's assigned 
        // formation slot, it temporarily drops into a single-file path line behind 
        // the leader to cleanly dodge corners without getting stuck!
        // ========================================================================
        if (myLeader != null)
        {
            Vector2 myPhysicalPos = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 leaderPos = (myLeader.rb != null) ? myLeader.rb.position : (Vector2)myLeader.transform.position;

            SquadLeader masterLeader = myLeader as SquadLeader;
            if (masterLeader != null)
            {
                // 1. Calculate our ideal structured formation target position
                Vector2 idealTargetSlot = masterLeader.GetSquadPosition(squadIndex);
                Vector2 trackingDestination = idealTargetSlot;

                // 2. 🔥 THE WALL EVASION SHIELD:
                Vector2 dirToSlot = (idealTargetSlot - myPhysicalPos);
                float distToSlot = dirToSlot.magnitude;

                if (distToSlot > 0.01f)
                {
                    int wallLayerMask = LayerMask.GetMask("Default", "Obstacles");
                    RaycastHit2D wallCheck = Physics2D.Raycast(myPhysicalPos, dirToSlot.normalized, distToSlot, wallLayerMask);

                    if (wallCheck.collider != null)
                    {
                        // A corner tile blocks our slot! Override our targets and force this grunt
                        // to march straight down the leader's safe, open center path vector instead!
                        trackingDestination = leaderPos;
                    }
                }

                // 3. Process movement vectors smoothly using our dynamic fallback destination
                float distToTargetCoordinateSqr = (trackingDestination - myPhysicalPos).sqrMagnitude;
                float arrivalPadding = 0.2f;

                if (distToTargetCoordinateSqr > arrivalPadding * arrivalPadding)
                {
                    MoveTowards(trackingDestination, false);
                    WakeUpSquadFormation();
                }
                else
                {
                    if (myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude > 0.1f)
                    {
                        rb.linearVelocity = myLeader.rb.linearVelocity;
                        animator.SetBool("isMoving", true);
                    }
                    else
                    {
                        StopMoving();
                    }
                }
            }
            return;
        }

        if (PathContainer == null || !PathContainer.activeInHierarchy)
        {
            missionTarget = null;
            StopMoving();
            WakeUpSquadFormation();
            return;
        }

        // 2. WAYPOINT ASSEMBLING CONTROL
        if (isWaitingForSquad)
        {
            StopMoving();

            if (IsEntireSquadAssembledAndIdle())
            {
                isWaitingForSquad = false;
                currentWaypointIndex++;
                missionTarget = null;

                if (currentWaypointIndex >= pathWaypoints.Count)
                {
                    currentWaypointIndex = pathWaypoints.Count;
                }
            }
            return;
        }

        if (missionTarget == null && pathWaypoints.Count > 0 && currentWaypointIndex < pathWaypoints.Count)
        {
            missionTarget = pathWaypoints[currentWaypointIndex];
        }

        if (missionTarget != null && !squadEngaged)
        {
            Vector2 leaderPhysicalPos = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 deltaMission = (Vector2)missionTarget.position - leaderPhysicalPos;
            float distToMissionSqr = deltaMission.sqrMagnitude;
            float targetBuffer = 1.5f;

            if (distToMissionSqr > targetBuffer * targetBuffer)
            {
                MoveTowards(missionTarget.position, false);
                WakeUpSquadFormation();
            }
            else
            {
                StopMoving();
                isWaitingForSquad = true;
            }
            return;
        }

        StopMoving();
    }
    // ========================================================================
    // 🟩 TACTICAL POSITION MATRIX ENFORCER (FIXED):
    // Properly transforms grid coordinates using the leader's heading, 
    // keeping grunt offsets locked in place even when stopped!
    // ========================================================================
    public Vector2 GetSquadPosition(int index)
    {
        Vector2 leaderPhysicalPos = (rb != null) ? rb.position : (Vector2)transform.position;

        if (index < 0 || index >= squadMembers.Count) return leaderPhysicalPos;

        MusouUnit targetMember = squadMembers[index];

        if (targetMember != null && memberOffsets.ContainsKey(targetMember))
        {
            Vector2 localOffset = memberOffsets[targetMember];

            // Extract heading direction safely from velocity or fallback starting direction
            Vector2 heading = (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
                ? rb.linearVelocity.normalized
                : startingDirection.normalized;

            // Generate a true perpendicular right-hand vector for row layout splits
            Vector2 right = new Vector2(-heading.y, heading.x);

            // 🔥 MATRIX TRANSFORM: Projects offsets along the true forward/right axes!
            float worldX = (localOffset.x * right.x) + (localOffset.y * heading.x);
            float worldY = (localOffset.x * right.y) + (localOffset.y * heading.y);

            return leaderPhysicalPos + new Vector2(worldX, worldY);
        }

        return leaderPhysicalPos;
    }

    private void WakeUpSquadFormation()
    {
        for (int i = 0; i < squadMembers.Count; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member != null && member.currentMission != AIMission.FollowLeader)
            {
                member.currentMission = AIMission.FollowLeader;
            }
        }
    }

    private void SetSquadMode(bool attack)
    {
        if (squadEngaged == attack) return;
        squadEngaged = attack;

        int totalMembers = squadMembers.Count;

        for (int i = 0; i < totalMembers; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member == null) continue;

            if (attack)
            {
                member.currentTarget = playerTransform;
                if (member.moveSpeed == moveSpeed) member.moveSpeed *= 1.2f;

                // ========================================================================
                // 🔥 THE COMBAT SPACING LAYER MATRIX:
                // Distributes grunts evenly in a radial circle surrounding the player!
                // This keeps them spread out perfectly instead of collapsing into a pile.
                // ========================================================================
                float angle = (i * (360f / Mathf.Max(1, totalMembers))) * Mathf.Deg2Rad;

                // Establish a comfortable combat ring radius (e.g., 2.5 world units out)
                float combatRingRadius = 2.5f;

                // Save a custom offset vector inside your grunt script properties
                member.combatOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * combatRingRadius;
            }
            else
            {
                member.currentTarget = null;
                if (member.moveSpeed != moveSpeed) member.moveSpeed /= 1.2f;
                member.combatOffset = Vector2.zero;
            }
        }
    }

    protected virtual bool IsEntireSquadAssembledAndIdle()
    {
        Vector2 leaderPhysicalPos = rb != null ? rb.position : (Vector2)transform.position;
        float assembleRadiusSqr = 3.5f * 3.5f;

        for (int i = 0; i < squadMembers.Count; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member == null) continue;

            Rigidbody2D memberRb = member.rb != null ? member.rb : member.GetComponentInChildren<Rigidbody2D>();
            Vector2 gruntPhysicalPos = memberRb != null ? memberRb.position : (Vector2)member.transform.position;

            float distToLeaderSqr = (leaderPhysicalPos - gruntPhysicalPos).sqrMagnitude;

            // Strict checklist controls: hold waypoints if any grunt is still traveling
            if (distToLeaderSqr > assembleRadiusSqr) return false;
            if (member.currentState != EnemyState.Idle) return false;
            if (memberRb != null && memberRb.linearVelocity.sqrMagnitude > 0.05f) return false;
        }

        return true;
    }
    public void BroadcastTarget(Transform target) { for (int i = 0; i < squadMembers.Count; i++) { MusouUnit member = squadMembers[i]; if (member != null && member.currentTarget == null) { member.currentTarget = target; } } }
    public void AssignSquadMission(AIMission newMission, Transform targetNode)
    {
        currentMission = newMission; missionTarget = targetNode; 

        for (int i = 0; i < squadMembers.Count; i++)
        {
            MusouUnit member = squadMembers[i]; if (member == null) continue;
            member.currentMission = newMission; member.missionTarget = targetNode;
        }
    }
}