using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ComboCounter : MonoBehaviour
{
    public static ComboCounter Instance { get; private set; }

    [Header("UI Components")]
    [Tooltip("Drag your legacy UI Text component here")]
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
            comboText.enabled = false; // Hide until first hit connects
        }
    }

    private void Update()
    {
        if (!isComboActive) return;

        currentExpiryTimer -= Time.deltaTime;

        if (currentExpiryTimer <= 0f)
        {
            BreakCombo();
        }
    }

    public void AddHit(int hitsToAdd = 1)
    {
        currentComboCount += hitsToAdd;
        currentExpiryTimer = comboExpiryDuration;
        isComboActive = true;

        if (comboText != null)
        {
            if (!comboText.enabled) comboText.enabled = true;

            comboText.text = $"{currentComboCount} <size=40%>Combo</size>";
            UpdateComboTierColor();

            // Trigger a perfectly clean scale pop forward with ZERO position shifts!
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
        Vector3 targetPopScale = originalTextScale * 1.35f;
        textTransform.localScale = targetPopScale;

        float elapsed = 0f;
        float duration = 0.15f;

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
            comboText.enabled = false;
        }
    }
}