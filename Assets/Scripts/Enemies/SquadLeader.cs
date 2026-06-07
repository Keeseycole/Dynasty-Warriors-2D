using UnityEngine;
using System.Collections.Generic;

public class SquadLeader : MusouUnit
{
    [Header("Squad Management")]
    public List<MusouUnit> squadMembers = new List<MusouUnit>();
    public float rallyRange = 10f;

    [Header("Combat Settings")]
    public float engageDistance = 8f;
    private bool squadEngaged = false;

    // Stores the unique physical offset from the inspector for each soldier
    private Dictionary<MusouUnit, Vector2> memberOffsets = new Dictionary<MusouUnit, Vector2>();

    public override void Start()
    {
        base.Start();

        // Link all soldiers set up near the leader in the scene view
        LinkSquad();
    }

    private void Update()
    {
        // 1. Fallback player tracking
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null) return;

        // 2. TACTICAL OVERRIDE: March the army toward strategic points before local fights
        if (missionTarget != null && !squadEngaged)
        {
            return;
        }

        // 3. Simple Dynasty Warriors Distance Check
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 4. Trigger the Squad Aggression Rush State
        if (distToPlayer <= engageDistance && !squadEngaged)
        {
            SetSquadMode(true);
        }
        else if (distToPlayer > engageDistance + 5f && squadEngaged)
        {
            SetSquadMode(false);
        }

        // 5. If squad is actively engaged, continually lock down the target reference
        if (squadEngaged)
        {
            currentTarget = playerTransform;
            BroadcastTarget(playerTransform);
        }
    }

    void SetSquadMode(bool attack)
    {
        // Avoid cascading speed adjustments repeatedly over frames
        if (squadEngaged == attack) return;
        squadEngaged = attack;

        foreach (MusouUnit member in squadMembers)
        {
            if (member == null) continue;

            if (attack)
            {
                member.currentTarget = playerTransform;
                member.moveSpeed *= 1.2f; // Charge boost
            }
            else
            {
                member.currentTarget = null;
                member.moveSpeed /= 1.2f; // Back to standard march speed
            }
        }
    }

    // This overrides the virtual/abstract base slot call perfectly
    public override Vector2 GetSlotPosition(int index)
    {
        // Out of bounds safety fallback
        if (index < 0 || index >= squadMembers.Count) return transform.position;

        MusouUnit targetMember = squadMembers[index];

        // If we have an Inspector-saved spot for this unit, use it!
        if (targetMember != null && memberOffsets.ContainsKey(targetMember))
        {
            return (Vector2)transform.position + memberOffsets[targetMember];
        }

        // Fallback: If no dictionary entry exists, keep them at the leader's position
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

    [ContextMenu("Link Nearby Squad")]
    public void LinkSquad()
    {
        squadMembers.Clear();
        memberOffsets.Clear();

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, rallyRange);

        int index = 0;
        foreach (var hit in hitColliders)
        {
            MusouUnit unit = hit.GetComponent<MusouUnit>();

            // Confirm unit is valid, is not the leader itself, and shares the exact same team faction
            if (unit != null && unit != this && unit.unitTeam == this.unitTeam)
            {
                unit.myLeader = this;
                unit.squadIndex = index;
                squadMembers.Add(unit);

                // KEY STEP: Lock down the exact vector difference from where they stand in the Inspector layout
                Vector2 layoutOffset = (Vector2)unit.transform.position - (Vector2)transform.position;
                memberOffsets.Add(unit, layoutOffset);

                index++;
            }
        }
        Debug.Log($"[Squad Master] {squadMembers.Count} guards locked into their layout offsets relative to {gameObject.name}.");
    }

    private void OnDestroy()
    {
        // Clean breakup when commander falls
        foreach (MusouUnit member in squadMembers)
        {
            if (member != null)
            {
                member.myLeader = null;
                member.followsPlayer = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draws cyan wire spheres in scene view mapping out where your layout tells grunts to go
        Gizmos.color = Color.cyan;
        for (int i = 0; i < squadMembers.Count; i++)
        {
            Gizmos.DrawWireSphere(GetSlotPosition(i), 0.25f);
        }
    }

    // Keep empty hooks for legacy systems or animation configurations
    public virtual void SpawnGuards() { }
    public virtual void SpawnGaurds() { }

    public void AssignSquadMission(AIMission newMission, Transform newTarget)
    {
        currentMission = newMission;
        missionTarget = newTarget;
        currentTarget = null;

        foreach (MusouUnit member in squadMembers)
        {
            if (member == null) continue;

            member.currentMission = newMission;
            member.missionTarget = newTarget;
            member.currentTarget = null;

            if (newMission == AIMission.FollowLeader)
            {
                member.missionTarget = null;
            }
        }
    }
}