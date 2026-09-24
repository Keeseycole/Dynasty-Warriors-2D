using System.Collections.Generic; // 🔥 REQUISITE SYSTEM GATEWAY FOR LIST STRUCTURES
using UnityEngine;
using UnityEngine.UI;

public class MoraleManager : MonoBehaviour
{
    public static MoraleManager Instance;

    [Header("Current Morale Scales (0 to 100)")]
    [Range(0f, 100f)] public float playerFactionMorale = 50f;
    [Range(0f, 100f)] public float enemyFactionMorale = 50f;

    [Header("🔥 Retro UI Layout Hooks")]
    [Tooltip("Drag your 'Player_Morale_Fill' Image component here!")]
    public Image playerMoraleFillImage;

    [Tooltip("How fast the UI line moves to its new position (Classic: 0.5f to 1.5f)")]
    public float uiSmoothSpeed = 0.5f;

    // Internal target value the UI image smoothly glides toward
    private float targetFillAmount = 0.5f;

    // ========================================================================
    // 🔥 THE MASTER BATTLEFIELD LIST TRACKER:
    // Holds an active memory pointer for every single living unit on the map.
    // Allows massive global stat updates to occur with absolute zero lookup lag!
    // ========================================================================
    [HideInInspector]
    public List<MusouUnit> activeBattlefieldUnits = new List<MusouUnit>();

    public float EventMoraleLoss = 40f; // ◄── PUBLIC ADJUSTABLE SLIDER / INPUT!

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (playerMoraleFillImage != null)
        {
            targetFillAmount = playerFactionMorale / 100f;
            playerMoraleFillImage.fillAmount = targetFillAmount;
        }

