using UnityEngine;
using TMPro; // Requires Unity TextMeshPro
using System.Collections;
using UnityEngine.UI;

public class ComboCounterHUD : MonoBehaviour
{
    public static ComboCounterHUD Instance { get; private set; }

    [Header("UI Components")]
    [Tooltip("Drag your TextMeshProUGUI component here")]
    public Text comboText;

    [Header("Combo Rules")]
    [Tooltip("How many seconds the player has to land another hit before the combo breaks")]
    public float comboExpiryDuration = 3f;

    [Header("Stylized Colors")]
    public Color tier1Color = Color.white;       // 1 - 49 Hits
    public Color tier2Color = Color.yellow;      // 50 - 199 Hits
    public Color tier3Color = new Color(1f, 0.5f, 0f); // 200+ Hits (Orange)
    public Color tier4Color = Color.red;         // 500+ Hits (Basara Tier)

    // Internal tracking variables
    private int currentComboCount = 0;
    private float currentExpiryTimer = 0f;
    private bool isComboActive = false;

    private Vector3 originalTextScale;
    private Coroutine activePulseRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (comboText != null)
        {
            originalTextScale = comboText.transform.localScale;
            comboText.enabled = false; // Keep it hidden until the first hit connects
        }
    }

    private void Update()
    {
        if (!isComboActive) return;

        // Count down the decay timer
        currentExpiryTimer -= Time.deltaTime;

        if (currentExpiryTimer <= 0f)
        {
            BreakCombo();
        }
    }

    /// <summary>
    /// Increments the combo counter, pulses the UI text scale, and resets the decay timer.
    /// </summary>
    public void AddHit(int hitsToAdd = 1)
    {
        currentComboCount += hitsToAdd;
        currentExpiryTimer = comboExpiryDuration;
        isComboActive = true;

        if (comboText != null)
        {
            if (!comboText.enabled) comboText.enabled = true;

            // Update Text and Style Layout
            comboText.text = $"{currentComboCount} <size=40%>Combo</size>"; 
            UpdateComboTierColor();

            // Trigger explosive size pulse
            if (activePulseRoutine != null) StopCoroutine(activePulseRoutine);
            activePulseRoutine = StartCoroutine(PulseTextRoutine());
        }
    }

    private void UpdateComboTierColor()
    {
        if (currentComboCount >= 500) comboText.color = tier4Color;
        else if (currentComboCount >= 200) comboText.color = tier3Color;
        else if (currentComboCount >= 50) comboText.color = tier2Color;
        else comboText.color = tier1Color;
    }

    private IEnumerator PulseTextRoutine()
    {
        Transform textTransform = comboText.transform;

        // Instant massive scale pop forward on impact frame (Uses unscaled time to pop during hit-lag!)
        Vector3 targetPopScale = originalTextScale * 1.4f;
        textTransform.localScale = targetPopScale;

        float elapsed = 0f;
        float duration = 0.15f; // Fast snap back to base sizing

        while (elapsed < duration)
        {
            textTransform.localScale = Vector3.Lerp(targetPopScale, originalTextScale, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        textTransform.localScale = originalTextScale;
    }

    private void BreakCombo()
    {
        isComboActive = false;
        currentComboCount = 0;

        if (comboText != null)
        {
            // Simple instant drop out. You can add a fade animation here later if desired!
            comboText.enabled = false;
        }
    }
}