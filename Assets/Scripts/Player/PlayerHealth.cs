using UnityEngine;
using UnityEngine.UI; // Essential for Slider

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;

    [Header("Dynamic Length")]
    public float pixelsPerHealthPoint = 1f; // Each 1 HP adds 1 pixel of width
    private RectTransform sliderRect;

    void Awake()
    {
        currentHealth = maxHealth;
        sliderRect = healthSlider.GetComponent<RectTransform>();
        UpdateBarVisuals();
    }
    // Update your TakeDamage function to this:
    // Change this in PlayerHealth.cs
    public void TakeDamage(float amount, Vector2 attackerPos, Vector2 knockbackForce)
    {
        currentHealth -= (int)amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null) healthSlider.value = currentHealth;

        // Apply the knockback push
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        Debug.Log("Player Died!");
        // Add death logic here (e.g., Reload scene or Play animation)
    }

    // --- ADD THIS FUNCTION ---
    public void IncreaseMaxHealth(int increaseAmount)
    {
        maxHealth += increaseAmount;
        currentHealth += increaseAmount; // Also heals the player for the amount gained
        UpdateBarVisuals();
    }

    private void UpdateBarVisuals()
    {
        if (healthSlider != null && sliderRect != null)
        {
            // 1. Physically stretch the bar's width
            float newWidth = maxHealth * pixelsPerHealthPoint;
            sliderRect.sizeDelta = new Vector2(newWidth, sliderRect.sizeDelta.y);

            // 2. Update the slider's math range
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
}