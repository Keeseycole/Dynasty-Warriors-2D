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
    }

    /// <summary>
    /// Calculates an adjusted aggression score factoring in team morale and difficulty ceilings.
    /// </summary>
    public float GetAdjustedAggression(MusouUnit.Team unitTeam, float baseAggression)
    {
        float minAggressionCap = 0.2f;
        float maxAggressionCap = 0.85f;
        float difficultyMultiplier = 1.0f;

        DifficultyLevel currentDiff = DifficultyLevel.Normal;
        if (DifficultyManager.Instance != null) currentDiff = DifficultyManager.Instance.currentDifficulty;

        switch (currentDiff)
        {
            case DifficultyLevel.Easy:
                minAggressionCap = 0.1f;
                maxAggressionCap = 0.45f;
                difficultyMultiplier = 0.6f;
                break;
            case DifficultyLevel.Normal:
                minAggressionCap = 0.3f;
                maxAggressionCap = 0.7f;
                difficultyMultiplier = 1.0f;
                break;
            case DifficultyLevel.Hard:
                minAggressionCap = 0.5f;
                maxAggressionCap = 0.85f;
                difficultyMultiplier = 1.3f;
                break;
            case DifficultyLevel.Chaos:
                minAggressionCap = 0.7f;
                maxAggressionCap = 0.95f;
                difficultyMultiplier = 1.6f;
                break;
        }

        float factionMorale = (unitTeam == MusouUnit.Team.PlayerSide) ? playerFactionMorale : enemyFactionMorale;
        float normalizedMoraleCurve = (factionMorale - 50f) / 50f;
        float moraleInfluenceValue = normalizedMoraleCurve * 0.15f;

        float finalCalculatedScore = (baseAggression * difficultyMultiplier) + moraleInfluenceValue;
        return Mathf.Clamp(finalCalculatedScore, minAggressionCap, maxAggressionCap);
    }
}