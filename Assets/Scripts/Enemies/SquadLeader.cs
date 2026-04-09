using UnityEngine;
using System.Collections.Generic;

public enum FormationType { Wedge, Circle, Square, Line }

public class SquadLeader : MusouUnit // Inherit from your unit script
{
   

    [Header("New Formation Settings")]
    public FormationType currentFormation;
    public float formationRotation = 0f; // Degrees (e.g., 0 = Down, 90 = Left)

    [Header("Squad Management")]
    public List<MusouUnit> squadMembers = new List<MusouUnit>();
    public float formationSpacing = 2.0f;
    public float rallyRange = 10f;


    [Header("Formation Settings")]
    [Range(0.5f, 10f)] // Adds a slider to the Inspector
    public float spacing = 2.0f;

    public float engageDistance = 8f; // When the squad breaks formation to fight
    private bool squadEngaged = false;


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

  
        // 1. If we don't have a player reference, find one
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform == null) return;

        // 2. Simple DW3 Distance Check
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 3. Trigger the Squad Rush
        if (distToPlayer <= engageDistance && !squadEngaged)
        {
            SetSquadMode(true);
        }
        else if (distToPlayer > engageDistance + 5f && squadEngaged)
        {
            SetSquadMode(false);
        }

        // 4. If we are fighting, make sure everyone knows who to hit
        if (squadEngaged)
        {
            currentTarget = playerTransform;
            BroadcastTarget(playerTransform);
        }
    }
    void SetSquadMode(bool attack)
    {
        squadEngaged = attack;

        foreach (MusouUnit member in squadMembers)
        {
            if (member == null) continue;

            if (attack)
            {
                member.currentTarget = playerTransform;
                // Optional: Give them a random offset so they don't all stack on one pixel
                member.moveSpeed *= 1.2f;
            }
            else
            {
                // This is how they "go back" to the leader after the player runs away
                member.currentTarget = null;
                member.moveSpeed /= 1.2f;
            }
        }
    }

    // This math creates a V-shape / Staggered Grid
    public override Vector2 GetSlotPosition(int index)
    {
        Vector2 offset = Vector2.zero;

        switch (currentFormation)
        {
            case FormationType.Wedge: // Classic V-shape
                float xW = (index % 2 == 0 ? 1 : -1) * spacing * ((index / 2) + 1);
                float yW = -spacing * ((index / 2) + 1);
                offset = new Vector2(xW, yW);
                break;

            case FormationType.Circle:
                float angle = (360f / 8f) * index * Mathf.Deg2Rad; // Assumes ~8 units
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spacing * 2;
                break;

            case FormationType.Square:
                int columns = 3;
                float xS = (index % columns) * spacing;
                float yS = -(index / columns) * spacing;
                offset = new Vector2(xS, yS);
                break;

            case FormationType.Line:
                offset = new Vector2((index - 4) * spacing, -spacing); // Horizontal line
                break;
        }

        // APPLY STARTING DIRECTION: Rotate the offset based on formationRotation
        return (Vector2)transform.position + (Vector2)(Quaternion.Euler(0, 0, formationRotation) * offset);
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