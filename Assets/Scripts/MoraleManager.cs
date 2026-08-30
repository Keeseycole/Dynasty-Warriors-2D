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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Set initial balance line precisely in the center on boot
        if (playerMoraleFillImage != null)
        {
            targetFillAmount = playerFactionMorale / 100f;
            playerMoraleFillImage.fillAmount = targetFillAmount;
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

        // ========================================================================
        // 🔥 THE GLOBAL BROADCAST RIPPLE PASS:
        // Loops backward through the living army grid. Instantly forces every soldier
        // to re-evaluate their attack values, defense, and speed based on the new bar!
        // ========================================================================
        for (int i = activeBattlefieldUnits.Count - 1; i >= 0; i--)
        {
            if (activeBattlefieldUnits[i] != null)
            {
                // Force individual unit attributes to scale to the global wave change
                activeBattlefieldUnits[i].SyncIndividualWithGlobalMorale();
            }
            else
            {
                // Safety cleanup loop: Prunes out any destroyed array pointers safely
                activeBattlefieldUnits.RemoveAt(i);
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
}