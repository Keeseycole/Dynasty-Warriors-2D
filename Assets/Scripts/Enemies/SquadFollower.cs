using UnityEngine;
using System.Collections.Generic;

public class SquadFollower : MonoBehaviour
{
    public GameObject leader; // Drag the Leader here in Inspector
    public int stepsBehind = 20; // Increase this to make them stay further back
    public float moveSpeed = 3f;

    private Vector3 lastRecordedPos;

    private Queue<Vector3> positionHistory = new Queue<Vector3>();

    public Vector3 formationOffset; // Set this in the Inspector (e.g., x: -1.5, y: -1)

    void FixedUpdate()
    {


        if (leader == null) return;

        // 1. Only record a new "breadcrumb" if the leader has moved enough
        // This prevents the follower from "jittering" or moving when the leader is idle.
        Vector3 currentLeaderPos = leader.transform.position + formationOffset;

        // We only Enqueue if the leader moved more than, say, 0.1 units from the last recorded spot
        if (positionHistory.Count == 0 || Vector3.Distance(currentLeaderPos, lastRecordedPos) > 0.1f)
        {
            positionHistory.Enqueue(currentLeaderPos);
            lastRecordedPos = currentLeaderPos;
        }

        // 2. Only move if we have enough history AND are far enough away
        float distanceToLeader = Vector3.Distance(transform.position, leader.transform.position);

        if (positionHistory.Count > stepsBehind && distanceToLeader > 1.5f) // Adjust 1.5f for your "stopping distance"
        {
            Vector3 targetPos = positionHistory.Dequeue();
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.fixedDeltaTime);
        }
    }


}
    

