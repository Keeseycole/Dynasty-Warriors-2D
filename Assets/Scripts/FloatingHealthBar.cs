using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : MonoBehaviour
{
    public Image fillImage;
    public GameObject barParent; // The whole canvas or background
    private float hideTimer;

    public void UpdateBar(float current, float max)
    {
        fillImage.fillAmount = current / max;
        
        // Show the bar when taking damage
        barParent.SetActive(true);
        hideTimer = 3f; // Stay visible for 3 seconds
    }

    void Update()
    {
        // DW3 Style: Bars disappear when not in active combat
        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0) barParent.SetActive(false);
        }

        // Keep the bar from flipping if the character flips
        transform.rotation = Quaternion.identity;
    }
}