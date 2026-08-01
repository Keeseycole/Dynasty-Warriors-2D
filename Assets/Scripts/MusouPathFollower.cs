using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MusouPathFollower : MonoBehaviour
{
    private MusouUnit unit;
    private Coroutine followCoroutine;
    private float nodeThreshold = 1.5f;

    // --- 🔥 NEW VISUAL INSPECTOR SLOTS ---
    [Header("Active Route Debugger (Read Only)")]
    [Tooltip("The actual path this specific unit is currently processing in real-time.")]
    public List<Transform> pathNodesToFollow = new List<Transform>();

    [Tooltip("The ultimate end objective target for this unit.")]
    public Transform finalObjective;

    private void Awake()
    {
        unit = GetComponent<MusouUnit>();
    }



    // --- PUBLIC ENTRY POINT ---
    public void InitiatePathFollow(List<Transform> nodes, Transform finalObjectiveTarget)
    {
        if (nodes == null || nodes.Count == 0) return;

        pathNodesToFollow = new List<Transform>(nodes);
        finalObjective = finalObjectiveTarget;

        // ========================================================================
        // 🔥 FIXED: UNLOCK AI DIRECTIVES
        // Force your unit to snap out of 'FollowLeader' or 'Idle' and allow pathing!
        // (Ensure 'EnemyState.Move' matches your moving state name from earlier)
        // ========================================================================
        if (unit != null)
        {
            unit.ChangeState(EnemyState.Walk); // Breaks the unit out of the Idle loop block

            // Forcefully change their active AIMission variable so they trace waypoints!
            // If your AI mission enum is slightly different, replace 'CaptureBase' with any generic target mission name.
            unit.currentMission = MusouUnit.AIMission.CaptureBase;
        }

        if (followCoroutine != null) StopCoroutine(followCoroutine);
        followCoroutine = StartCoroutine(FollowPathRoutine());
    }

    private IEnumerator FollowPathRoutine()
    {
        // ========================================================================
        // 🔥 FIXED: THE DATA VALIDATION FLUID BUFFER
        // Stalls loop execution until our path lists are populated with data by the spawners.
        // This stops the coroutine from aborting on frame 1!
        // ========================================================================
        float safetyTimeout = 2.0f;
        while ((pathNodesToFollow == null || pathNodesToFollow.Count == 0) && safetyTimeout > 0f)
        {
            safetyTimeout -= Time.deltaTime;
            yield return null;
        }

        // Safety backup if no path data ever arrives
        if (pathNodesToFollow == null || pathNodesToFollow.Count == 0)
        {
            Debug.LogError($"[PATH ERROR] {gameObject.name} sat waiting but never received any path nodes! Aborting.");
            yield break;
        }

        Debug.Log($"[PATH START] {gameObject.name} successfully validated path. Beginning march across {pathNodesToFollow.Count} nodes!");

        // Loop through our public path nodes list sequentially
        for (int i = 0; i < pathNodesToFollow.Count; i++)
        {
            Transform currentNode = pathNodesToFollow[i];
            if (unit == null || !unit.enabled || currentNode == null) yield break;

            // Inform your existing MusouUnit target slot where it needs to head
            unit.missionTarget = currentNode;
            float checkTimer = 0f;

            while (unit != null && currentNode != null)
            {
                // If they aren't currently locked in a skirmish duel with a player/enemy, push them forward
                if (unit.currentTarget == null)
                {
                    unit.MoveTowards(currentNode.position, false);
                }

                // Low-overhead proximity check (5 times a second)
                checkTimer += Time.deltaTime;
                if (checkTimer >= 0.2f)
                {
                    checkTimer = 0f;
                    if (Vector2.Distance(transform.position, currentNode.position) <= nodeThreshold)
                    {
                        Debug.Log($"[PATH PROGRESS] {gameObject.name} reached Node index {i}: {currentNode.name}. Advancing!");
                        break; // Breakthrough this local while loop to advance the main index counter
                    }
                }

                yield return null;
            }
        }

        // Final handoff to charge your core objective gate location
        if (unit != null)
        {
            unit.missionTarget = finalObjective;
            Debug.Log($"[PATH COMPLETE] {gameObject.name} has finished its node sequence track and is charging final target: {finalObjective.name}!");
        }
    }

    private void OnDisable()
    {
        if (followCoroutine != null) StopCoroutine(followCoroutine);
    }


    private void OnDrawGizmosSelected()
    {
        // Only draw lines if we have active nodes assigned and we select this unit in the hierarchy
        if (pathNodesToFollow == null || pathNodesToFollow.Count == 0) return;

        Gizmos.color = Color.cyan; // Bright neon blue lines for active pathways

        // 1. Draw a line from the unit's current position to its very next node target
        if (pathNodesToFollow[0] != null)
        {
            Gizmos.DrawLine(transform.position, pathNodesToFollow[0].position);
            Gizmos.DrawWireSphere(pathNodesToFollow[0].position, 0.4f);
        }

        // 2. Chaining line links connecting node to node down the sequence list length
        for (int i = 0; i < pathNodesToFollow.Count - 1; i++)
        {
            if (pathNodesToFollow[i] != null && pathNodesToFollow[i + 1] != null)
            {
                Gizmos.DrawLine(pathNodesToFollow[i].position, pathNodesToFollow[i + 1].position);
                Gizmos.DrawWireSphere(pathNodesToFollow[i + 1].position, 0.4f);
            }
        }

        // 3. Draw a final red line connecting the last path node straight to the end objective core gate
        if (pathNodesToFollow[pathNodesToFollow.Count - 1] != null && finalObjective != null)
        {
            Gizmos.color = Color.red; // Red for the ultimate final objective target point!
            Gizmos.DrawLine(pathNodesToFollow[pathNodesToFollow.Count - 1].position, finalObjective.position);
            Gizmos.DrawFrustum(finalObjective.position, 1f, 0.5f, 0f, 1f);
        }
    }
}