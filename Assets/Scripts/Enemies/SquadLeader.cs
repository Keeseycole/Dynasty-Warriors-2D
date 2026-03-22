using UnityEngine;
using System.Collections.Generic;

public class SquadLeader : MusouUnit // Inherit from your unit script
{
    [Header("Squad Management")]
    public List<MusouUnit> squadMembers = new List<MusouUnit>();
    public float formationSpacing = 2.0f;
    public float rallyRange = 10f;


    [Header("Formation Settings")]
    [Range(0.5f, 10f)] // Adds a slider to the Inspector
    public float spacing = 2.0f;



    private void Start()
    {
        base.Start();
         // First, link everyone nearby
            LinkSquad();

            // Then, instantly move them to their spots so they don't have to walk there
            for (int i = 0; i < squadMembers.Count; i++)
            {
                if (squadMembers[i] != null)
                {
                    squadMembers[i].transform.position = GetSlotPosition(i);
                }
            }
     }

    private void Update()
    {
        // If the Leader finds a target, tell the whole squad to attack it!
        if (currentTarget != null)
        {
            BroadcastTarget(currentTarget);
        }

    }

    // This math creates a V-shape / Staggered Grid
    public Vector2 GetSlotPosition(int index)
    {
        // Alternating Left/Right math
        float xOffset = (index % 2 == 0 ? 1 : -1) * spacing * ((index / 2) + 1);
        float yOffset = -spacing * ((index / 2) + 1);

        return (Vector2)transform.position + new Vector2(xOffset, yOffset);
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

    [ContextMenu("Gather Nearby Units")]
    public void GatherSquad()
    {
        squadMembers.Clear();
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, rallyRange);

        int index = 0;
        foreach (var col in nearby)
        {
            MusouUnit unit = col.GetComponent<MusouUnit>();
            // Don't add yourself, and only add allies
            if (unit != null && unit != this && unit.unitTeam == this.unitTeam)
            {
                unit.myLeader = this;
                unit.squadIndex = index;
                squadMembers.Add(unit);
                index++;
            }
        }
    }
    void OnDestroy()
    {
        foreach (MusouUnit member in squadMembers)
        {
            if (member != null)
            {
                member.myLeader = null; // They are now "Ronin" and act independently
                member.followsPlayer = true; // Or have them retreat!
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 8; i++) // Preview 8 slots
        {
            Gizmos.DrawWireSphere(GetSlotPosition(i), 0.5f);
        }
    }

    // Add this inside SquadLeader.cs
    [ContextMenu("Link Nearby Squad")]
    public void LinkSquad()
    {
        squadMembers.Clear();
        // Find all MusouUnits in a 10-unit radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 10f);

        int index = 0;
        foreach (var hit in hitColliders)
        {
            MusouUnit unit = hit.GetComponent<MusouUnit>();

            // Don't add yourself (the leader) and only add allies
            if (unit != null && unit != this && unit.unitTeam == this.unitTeam)
            {
                unit.myLeader = this;
                unit.squadIndex = index;
                squadMembers.Add(unit);
                index++;
            }
        }
        Debug.Log($"Squad Linked: {squadMembers.Count} members assigned.");
    }

   

 
}