        // ========================================================================
        // 🟩 THE BOOT LEVEL SWEEP (NEW PERFORMANCE GATHER):
        // Sweeps the map layout on frame one to manually register any characters
        // that were drag-and-dropped straight into the editor scene hierarchy view!
        // ========================================================================
        MusouUnit[] prePlacedUnits = FindObjectsByType<MusouUnit>(FindObjectsSortMode.None);
        for (int i = 0; i < prePlacedUnits.Length; i++)
        {
            if (prePlacedUnits[i] != null && !activeBattlefieldUnits.Contains(prePlacedUnits[i]))
            {
                activeBattlefieldUnits.Add(prePlacedUnits[i]);
            }
        }
    }

    private void Update()
    {
        // 🔥 THE RETRO SLIDE EFFECT: 
        // Smoothly glides the dividing line across the UI frame instead of snapping it instantly!
        if (playerMoraleFillImage != null && Mathf.Abs(playerMoraleFillImage.fillAmount - targetFillAmount) > 0.001f)
        {
            playerMoraleFillImage.fillAmount = Mathf.MoveTowards(
                playerMoraleFillImage.fillAmount,
                targetFillAmount,
                Time.deltaTime * uiSmoothSpeed
            );
        }
    }

    /// <summary>
    /// Call this dynamically inside Health.Die() or BattleEventManager shifts!
    /// </summary>
    /// <summary>
    /// Adjusts the global team morale scales and triggers an event-driven broadcast pass.
    /// Safely handles massive simultaneous death events (like fire traps) without thread crashes!
    /// </summary>
    /// <summary>
    /// Adjusts the global team morale scales and triggers an event-driven broadcast pass.
    /// Safely handles massive simultaneous death events (like fire traps) and prints out active count diagnostics.
    /// </summary>
    public void ChangeMorale(MusouUnit.Team scoringTeam, float amount)
    {
        if (scoringTeam == MusouUnit.Team.PlayerSide)
        {
            playerFactionMorale = Mathf.Min(100f, playerFactionMorale + amount);
            enemyFactionMorale = Mathf.Max(0f, enemyFactionMorale - amount);
        }
        else if (scoringTeam == MusouUnit.Team.EnemySide)
        {
            enemyFactionMorale = Mathf.Min(100f, enemyFactionMorale + amount);
            playerFactionMorale = Mathf.Max(0f, playerFactionMorale - amount);
        }

        // Calculate what percentage of the bar should be filled by the Player Side
        targetFillAmount = playerFactionMorale / 100f;

        if (activeBattlefieldUnits == null) return;

        // ========================================================================
        // 🟩 THE RE-ENGINEERED THREAD GUARD (FIXED MASS DEATHTRAPS):
        // 1. Instantly prunes out any missing, null, or dead references using an optimized
        //    Lambda filter BEFORE we broadcast. This prevents any Null Reference Exceptions!
        // 2. Safely loops through the remaining live array grid to pass the morale values.
        // ========================================================================

        // Step A: Clean out dead units first so they cannot block or crash our loops
        activeBattlefieldUnits.RemoveAll(unit => unit == null || unit.gameObject == null);

        int updatedAllies = 0;
        int updatedEnemies = 0;

        // Step B: Loop forward safely through 100% verified living, active combat units
        for (int i = 0; i < activeBattlefieldUnits.Count; i++)
        {
            MusouUnit currentUnit = activeBattlefieldUnits[i];

            if (currentUnit != null)
            {
                currentUnit.SyncIndividualWithGlobalMorale();

                // Live internal tracking counts
                if (currentUnit.unitTeam == MusouUnit.Team.PlayerSide) updatedAllies++;
                else if (currentUnit.unitTeam == MusouUnit.Team.EnemySide) updatedEnemies++;
            }
        }


    }
    /// <summary>
    /// Calculates an adjusted aggression score factoring in team morale and difficulty ceilings.
    /// </summary>
    public float GetAdjustedAggression(MusouUnit.Team unitTeam, float baseAggression)
    {
        float activeBaseAggression = (baseAggression > 0.05f) ? baseAggression : 0.45f;

        float minAggressionCap = 0.35f; // Raised to keep test scenes aggressive!
        float maxAggressionCap = 0.85f;
        float difficultyMultiplier = 1.0f;

        DifficultyLevel currentDiff = DifficultyLevel.Normal;
        if (DifficultyManager.Instance != null) currentDiff = DifficultyManager.Instance.currentDifficulty;

        switch (currentDiff)
        {
            case DifficultyLevel.Easy:
                minAggressionCap = 0.2f;
                maxAggressionCap = 0.45f;
                difficultyMultiplier = 0.6f;
                break;
            case DifficultyLevel.Normal:
                minAggressionCap = 0.35f;
                maxAggressionCap = 0.7f;
                difficultyMultiplier = 1.0f;
                break;
            case DifficultyLevel.Hard:
                minAggressionCap = 0.55f;
                maxAggressionCap = 0.85f;
                difficultyMultiplier = 1.3f;
                break;
            case DifficultyLevel.Chaos:
                minAggressionCap = 0.75f;
                maxAggressionCap = 0.95f;
                difficultyMultiplier = 1.6f;
                break;
        }

        float factionMorale = (unitTeam == MusouUnit.Team.PlayerSide) ? playerFactionMorale : enemyFactionMorale;

        // ========================================================================
        // 🟩 THE IN-SCENE INITIALIZATION GUARD (FIXED):
        // If a test scene starts up and morale scales are sitting at absolute 0,
        // force them to default to a neutral 50 baseline so your AI score doesn't tank!
        // ========================================================================
        if (factionMorale < 1f) factionMorale = 50f;

        float normalizedMoraleCurve = (factionMorale - 50f) / 50f;
        float moraleInfluenceValue = normalizedMoraleCurve * 0.15f;

        float finalCalculatedScore = (activeBaseAggression * difficultyMultiplier) + moraleInfluenceValue;
        return Mathf.Clamp(finalCalculatedScore, minAggressionCap, maxAggressionCap);
    }

    // ========================================================================
    // 🟩 SCRIPTED MORALE CRISIS ENGINE (FIXED YI LING FIRE ATTACK PLUMMET):
    // Completely bypasses your standard casualty reward and team sorting logic!
    // Call this explicitly from your map event managers to forcefully drop a faction's
    // morale slider down to a crisis state with 100% mechanical consistency.
    // ========================================================================
    public void ApplyScriptedMoraleCrisis(MusouUnit.Team targetedTeam, float moraleLossAmount)
    {
        // If no custom loss parameter was explicitly passed, pull directly from your Inspector slider!
        if (moraleLossAmount < 0f)
        {
            moraleLossAmount = EventMoraleLoss;
        }

        float absoluteLossValue = Mathf.Abs(moraleLossAmount);

        if (BattlefieldManager.Instance != null)
        {
            var targetFactionList = (targetedTeam == MusouUnit.Team.EnemySide) ?
                                     BattlefieldManager.Instance.enemySideUnits :
                                     BattlefieldManager.Instance.playerSideUnits;

            if (targetFactionList != null)
            {
                for (int u = 0; u < targetFactionList.Count; u++)
                {
                    MusouUnit soldier = targetFactionList[u];
                    if (soldier != null)
                    {
                        // Drop their individual combat stats directly to simulate raw panic!
                        soldier.stats.morale = (int)Mathf.Clamp(soldier.stats.morale - absoluteLossValue, 10f, 100f);
                    }
                }
            }
        }

        if (targetedTeam == MusouUnit.Team.PlayerSide)
        {
            // Symmetrically boost the Enemy side by your positive loss value amount.
            // This naturally forces the allied bar to plunge on your UI slider cleanly!
            ChangeMorale(MusouUnit.Team.EnemySide, absoluteLossValue);
        }
        else if (targetedTeam == MusouUnit.Team.EnemySide)
        {
            // Symmetrically boost the Player side if the fire attack hits the enemy lines instead.
            ChangeMorale(MusouUnit.Team.PlayerSide, absoluteLossValue);
        }
    }
}