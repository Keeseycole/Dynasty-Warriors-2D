using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MusouUnit))]
public class MusouPathFollower : MonoBehaviour
{
    private MusouUnit unit;
    private List<Transform> activePath;
    private Transform finalTarget;
    private Coroutine followCoroutine;
    private float nodeThreshold = 1.5f;

    private void Awake()
    {
        unit = GetComponent<MusouUnit>();
    }

    // --- PUBLIC ENTRY POINT ---
    // Anyone (BattleEventManager, Spawners, etc.) can call this to make a unit walk a path!
    public void InitiatePathFollow(List<Transform> nodes, Transform finalObjective)
    {
        if (nodes == null || nodes.Count == 0) return;

        activePath = new List<Transform>(nodes);
        finalTarget = finalObjective;

        if (followCoroutine != null) StopCoroutine(followCoroutine);
        followCoroutine = StartCoroutine(FollowPathRoutine());
    }

    private IEnumerator FollowPathRoutine()
    {
        // One-frame physics stabilization buffer to prevent spawning teleport glitches
        yield return null;

        foreach (Transform node in activePath)
        {
            if (unit == null || !unit.enabled || node == null) yield break;

            unit.missionTarget = node;
            float checkTimer = 0f;

            while (unit != null)
            {
                // Drive your existing MusouUnit movement methods cleanly
                if (unit.currentTarget == null)
                {
                    unit.MoveTowards(node.position, false);
                }

                // Low-overhead proximity check (5 times a second)
                checkTimer += Time.deltaTime;
                if (checkTimer >= 0.2f)
                {
                    checkTimer = 0f;
                    if (Vector2.Distance(transform.position, node.position) <= nodeThreshold)
                    {
                        break; // Step to the next node container link
                    }
                }

                yield return null;
            }
        }

        // Final drop-off assignment logic once the node track completely runs out
        if (unit != null)
        {
            unit.missionTarget = finalTarget;
            Debug.Log($"[PATH COMPLETE] {gameObject.name} has finished its assigned path route!");
        }
    }

    private void OnDisable()
    {
        if (followCoroutine != null) StopCoroutine(followCoroutine);
    }
}