using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;

    [Header("Dynamic Length")]
    public float pixelsPerHealthPoint = 1f; // Each 1 HP adds 1 pixel of width
    private RectTransform sliderRect;

    [Header("I-Frames")]
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    void Awake()
    {
        // 🛑 SAFETY EDIT: Remove "currentHealth = maxHealth" from here so it doesn't 
        // overwrite the custom stats injected by your LevelSpawner!
    }

    void Start()
    {
        // 🔥 THE LOGICAL SEARCH RESCUE:
        // If the prefab didn't cross scenes with a slider assigned, find it in the level canvas!
        if (healthSlider == null)
        {
            // Looks for the Slider script on your Canvas hierarchy objects
            healthSlider = FindFirstObjectByType<Slider>();
        }

        // Cache the RectTransform safely if the slider exists
        if (healthSlider != null)
        {
            sliderRect = healthSlider.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("PlayerHealth: No UI Slider component was found in the combat level scene layout!");
        }

        // Initialize values safely now that scene links are established
        UpdateBarVisuals();
    }

    public void TakeDamage(float amount, Vector2 attackerPos, Vector2 knockbackForce)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= (int)amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null) healthSlider.value = currentHealth;

        // Apply physical launch forces
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Clear existing velocity drift
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        }

        // =========================================================================
        // 🔥 THE VISUAL STAGGER & STATE LOCK:
        // Forcefully break the player out of movement/combat logic and snap their graphics
        // directly into the stagger state so they react visually to enemy impacts!
        // =========================================================================
        PlayerController controller = GetComponent<PlayerController>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();

        Animator anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        if (controller != null && anim != null)
        {
            // Lock out player controls by entering the stagger state
            controller.currentState = PlayerState.stagger;

            // Clear any active combo sequences running in the background
            PlayerCombo combo = GetComponent<PlayerCombo>();
            if (combo == null) combo = GetComponentInChildren<PlayerCombo>();
            if (combo != null) combo.FinishAttack();

            // Play your hit animation state immediately
            // Ensure you have an animation state in your graph named exactly "Hit"
            anim.SetBool("isHit", true);
            anim.SetBool("isMoving", false);
        }
        // =========================================================================

        if (currentHealth <= 0) Die();

        StartCoroutine(InvincibilityRoutine(controller));
    }

    // Updated recovery routine to hand back movement control frames cleanly
    private IEnumerator InvincibilityRoutine(PlayerController controller)
    {
        isInvincible = true;

        // How long the player is physically locked in the flinch/stagger state
        yield return new WaitForSeconds(0.2f);

        // 🔥 THE AUTOMATED VISUAL UNLOCKER:
        // Locate the active animator component layer and forcefully flip the 
        // 'isHit' bool flag back to FALSE! This lets the state machine leave 
        // the Hit state and blend back into your normal running motion loops.
        Animator anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        if (anim != null)
        {
            anim.SetBool("isHit", false);
        }

        // Restore manual control state mapping so they can run or dodge away!
        if (controller != null && currentHealth > 0)
        {
            controller.currentState = PlayerState.idle;
        }

        // Spend the remainder of the duration flashing or being invulnerable to multi-hits
        float remainingIFrameTime = invincibilityDuration - 0.2f;
        if (remainingIFrameTime > 0)
        {
            yield return new WaitForSeconds(remainingIFrameTime);
        }

        isInvincible = false;
    }

    // 🔥 THE RUNTIME PERMANENT STAT INCREASER:
    // Invoked by pickable boost objects mid-battle!
    // --- THE RUNTIME PERMANENT STAT INCREASER ---
    // Invoked by pickable boost objects mid-battle!
    public void PermanentHealthUpgrade(float boostAmount, float universalMaxMenuCap)
    {
        // 1. Permanently update the active container limits inside our running game scene
        maxHealth += Mathf.RoundToInt(boostAmount);
        currentHealth += Mathf.RoundToInt(boostAmount);

        // Cap health at your universal maximum limit if necessary
        if (maxHealth > universalMaxMenuCap) maxHealth = Mathf.RoundToInt(universalMaxMenuCap);
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        // 2. Push changes to the persistent character data file asset!
        CharacterData chosenChar = CharacterSelectManager.Instance?.GetSelectedCharacter();
        if (chosenChar != null)
        {
            // 🟢 FIXED: Line 151 is deleted! We no longer try to write to a read-only variable.
            // The permanent bonus points are already securely saved by the pickup script!
        }

        // 3. Stretch the UI bar layout instantly in real-time frame views
        UpdateBarVisuals();
    }

    void Die()
    {
        Debug.Log("Player Died!");
    }

    public void IncreaseMaxHealth(int increaseAmount)
    {
        maxHealth += increaseAmount;
        currentHealth += increaseAmount;
        UpdateBarVisuals();
    }

    // Call this public method from your LevelSpawner right after injecting stats!
    public void UpdateBarVisuals()
    {
        // Fallback catch if sliderRect hasn't been cached yet
        if (sliderRect == null && healthSlider != null)
        {
            sliderRect = healthSlider.GetComponent<RectTransform>();
        }

        if (healthSlider != null && sliderRect != null)
        {
            // 1. Physically stretch the bar's width based on your HP capacity
            float newWidth = maxHealth * pixelsPerHealthPoint;
            sliderRect.sizeDelta = new Vector2(newWidth, sliderRect.sizeDelta.y);

            // 2. Update the slider's math range
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    // 🔥 THE CHARACTER SELECT INJECTION BRIDGE:
    // Call this public method from your LevelSpawner script immediately on frame one!
    public void InitializeInjectedStats(float selectedMaxHealth, float universalMaxCap)
    {
        // 1. Map the custom whole-number stats directly to our runtime containers
        maxHealth = Mathf.RoundToInt(selectedMaxHealth);
        currentHealth = maxHealth;

        // 2. Safely locate component paths if they weren't assigned in the scene hierarchy yet
        if (healthSlider == null) healthSlider = FindFirstObjectByType<Slider>();
        if (healthSlider != null && sliderRect == null) sliderRect = healthSlider.GetComponent<RectTransform>();

        if (healthSlider != null)
        {
            // ❌ REMOVE OLD METHOD: healthSlider.maxValue = maxHealth;
            // This forces bars to look 100% full regardless of stats.

            // 🟢 UNIVERSAL MATH SCALE MATCH:
            // Lock the in-game health slider's physical maximum value to match your menu cap!
            healthSlider.maxValue = universalMaxCap;
            healthSlider.value = currentHealth;

            // 3. Dynamically resize the physical layout frame if you want longer bars for higher health
            if (sliderRect != null)
            {
                float newWidth = maxHealth * pixelsPerHealthPoint;
                sliderRect.sizeDelta = new Vector2(newWidth, sliderRect.sizeDelta.y);
            }
        }

        Debug.Log($"<color=#00FF7F>[HEALTH BRIDGE INITIALIZED]:</color> Player health synced! HP: {currentHealth}/{maxHealth} scaled against Cap: {universalMaxCap}");
    }
}