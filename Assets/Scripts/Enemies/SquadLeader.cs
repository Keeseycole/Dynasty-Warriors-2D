using UnityEngine;
using System.Collections.Generic;

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
    protected int currentWaypointIndex = 0;
    protected bool isWaitingForSquad = false;

    [Tooltip("The parent GameObject that physically holds all your waypoint transforms as children.")]
    public GameObject PathContainer;

    private void Awake()
    {
        // Link references using the manual entries you dragged into the Inspector list
        InitializeSquad();
    }

    public override void Start()
    {
        base.Start();
        engageDistanceSqr = engageDistance * engageDistance;

        CancelInvoke("FindNearestTarget");
    }

    private void FixedUpdate()
    {
        CheckDistance();
    }

    /// <summary>
    /// REVERTED: Uses your manual Inspector list elements instead of scanning child hierarchies!
    /// </summary>
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

            // Link references back to this leader explicitly
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
        if (animator.GetBool("isHit") || currentState == EnemyState.Stagger || isBusy) return;

        // 1. COMBAT TRACKING (Highest Priority)
        if (currentTarget == null) FindNearestTarget();

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

            base.CheckDistance();
            return;
        }

        if (myLeader != null)
        {
            // Trailing sub-leaders follow their master Officer
            Vector2 leaderPos = (myLeader.rb != null) ? myLeader.rb.position : (Vector2)myLeader.transform.position;
            Vector2 myPhysicalPos = rb != null ? rb.position : (Vector2)transform.position;
            float distToMasterSqr = (leaderPos - myPhysicalPos).sqrMagnitude;
            float followSpacingRadius = 2.5f;

            if (distToMasterSqr > followSpacingRadius * followSpacingRadius)
            {
                MoveTowards(leaderPos, false);
                WakeUpSquadFormation();
            }
            else
            {
                if (myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    rb.linearVelocity = myLeader.rb.linearVelocity;
                    animator.SetBool("isMoving", true);
                }
                else StopMoving();
            }
            return; 
        }


        // If the path container object is missing, or explicitly turned OFF in the hierarchy,
        // force the Officer to stand fast and guard his position!
        if (PathContainer == null || !PathContainer.activeInHierarchy)
        {
            missionTarget = null;
            StopMoving();
            WakeUpSquadFormation(); // Keep grunts standing neatly around him
            FindNearestTarget();   // Keep scanning for incoming enemies
            return; // Exit here! Blocks him from walking out on scene load
        }

        // 2. WAYPOINT ASSEMBLING CONTROL (Only runs if the master switch is ON)
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
            else FindNearestTarget();
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

            if (distToLeaderSqr > assembleRadiusSqr) return false;
            if (member.currentState != EnemyState.Idle) return false;
            if (memberRb != null && memberRb.linearVelocity.sqrMagnitude > 0.05f) return false;
        }

        return true;
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

    private void Update()
    {
        if (playerTransform == null) return;

        if (this.unitTeam == Team.PlayerSide || currentMission == AIMission.CaptureBase || currentMission == AIMission.AttackCommander)
        {
            if (squadEngaged) SetSquadMode(false);
            return;
        }

        Vector2 deltaPlayer = (Vector2)playerTransform.position - rb.position;
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

    private void SetSquadMode(bool attack)
    {
        if (squadEngaged == attack) return;
        squadEngaged = attack;

        for (int i = 0; i < squadMembers.Count; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member == null) continue;

            if (attack)
            {
                member.currentTarget = playerTransform;
                member.moveSpeed = member.moveSpeed * 1.2f;
            }
            else
            {
                member.currentTarget = null;
                member.moveSpeed = member.moveSpeed / 1.2f;
            }
        }
    }

    public Vector2 GetSquadPosition(int index)
    {
        if (index < 0 || index >= squadMembers.Count) return rb.position;

        MusouUnit targetMember = squadMembers[index];

        if (targetMember != null && memberOffsets.ContainsKey(targetMember))
        {
            Vector2 localOffset = memberOffsets[targetMember];
            Vector2 leaderPhysicalPos = rb.position;

            Vector2 heading = rb.linearVelocity.sqrMagnitude > 0.1f
                ? rb.linearVelocity.normalized
                : startingDirection.normalized;

            float rightX = heading.y;
            float rightY = -heading.x;

            float rotatedX = localOffset.x * rightX + localOffset.y * heading.x;
            float rotatedY = localOffset.x * rightY + localOffset.y * heading.y;

            return leaderPhysicalPos + new Vector2(rotatedX, rotatedY);
        }

        return rb.position;
    }

    public void BroadcastTarget(Transform target)
    {
        for (int i = 0; i < squadMembers.Count; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member != null && member.currentTarget == null)
            {
                member.currentTarget = target;
            }
        }
    }

    public void AssignSquadMission(AIMission newMission, Transform targetNode)
    {
        currentMission = newMission;
        missionTarget = targetNode;

        for (int i = 0; i < squadMembers.Count; i++)
        {
            MusouUnit member = squadMembers[i];
            if (member == null) continue;

            member.currentMission = newMission;
            member.missionTarget = targetNode;
        }
    }
}