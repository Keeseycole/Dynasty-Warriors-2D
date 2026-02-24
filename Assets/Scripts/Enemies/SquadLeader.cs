using UnityEngine;
using System.Collections.Generic;

public class SquadLeader : MeleeEnemy 
{
  // Initialize it directly here
    public List<Transform> squadMembers = new List<Transform>();
    public float spacing = 2.0f;

    // Add this Awake function to FORCE the list to exist
    void Awake() 
    {
        if (squadMembers == null) 
            squadMembers = new List<Transform>();
    }

    public Vector2 GetSlotPosition(int index)
    {
        // Safety: If spacing is 0, they stack on top of each other
        if (spacing <= 0) spacing = 1.5f;

        float xOffset = (index % 2 == 0 ? 1 : -1) * spacing * ((index / 2) + 1);
        float yOffset = -spacing; 
        return (Vector2)transform.position + new Vector2(xOffset, yOffset);
    }

    void Reset()
    {
        // This clears any "bad data" saved in the Inspector
        squadMembers = new List<Transform>();
        spacing = 2.0f;
    }
}