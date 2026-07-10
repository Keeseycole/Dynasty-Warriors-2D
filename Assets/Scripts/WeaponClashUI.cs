using UnityEngine;
using UnityEngine.UI;

public class WeaponClashUI : MonoBehaviour
{
    [Header("UI Visual Elements")]
    [Tooltip("Assign your main Canvas or UI Panel folder container here!")]
    public GameObject clashPanel;

    // 🔥 THE RAW IMAGE CONVERSION:
    // Replaced the old Slider reference component with a direct Image layer component!
    [Tooltip("Assign the Blue Player Fill Image component that overlaps the Red background image!")]
    public Image playerFillImage;

    [Header("Clash Bar Settings")]
    public Color playerColor = Color.blue;

    private void Start()
    {
        // Event delegation hooks
        if (WeaponClashManager.Instance != null)
        {
            WeaponClashManager.Instance.OnClashStateChanged += ToggleClashUI;
            WeaponClashManager.Instance.OnClashValueUpdated += UpdateClashImageFill;
        }

        if (clashPanel != null) clashPanel.SetActive(false);

        // Force your fill image to track your precise color configuration
        if (playerFillImage != null)
        {
            playerFillImage.color = playerColor;
        }
    }

    private void OnDestroy()
    {
        if (WeaponClashManager.Instance != null)
        {
            WeaponClashManager.Instance.OnClashStateChanged -= ToggleClashUI;
            WeaponClashManager.Instance.OnClashValueUpdated -= UpdateClashImageFill;
        }
    }

    private void ToggleClashUI(bool isActive)
    {
        if (clashPanel != null)
        {
            clashPanel.SetActive(isActive);
        }
    }

    /// <summary>
    /// Fires continuously every physics frame tick while a weapon clash struggle is processing!
    /// </summary>
    private void UpdateClashImageFill(float currentBalance)
    {
        if (playerFillImage == null) return;

        // 🔥 THE FILL METHOD: 
        // Directly maps the 0f-1f tug-of-war value to the horizontal texture mask!
        playerFillImage.fillAmount = currentBalance;
    }
}