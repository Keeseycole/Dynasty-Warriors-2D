using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlefieldManager : MonoBehaviour
{
    public static BattlefieldManager Instance;

    // Fast tracking list clusters for simple vector distance checks
    public List<MusouUnit> playerSideUnits = new List<MusouUnit>();
    public List<MusouUnit> enemySideUnits = new List<MusouUnit>();



    [Header("Off-Screen Simulation Settings")]
    [Tooltip("How often background battles process their damage checks (in seconds).")]
    public float simulationTickRate = 1.5f;
    [Tooltip("The minimum distance a unit must be from the player to be considered off-screen.")]
    public float offScreenDistanceThreshold = 25f;

    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Find the player early to track global proximity circles
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        // 🔥 ACTIVATE THE BACKGROUND SIMULATION MOTOR:
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

    // ========================================================================
    // 🔥 THE OFF-SCREEN COMBAT BALANCING ENGINE (NEW)
    // Runs automatically on a gentle background ticker. Iterates through units
    // out of player sight to simulate territorial wars with safe pace dampening!
    // ========================================================================
    private IEnumerator BackgroundCombatSimulationRoutine()
    {
        float thresholdSqr = offScreenDistanceThreshold * offScreenDistanceThreshold;

        while (true)
        {
            yield return new WaitForSeconds(simulationTickRate);

            // Clean up dead/destroyed object references safely before processing
            playerSideUnits.RemoveAll(u => u == null);
            enemySideUnits.RemoveAll(u => u == null);

            if (playerTransform == null)
            {
                GameObject pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) playerTransform = pObj.transform;
                continue;
            }

            Vector2 playerPos = playerTransform.position;

            // Loop through all active enemy units to find candidates for background clashes
            for (int i = 0; i < enemySideUnits.Count; i++)
            {
                MusouUnit enemyUnit = enemySideUnits[i];
                if (enemyUnit == null || enemyUnit.currentTarget == null) continue;

                Vector2 enemyPos = enemyUnit.rb != null ? enemyUnit.rb.position : (Vector2)enemyUnit.transform.position;

                // 1. DISTANCE VALIDATION: Only simulate if they are far away from the player camera view!
                if ((enemyPos - playerPos).sqrMagnitude < thresholdSqr) continue;

                // 2. TARGET LOOKUP: Check if the enemy unit is currently targeting a friendly player-side unit
                MusouUnit alliedTarget = enemyUnit.currentTarget.GetComponent<MusouUnit>() ?? enemyUnit.currentTarget.GetComponentInParent<MusouUnit>();
                if (alliedTarget == null || alliedTarget.unitTeam != MusouUnit.Team.PlayerSide) continue;

                // 3. 🔥 THE PACE LIMITER DIE ROLL:
                // Background troops do not hit successfully on every single frame tick.
                // This skips processing on 80% of ticks to stretch battles out organically!
                if (Random.value > 0.20f) continue;

                // 4. PROCESS SIMULATED STAT DUELS:
                ExecuteSimulatedAttack(enemyUnit, alliedTarget); // Enemy attacks Ally
                ExecuteSimulatedAttack(alliedTarget, enemyUnit); // Ally counters Enemy back
            }
        }
    }

    // ========================================================================
    // 🟩 THE DISCRETE STEP ATTACK CORE (FIXED):
    // Processes a clean stat duel once per heartbeat tick. 
    // Uses strict rounding math to guarantee health drops in clear, integer steps!
    // ========================================================================
    private void ExecuteSimulatedAttack(MusouUnit attacker, MusouUnit defender)
    {
        if (attacker == null || defender == null) return;

        Health defenderHealth = defender.GetComponent<Health>() ?? defender.GetComponentInChildren<Health>();
        if (defenderHealth == null || defenderHealth.currentHealth <= 0) return;

        // 1. Pull raw statistics directly out of your pre-loaded data structs
        float rawAttack = attacker.stats.attackPower > 0 ? attacker.stats.attackPower : 5f;
        float baseDefense = defender.stats.defensePower;

        // 2. Factor in active faction morale scales (50 Morale = 1.0x baseline multiplier)
        float attackerMoraleMultiplier = 1.0f + ((attacker.stats.morale - 50f) / 100f);
        float defenderMoraleMultiplier = 1.0f + ((defender.stats.morale - 50f) / 100f);

        float calculatedAttack = rawAttack * attackerMoraleMultiplier;
        float effectiveDefense = baseDefense * defenderMoraleMultiplier;

        // 3. APPLY ATTACK RATIO DAMPENER:
        // Scales your massive combat stats down into an appropriate background step size.
        float offScreenScale = 0.05f;
        float rawCalculatedDamage = (calculatedAttack - (effectiveDefense * 0.5f)) * offScreenScale;

        // 4. 🔥 THE WHOLE-NUMBER CLAMP CUE:
        // By casting this value through Mathf.RoundToInt(), we completely convert the damage 
        // value from a sliding float scale into a solid whole number integer chunk!
        int finalStepDamage = Mathf.RoundToInt(rawCalculatedDamage);

        // 5. Guarantee a minimum hard clamp of 1 solid point of damage per successful tick
        if (finalStepDamage < 1) finalStepDamage = 1;

        // 6. Deliver the solid chunk damage directly to their primary health storage parameters
        defenderHealth.currentHealth -= finalStepDamage;

        // Sync their visual health bar display values right on the tick frame
        if (defenderHealth.healthBar != null)
        {
            defenderHealth.healthBar.UpdateBar(defenderHealth.currentHealth, defenderHealth.maxHealth);
        }



        if (defenderHealth.currentHealth <= 0)
        {
            defenderHealth.Die();
        }
    }
}