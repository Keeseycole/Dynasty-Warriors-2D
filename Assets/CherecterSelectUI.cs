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
        foreach (Transform child in gridContentContainer) Destroy(child.gameObject);

        for (int i = 0; i < dataCarrier.availableCharacters.Length; i++)
        {
            CharacterData character = dataCarrier.availableCharacters[i];
            if (character == null) continue;

            GameObject buttonObj = Instantiate(portraitButtonPrefab, gridContentContainer);
            CharacterGridButton gridScript = buttonObj.GetComponent<CharacterGridButton>();

            if (gridScript != null)
            {
                gridScript.SetupButton(i, character.gridIcon, this);
            }
        }
    }

    public void SelectCharacterFromGrid(int index, RectTransform buttonRect)
    {
        activeGridIndex = index;
        dataCarrier.UpdateIndex(index);

        CharacterData selected = dataCarrier.availableCharacters[activeGridIndex];
        if (selected != null)
        {
            if (massiveOfficerImage != null) massiveOfficerImage.sprite = selected.massivePreview;
            if (nameText != null) nameText.text = selected.characterName;
            if (titleText != null) titleText.text = selected.characterTitle;

            // 🔥 1. DEFINE YOUR INDIVIDUAL MAX CAPS HERE:
            float maxHealthCap = 500f;
            float maxAttackCap = 500f;
            float maxDefenseCap = 500f;

            // 2. FORCE THE VISUAL TRACKS TO FILL BY PERCENTAGE (0.0 to 1.0)
            if (healthSlider != null) healthSlider.value = selected.maxHealth / maxHealthCap;
            if (attackSlider != null) attackSlider.value = selected.attackPower / maxAttackCap;
            if (defenseSlider != null) defenseSlider.value = selected.defensePower / maxDefenseCap;

            // 3. DISPLAY THE LITERAL STAT NUMBERS UNTOUCHED
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
        dataCarrier.LoadGameScene(battleSceneName);
    }
}