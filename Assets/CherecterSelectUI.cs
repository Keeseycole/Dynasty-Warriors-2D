using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharecterSelectUI : MonoBehaviour
{
    [Header("Roster Architecture")]
    [SerializeField] private GameObject portraitButtonPrefab;
    [SerializeField] private Transform gridContentContainer;

    [Header(" Preview Panel (Left Side)")]
    [SerializeField] private Image massiveOfficerImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header(" Bottom Stat Bars")]
    [SerializeField] private Slider attackSlider;
    [SerializeField] private Slider defenseSlider;
    [SerializeField] private Slider healthSlider;

    // 🔥 NEW: Text references to display the literal numerical stats!
    [Header(" Stat Numbers")]
    [SerializeField] private TextMeshProUGUI attackValueText;
    [SerializeField] private TextMeshProUGUI defenseValueText;
    [SerializeField] private TextMeshProUGUI healthValueText;

    [Header("The Selection Cursor")]
    [SerializeField] private RectTransform activeCursorHighlight;

    [Header("Scene Config")]
    [SerializeField] private string battleSceneName = "BattlefieldScene";

    private CharacterSelectManager dataCarrier; // Target unified manager name
    private int activeGridIndex = 0;

    private void Start()
    {
        dataCarrier = CharacterSelectManager.Instance;
        if (dataCarrier == null) return;

        GenerateBasaraGrid();
        SelectCharacterFromGrid(0, null);
    }

    private void GenerateBasaraGrid()
    {
        // Wipe out placeholder templates
        foreach (Transform child in gridContentContainer) Destroy(child.gameObject);

        // 🔥 CRITICAL: Must use a standard 'for' loop so 'i' increases unique values (0, 1, 2...)
        for (int i = 0; i < dataCarrier.availableCharacters.Length; i++)
        {
            CharacterData character = dataCarrier.availableCharacters[i];
            if (character == null) continue;

            // Spawn button instance
            GameObject buttonObj = Instantiate(portraitButtonPrefab, gridContentContainer);
            CharacterGridButton gridScript = buttonObj.GetComponent<CharacterGridButton>();

            if (gridScript != null)
            {
                // 🔥 THE FIX: Make sure you pass 'i' explicitly into the setup!
                gridScript.SetupButton(i, character.gridIcon, this);
            }
        }
    }
    public void SelectCharacterFromGrid(int index, RectTransform buttonRect)
    {
        activeGridIndex = index;

        if (dataCarrier != null)
        {
            dataCarrier.UpdateIndex(index);
        }

        CharacterData selected = dataCarrier.availableCharacters[activeGridIndex];
        if (selected != null)
        {
            if (massiveOfficerImage != null) massiveOfficerImage.sprite = selected.massivePreview;
            if (nameText != null) nameText.text = selected.characterName;
            if (titleText != null) titleText.text = selected.characterTitle;

            // =========================================================================
            // 🔥 THE UNIVERSAL SCALE CONSTANT CAPS:
            // Change these numbers to whatever you want your true "Full Bar" value to be.
            // Setting maxHealthCap to 500f means Ling Yin (64 HP) will correctly fill 
            // only about 13% of her health bar!
            // =========================================================================
            float maxHealthCap = 250f;  // Set this to 500f, 1000f, or whatever you choose!
            float maxAttackCap = 250f;  // Shared universal maximum for attack bar
            float maxDefenseCap = 250f; // Shared universal maximum for defense bar
            // =========================================================================

            // Calculate slider values relative to your absolute caps
            if (healthSlider != null) healthSlider.value = selected.maxHealth / maxHealthCap;
            if (attackSlider != null) attackSlider.value = selected.attackPower / maxAttackCap;
            if (defenseSlider != null) defenseSlider.value = selected.defensePower / maxDefenseCap;

            // The raw numerical strings remain untouched (displays literal 64, 100, etc.)
            if (healthValueText != null) healthValueText.text = selected.maxHealth.ToString("F0");
            if (attackValueText != null) attackValueText.text = selected.attackPower.ToString("F0");
            if (defenseValueText != null) defenseValueText.text = selected.defensePower.ToString("F0");
        }

        if (activeCursorHighlight != null && buttonRect != null)
        {
            activeCursorHighlight.gameObject.SetActive(true);
            activeCursorHighlight.position = buttonRect.position;
            activeCursorHighlight.sizeDelta = buttonRect.sizeDelta;
        }
    }
    public void ConfirmOfficerChoice()
    {
        // 🔥 THE ABSOLUTE PHYSICS RESET:
        // Force the engine's internal physics clocks back to 1 BEFORE loading the scene.
        // If this is missed, Unity 6 will carry the frozen time properties over, locking your Rigidbody!
        Time.timeScale = 1f;

        if (CharacterSelectManager.Instance != null)
        {
            // Launch the level using your persistent loader thread
            CharacterSelectManager.Instance.LoadGameScene(battleSceneName);
        }
        else
        {
            // Hard fallback if testing without the persistent object awake
            UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
        }
    }
}