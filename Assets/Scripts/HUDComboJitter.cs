using UnityEngine;
using UnityEngine.UI;

public class HUDComboJitter : MonoBehaviour
{
    public static HUDComboJitter Instance;

    [Header("Component Hookups")]
    [SerializeField] private Text comboText;

    [Header("Arcade Scale Settings")]
    public float maxSwellMultiplier = 1.35f;
    public float scaleReturnSpeed = 7.0f;

    [Header("Arcade Jitter Settings")]
    public float baseJitterIntensity = 8.0f;
    public float jitterDecaySpeed = 6.0f;

    private Vector3 nativeBaselineScale;
    private Vector3 nativeBaselinePosition;

    private float currentSwellProgress = 1.0f;
    private float currentJitterIntensity = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (comboText == null) comboText = GetComponent<Text>();

        // Securely lock in your pristine, unshifted baseline layout vectors
        nativeBaselineScale = transform.localScale;
        nativeBaselinePosition = transform.localPosition;
    }

    private void Update()
    {
        // 1. PROCESS UNIFORM SCALE SWELL
        if (currentSwellProgress > 1.0f)
        {
            currentSwellProgress = Mathf.MoveTowards(currentSwellProgress, 1.0f, Time.deltaTime * scaleReturnSpeed);
            transform.localScale = nativeBaselineScale * currentSwellProgress;
        }

        // 2. PROCESS IMPACT POSITION JITTER
        if (currentJitterIntensity > 0.05f)
        {
            // Rapidly decay the shaking force down toward absolute zero over time
            currentJitterIntensity = Mathf.MoveTowards(currentJitterIntensity, 0f, Time.deltaTime * jitterDecaySpeed);

            // Generate an explosive random offset offset matching our intensity score
            float randomX = Random.Range(-1f, 1f) * currentJitterIntensity;
            float randomY = Random.Range(-1f, 1f) * currentJitterIntensity;

            // Apply the offset directly on top of our permanent baseline position
            transform.localPosition = nativeBaselinePosition + new Vector3(randomX, randomY, 0f);
        }
        else
        {
            // Forcibly snap the text perfectly back to its absolute rest coordinates when decay finishes
            if (transform.localPosition != nativeBaselinePosition)
            {
                transform.localPosition = nativeBaselinePosition;
            }
        }
    }

    /// <summary>
    /// Call this to trigger a crisp arcade impact response.
    /// Sharp uniform scale burst coupled with a chaotic, self-stabilizing positional jitter loop!
    /// </summary>
    public void TriggerComboHitJuice(int currentStreak)
    {
        // Instantly trigger our scale swell tracking factors
        currentSwellProgress = maxSwellMultiplier;
        transform.localScale = nativeBaselineScale * currentSwellProgress;

        // 🔥 THE DYNAMIC JITTER MULTIPLIER:
        // Escalates the text shaking intensity on higher streak counts to reward long chains!
        float streakBonusFactor = Mathf.Clamp(1.0f + (currentStreak * 0.005f), 1.0f, 1.75f);
        currentJitterIntensity = baseJitterIntensity * streakBonusFactor;
    }
}
