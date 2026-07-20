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
    }

    public void SetupButton(int index, Sprite portrait, CharecterSelectUI masterScript)
    {
        assignedIndex = index;
        uiMaster = masterScript;

        if (portraitImage != null) portraitImage.sprite = portrait;

        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(OnPortraitClicked);
    }

    private void OnPortraitClicked()
    {
        // Pass both the index reference and this tile's layout position
        uiMaster.SelectCharacterFromGrid(assignedIndex, myRectTransform);
    }
}
