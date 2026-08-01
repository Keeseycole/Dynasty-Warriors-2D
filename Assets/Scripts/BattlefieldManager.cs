using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlefieldManager : MonoBehaviour
{
    public static BattlefieldManager Instance;

    // Fast tracking list clusters for simple vector distance checks
    private List<MusouUnit> playerSideUnits = new List<MusouUnit>();
    private List<MusouUnit> enemySideUnits = new List<MusouUnit>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- REGISTRATION HOOKS ---
    public void RegisterUnit(MusouUnit unit)
    {
        if (unit.unitTeam == MusouUnit.Team.PlayerSide && !playerSideUnits.Contains(unit))
            playerSideUnits.Add(unit);
        else if (unit.unitTeam == MusouUnit.Team.EnemySide && !enemySideUnits.Contains(unit))
            enemySideUnits.Add(unit);
    }

    public void UnregisterUnit(MusouUnit unit)
    {
        if (playerSideUnits.Contains(unit)) playerSideUnits.Remove(unit);
        if (enemySideUnits.Contains(unit)) enemySideUnits.Remove(unit);
    }

    // --- HIGH PERFORMANCE GLOBAL SEARCH MOTOR ---
    public Transform RequestClosestGlobalEnemy(Vector2 queryingUnitPos, MusouUnit.Team queryingTeam)
    {
        // 1. Determine which list represents the opposing side
        List<MusouUnit> opposingList = (queryingTeam == MusouUnit.Team.PlayerSide) ? enemySideUnits : playerSideUnits;

        // 2. Perform a fast clean up of dead references before looping
        opposingList.RemoveAll(u => u == null);

        if (opposingList.Count == 0) return null;

        Transform closestTarget = null;
        float closestSqrDistance = float.MaxValue;

        // 3. Process flat vector math iterations instead of heavy physics queries
        for (int i = 0; i < opposingList.Count; i++)
        {
            MusouUnit candidate = opposingList[i];
            if (candidate == null) continue;

            // Health status protection pass
            Health h = candidate.GetComponent<Health>() ?? candidate.GetComponentInChildren<Health>();
            if (h != null && h.currentHealth <= 0) continue;

            Vector2 candidatePos = (candidate.rb != null) ? candidate.rb.position : (Vector2)candidate.transform.position;
            float sqrDist = (candidatePos - queryingUnitPos).sqrMagnitude;

            if (sqrDist < closestSqrDistance)
            {
                closestSqrDistance = sqrDist;
                closestTarget = candidate.transform;
            }
        }

        return closestTarget;
    }
}