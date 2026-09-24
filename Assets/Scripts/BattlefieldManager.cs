using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlefieldManager : MonoBehaviour
{
    public static BattlefieldManager Instance;

    public List<MusouUnit> playerSideUnits = new List<MusouUnit>();
    public List<MusouUnit> enemySideUnits = new List<MusouUnit>();

    [Header("Off-Screen Simulation Settings")]
    public float simulationTickRate = 1.5f;
    public float offScreenDistanceThreshold = 25f;

    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        StartCoroutine(BackgroundCombatSimulationRoutine());
    }

    public List<MusouUnit> activeUnits
    {
        get
        {
            List<MusouUnit> combinedList = new List<MusouUnit>(playerSideUnits);
            combinedList.AddRange(enemySideUnits);
            return combinedList;
        }
    }

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

    public Transform RequestClosestGlobalEnemy(Vector2 queryingUnitPos, MusouUnit.Team queryingTeam)
    {
        List<MusouUnit> opposingList = (queryingTeam == MusouUnit.Team.PlayerSide) ? enemySideUnits : playerSideUnits;
        opposingList.RemoveAll(u => u == null);

        if (opposingList.Count == 0) return null;

        Transform closestTarget = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < opposingList.Count; i++)
        {
            MusouUnit candidate = opposingList[i];
            if (candidate == null) continue;

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

    private IEnumerator BackgroundCombatSimulationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(simulationTickRate);

            playerSideUnits.RemoveAll(u => u == null);
            enemySideUnits.RemoveAll(u => u == null);

            if (playerTransform == null)
            {
                GameObject pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) playerTransform = pObj.transform;
                continue;
            }

            // ========================================================================
            // 🟩 LOOP PASS A: ENEMY ATTACK SQUAD TRACE
            // Loops through all active enemies. If they have a valid, living target,
            // they deal damage down to your allied soldiers cleanly!
            // ========================================================================
            for (int i = 0; i < enemySideUnits.Count; i++)
            {
                MusouUnit enemyUnit = enemySideUnits[i];
                if (enemyUnit == null || enemyUnit.currentTarget == null) continue;

                Health targetHealth = enemyUnit.currentTarget.GetComponent<Health>() ?? enemyUnit.currentTarget.GetComponentInChildren<Health>();
                if (targetHealth == null || targetHealth.currentHealth <= 0)
                {
                    enemyUnit.currentTarget = null;
                    continue;
                }

                MusouUnit alliedTarget = enemyUnit.currentTarget.GetComponent<MusouUnit>() ?? enemyUnit.currentTarget.GetComponentInParent<MusouUnit>();
                if (alliedTarget == null || alliedTarget.unitTeam != MusouUnit.Team.PlayerSide) continue;

                if (Random.value > 0.20f) continue;

                // Enemy physically strikes the Ally!
                ExecuteSimulatedAttack(enemyUnit, alliedTarget);
            }

            // ========================================================================
            // 🟩 LOOP PASS B: ALLY ATTACK SQUAD TRACE (FIXED IMMUNE ALLIES)
            // Loops independently through all player-side soldiers. If they carry an active, 
            // living enemy target, they deal damage back to the enemy units symmetrically!
            // ========================================================================
            for (int j = 0; j < playerSideUnits.Count; j++)
            {
                MusouUnit alliedUnit = playerSideUnits[j];
                if (alliedUnit == null || alliedUnit.currentTarget == null) continue;

                Health targetHealth = alliedUnit.currentTarget.GetComponent<Health>() ?? alliedUnit.currentTarget.GetComponentInChildren<Health>();
                if (targetHealth == null || targetHealth.currentHealth <= 0)
                {
                    alliedUnit.currentTarget = null;
                    continue;
                }

                MusouUnit enemyTarget = alliedUnit.currentTarget.GetComponent<MusouUnit>() ?? alliedUnit.currentTarget.GetComponentInParent<MusouUnit>();
                if (enemyTarget == null || enemyTarget.unitTeam != MusouUnit.Team.EnemySide) continue;

                if (Random.value > 0.20f) continue;

                // Ally physically strikes the Enemy!
                ExecuteSimulatedAttack(alliedUnit, enemyTarget);
            }
        }
    }
    private void ExecuteSimulatedAttack(MusouUnit attacker, MusouUnit defender)
    {
        if (attacker == null || defender == null) return;

        Health defenderHealth = defender.GetComponent<Health>() ?? defender.GetComponentInChildren<Health>();
        if (defenderHealth == null || defenderHealth.currentHealth <= 0) return;

        float rawAttack = attacker.stats.attackPower > 0 ? attacker.stats.attackPower : 5f;
        float baseDefense = defender.stats.defensePower;

        float attackerMoraleMultiplier = 1.0f + ((attacker.stats.morale - 50f) / 100f);
        float defenderMoraleMultiplier = 1.0f + ((defender.stats.morale - 50f) / 100f);

        float calculatedAttack = rawAttack * attackerMoraleMultiplier;
        float effectiveDefense = baseDefense * defenderMoraleMultiplier;

        float baseCombatMitigation = calculatedAttack - (effectiveDefense * 0.5f);
        float absoluteMinimumPenetrationFloor = calculatedAttack * 0.25f;
        float rawCalculatedDamage = Mathf.Max(baseCombatMitigation, absoluteMinimumPenetrationFloor);

        // ========================================================================
        // 🟩 ANTI-SWARM FORCE DAMPENER (FIXED ENEMIES LACKING DAMAGE IN CROWDS):
        // Automatically counts how many active rivals are currently targeting this defender!
        // If 5 enemies are jumping 1 ally, we divide the incoming damage by a crowd factor.
        // This stops swarms from instantly deleting targets, giving both sides plenty of 
        // heartbeat ticks to trade hits and lose health steadily together!
        // ========================================================================
        int activeAttackerCount = 1;
        List<MusouUnit> attackerFactionList = (attacker.unitTeam == MusouUnit.Team.EnemySide) ? enemySideUnits : playerSideUnits;

        for (int i = 0; i < attackerFactionList.Count; i++)
        {
            if (attackerFactionList[i] != null && attackerFactionList[i] != attacker)
            {
                // If another teammate is also locked onto our current defender, increment the crowd scale
                if (attackerFactionList[i].currentTarget == defender.transform)
                {
                    activeAttackerCount++;
                }
            }
        }

        // Apply a gentle division scale based on crowd density to preserve unit longevity
        float crowdDampeningFactor = 1.0f / (1.0f + (activeAttackerCount * 0.4f));

        float offScreenScale = 0.05f;
        float scaledDamage = rawCalculatedDamage * offScreenScale * crowdDampeningFactor;

        int finalStepDamage = Mathf.RoundToInt(scaledDamage);

        if (finalStepDamage < 1) finalStepDamage = 1;

        defenderHealth.currentHealth -= finalStepDamage;

        if (defenderHealth.healthBar != null)
        {
            defenderHealth.healthBar.UpdateBar(defenderHealth.currentHealth, defenderHealth.maxHealth);
        }

        if (defenderHealth.currentHealth <= 0)
        {
            defenderHealth.currentHealth = 0;

            if (MoraleManager.Instance != null || MoraleManager.Instance != null)
            {
                float offScreenPointsGranted = defender.isOfficer ? 8f : 0.25f;
                MusouUnit.Team victoriousFactionTeam = (defender.unitTeam == MusouUnit.Team.EnemySide) ?
                                                       MusouUnit.Team.PlayerSide : MusouUnit.Team.EnemySide;

                if (MoraleManager.Instance != null) MoraleManager.Instance.ChangeMorale(victoriousFactionTeam, offScreenPointsGranted);
                else if (MoraleManager.Instance != null) MoraleManager.Instance.ChangeMorale(victoriousFactionTeam, offScreenPointsGranted);
            }

            defenderHealth.Die();
        }
    }
}