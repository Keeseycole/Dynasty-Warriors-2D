using UnityEngine;
using System.Collections.Generic;

public class SquadLeader : MusouUnit
{
    [Header("Squad Management")]
    public List<MusouUnit> squadMembers = new List<MusouUnit>();
   

    [Header("Combat Settings")]
    public float engageDistance = 8f;
    private bool squadEngaged = false;

    private Dictionary<MusouUnit, Vector2> memberOffsets = new Dictionary<MusouUnit, Vector2>();

    public override void Start()
    {
        // 1. Run base startup
        base.Start();

        // 2. STOP the leader from running the automatic grunt sensor loop.
        // Leaders rely on player proximity and strategic missions, not grunt duels!
        CancelInvoke("FindNearestTarget");

    }

    /// <summary>
    /// 🔥 FIXED STRATEGIC OVERRIDE: 
    /// Drives the leader using your working Rigidbody2D system instead of breaking physics!
    /// </summary>
    public override void CheckDistance()
    {
        // If busy or staggered by hit-lag, freeze strategic processing instantly
        if (animator.GetBool("isHit")) return;

        // 1. STRATEGIC MISSION MARCH: If moving the army, ignore local micro-combat scans
        if (missionTarget != null && !squadEngaged)
        {
            float distToMission = Vector2.Distance(transform.position, missionTarget.position);

            if (distToMission > 2.0f)
            {
                // FIXED: Uses your working parent physics movement method!
                // False means it is marching strategies, not chasing a combat target.
                MoveTowards(missionTarget.position, false);
            }
            else
            {
                // Do NOT set missionTarget to null here! Let BattleEventManager advance the path.
                StopMoving();
            }
            return;
        }

        // 2. ENGAGED COMBAT: If the leader breaks formation to fight, run standard combat priorities
        if (squadEngaged && playerTransform != null)
        {
            currentTarget = playerTransform;
            base.CheckDistance(); // Safely runs your working combat/attack/strafe priorities!
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null) return;

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Dynamic State Switching Gate
        if (distToPlayer <= engageDistance && !squadEngaged)
        {
            SetSquadMode(true);
        }
        else if (distToPlayer > engageDistance + 5f && squadEngaged)
        {
            SetSquadMode(false);
        }

        // Continually feed player tracking down to active guards
        if (squadEngaged)
        {
            BroadcastTarget(playerTransform);
        }
    }

    void SetSquadMode(bool attack)
    {
        if (squadEngaged == attack) return;
        squadEngaged = attack;

        foreach (MusouUnit member in squadMembers)
        {
            if (member == null) continue;

            if (attack)
            {
                member.currentTarget = playerTransform;
                member.moveSpeed *= 1.2f; // Basara charge speed boost
            }
            else
            {
                member.currentTarget = null;
                member.moveSpeed /= 1.2f; // Return to standard marching speed
            }
        }
    }

    public override Vector2 GetSlotPosition(int index)
    {
        if (index < 0 || index >= squadMembers.Count) return transform.position;

        MusouUnit targetMember = squadMembers[index];

        if (targetMember != null && memberOffsets.ContainsKey(targetMember))
        {
            return (Vector2)transform.position + memberOffsets[targetMember];
        }

        return transform.position;
    }

    public void BroadcastTarget(Transform target)
    {
        foreach (MusouUnit member in squadMembers)
        {
            if (member != null && member.currentTarget == null)
            {
                member.currentTarget = target;
            }
        }
    }

    public void AssignSquadMission(AIMission newMission, Transform newTarget)
    {
        currentMission = newMission;
        missionTarget = newTarget;
        currentTarget = null; // Clear combat target to force strategic redirection

        // Cascade the new strategic mission rules down to every living squad member
        foreach (MusouUnit member in squadMembers)
        {
            if (member == null) continue;

            member.currentMission = newMission;
            member.missionTarget = newTarget;
            member.currentTarget = null; // Break out of old localized grunt fights

            // If the new mission is to follow the leader again, reset their state
            if (newMission == AIMission.FollowLeader)
            {
                member.missionTarget = null;
            }
        }

        Debug.Log($"[Squad System] Strategic Mission '{newMission}' dispatched to {gameObject.name}'s division.");
    }
}