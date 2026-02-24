using UnityEngine;
using System.Collections.Generic;

public class SquadFollower : MonoBehaviour
{
    public GameObject leader; // Drag the Leader here in Inspector
    public int stepsBehind = 20; // Increase this to make them stay further back
    public float moveSpeed = 3f;

    private Queue<Vector3> positionHistory = new Queue<Vector3>();

    public Vector3 formationOffset; // Set this in the Inspector (e.g., x: -1.5, y: -1)

    void FixedUpdate()
    {
        if (leader == null) return;

        // 1. Record the position PLUS the offset
        Vector3 spotWithOffset = leader.transform.position + formationOffset;
        positionHistory.Enqueue(spotWithOffset);

        if (positionHistory.Count > stepsBehind)
        {
            Vector3 targetPos = positionHistory.Dequeue();
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.fixedDeltaTime);
        }
    }
}
    

