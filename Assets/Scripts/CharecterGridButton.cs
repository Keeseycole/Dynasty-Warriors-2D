using UnityEngine;
using UnityEngine.UI;

public class CharacterGridButton : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button targetButton;

    private int assignedIndex;
    private CharecterSelectUI uiMaster;
    private RectTransform myRectTransform;

    private void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
        if (targetButton == null) targetButton = GetComponent<Button>();
    }

    public void SetupButton(int index, Sprite portrait, CharecterSelectUI masterScript)
    {
        assignedIndex = index;
        uiMaster = masterScript;

        if (portraitImage != null) portraitImage.sprite = portrait;

        // 🔥 THE CODESIDE PADLOCK:
        // Clear any broken inspector hooks and force the button to run our function directly!
        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(OnPortraitClicked);
    }

    private void OnPortraitClicked()
    {
        if (uiMaster != null)
        {
            // Pass this button's unique index (0, 1, 2, etc.) up to the master controller canvas
            uiMaster.SelectCharacterFromGrid(assignedIndex, myRectTransform);
        }
    }
}