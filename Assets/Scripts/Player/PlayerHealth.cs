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

    [Header("🎨 Dynamic Health Bar Color Tuning")]
    [Tooltip("The default color of your health bar when you are in a safe condition.")]
    public Color healthyBarColor = Color.green;

    [Tooltip("The color the health bar changes to when dipping below the critical threshold.")]
    public Color criticalBarColor = Color.red;

    [Tooltip("The health percentage threshold (0.0 to 1.0) where the crisis color swaps over (e.g., 0.35 = 35% health remaining).")]
    [Range(0f, 1f)]
    public float criticalHealthThreshold = 0.35f;

    // Caches the underlying color fill image component from your health slider layout tree
    private UnityEngine.UI.Image healthFillImage;

    [Header("🎨 Dynamic HUD Layout Links")]
    private Text hudNameText;
    private UnityEngine.UI.Image hudCharacterIconImage;

    void Awake()
    {
        // 🛑 SAFETY EDIT: Remove "currentHealth = maxHealth" from here so it doesn't 
        // overwrite the custom stats injected by your LevelSpawner!
    }

    void Start()
    {
        // 🔥 THE SPECIFIC LOGICAL SEARCH RESCUE:
        // Instead of grabbing a random slider, search specifically for the object named "Health Slider"!
        if (healthSlider == null)
        {
            GameObject healthGo = GameObject.Find("Health Slider");
            if (healthGo != null)
            {
                healthSlider = healthGo.GetComponent<Slider>();
            }
        }

        // Cache the RectTransform safely if the slider exists
        if (healthSlider != null)
        {
            sliderRect = healthSlider.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("PlayerHealth: No object named 'Health Slider' was found in the combat level scene layout!");
        }
     // 🔥 THE AUTOMATED NAME DISCOVERY MATRIX:
        // Automatically search past background nodes to grab the active text element
        if (hudNameText == null)
        {
            GameObject nameGo = GameObject.Find("Name Text ");
            if (nameGo != null) 
            {
                hudNameText = nameGo.GetComponent<UnityEngine.UI.Text>();
            }
        }

        // 🔥 THE AUTOMATED THUMBNAIL PORTRAIT ROUTER:
        if (hudCharacterIconImage == null)
        {
            GameObject iconGo = GameObject.Find("Charecter Icon"); // Matches your exact spelling!
            if (iconGo != null) 
            {
                hudCharacterIconImage = iconGo.GetComponent<UnityEngine.UI.Image>();
            }
        }

        // Instantly force your data matrices to overwrite placeholder text cards
        SyncHUDProfileData();

        UpdateBarVisuals();

    }

    private void SyncHUDProfileData()
    {
        // Query the Selection Manager directly to find out who the player picked!
        CharacterData chosenChar = CharacterSelectManager.Instance?.GetSelectedCharacter();

        if (chosenChar != null)
        {
            // 1. Inject the literal warrior name string directly into your text asset box
            if (hudNameText != null)
            {
                hudNameText.text = chosenChar.characterName;
                Debug.Log($"<color=#00FFFF>[HUD INITIALIZED]:</color> Text box card successfully overwritten with officer name: <b>{chosenChar.characterName}</b>");
            }

            // 2. Extract your 'Grid Icon' sprite straight from your ScriptableObject layout!
            if (hudCharacterIconImage != null)
            {
                // 🟢 MATCHED: Targets the exact spelling of your inspector asset parameter
                hudCharacterIconImage.sprite = chosenChar.gridIcon;
                Debug.Log("<color=#00FF00>[HUD PORTRAIT SECURED]:</color> Target image placeholder replaced with officer sprite sheet card.");
            }
        }
        else
        {
            Debug.LogWarning("<color=red>[HUD SYSTEM FAILURE]:</color> Could not find any active character select data file to pull stats from!");
        }
    }

    public void TakeDamage(float amount, Vector2 attackerPos, Vector2 knockbackForce)
    {
     if (isInvincible || currentHealth <= 0) return;

        currentHealth -= (int)amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 🔥 RE-ROUTE: Force a complete layout check and color validation update loop pass!
        UpdateBarVisuals();

        if (healthSlider != null) healthSlider.value = currentHealth;

        // 🔥 THE COMBAT MATRIX LINK:
        // Automatically find your combo script and notify it that a hit landed!
        // This processes your 'musouGainPerHitTaken' calculation seamlessly.
        PlayerCombo combo = GetComponent<PlayerCombo>();
        if (combo == null) combo = GetComponentInChildren<PlayerCombo>();
        if (combo != null)
        {
            combo.NotifyPlayerTookDamage();
        }

        // Apply physical launch forces
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Clear existing velocity drift
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();

        Animator anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        if (controller != null && anim != null)
        {
            // Lock out player controls by entering the stagger state
            controller.currentState = PlayerState.stagger;

            // Clear any active combo sequences running in the background
            // 🟢 NOTE: This safely cancels normal swings, but your PlayerCombo script 
            // will automatically ignore this cancel if the Musou ultimate is running!
            if (combo != null) combo.FinishAttack();

            // Play your hit animation state immediately
            anim.SetBool("isHit", true);
            anim.SetBool("isMoving", false);
        }

        if (currentHealth <= 0) Die();

        StartCoroutine(InvincibilityRoutine(controller));
    }

    // Updated recovery routine to hand back movement control frames cleanly
    private IEnumerator InvincibilityRoutine(PlayerController controller)
    {
        isInvincible = true;

        // How long the player is physically locked in the flinch/stagger state
        yield return new WaitForSeconds(0.2f);

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

  public void PermanentHealthUpgrade(float boostAmount, float universalMaxMenuCap = 150f)
    {
        // 🌟 FORCE CAP RESCUE: Override incoming values to guarantee a maximum 150 cap ceiling
        universalMaxMenuCap = 150f;

        maxHealth += Mathf.RoundToInt(boostAmount);
        currentHealth += Mathf.RoundToInt(boostAmount);

        if (maxHealth >= universalMaxMenuCap) maxHealth = Mathf.RoundToInt(universalMaxMenuCap);
        if (currentHealth >= maxHealth) currentHealth = maxHealth;

        // Push changes to the persistent character data file asset
        CharacterData chosenChar = CharacterSelectManager.Instance?.GetSelectedCharacter();

        UpdateBarVisuals();
    }


    void Die()
    {
        Debug.Log("Player Died!");

        if (BattleEndManager.Instance != null)
        {
            StartCoroutine(BattleEndManager.Instance.DefeatSequenceCo());
        }
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
        // THE ULTIMATE OVERWRITE SHIELD:
        if (healthSlider != null && healthSlider.gameObject.name == "Musou Slider")
        {
            Debug.LogWarning("<color=orange>[HEALTH GUARD]:</color> Caught an external script trying to force the Musou Slider into PlayerHealth! Blocking overwrite.");
            healthSlider = null;
        }

        if (healthSlider == null)
        {
            GameObject healthGo = GameObject.Find("Health Slider");
            if (healthGo != null) healthSlider = healthGo.GetComponent<Slider>();
        }

        // Cache the RectTransform safely if the slider exists
        if (sliderRect == null && healthSlider != null)
        {
            sliderRect = healthSlider.GetComponent<RectTransform>();
        }

        // 🔥 AUTOMATED HEALTH FILL IMAGE TRACKER:
        // Automatically look past the slider's Background and capture the real visual Fill image!
        if (healthFillImage == null && healthSlider != null)
        {
            Transform fillTrans = healthSlider.transform.Find("Fill Area/Fill");
            if (fillTrans != null)
            {
                healthFillImage = fillTrans.GetComponent<UnityEngine.UI.Image>();
            }
        }

        if (healthSlider != null && sliderRect != null)
        {
            // 1. Physically stretch the bar's width based on your HP capacity
            float newWidth = maxHealth * pixelsPerHealthPoint;
            sliderRect.sizeDelta = new Vector2(newWidth, sliderRect.sizeDelta.y);

            // 2. Update the slider's math range
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;

            // 🔥 3. DYNAMIC COLOR SWAP LOGIC:
            if (healthFillImage != null && maxHealth > 0)
            {
                float currentHealthPct = (float)currentHealth / maxHealth;

                // Swap colors based on your customized Inspector threshold percentage bounds
                if (currentHealthPct <= criticalHealthThreshold)
                {
                    healthFillImage.color = criticalBarColor;
                }
                else
                {
                    healthFillImage.color = healthyBarColor;
                }
            }
        }
    }

    public void InitializeInjectedStats(float selectedMaxHealth, float universalMaxCap)
    {
        // 1. Map the custom whole-number stats directly to our runtime containers
        maxHealth = Mathf.RoundToInt(selectedMaxHealth);
        currentHealth = maxHealth;

        // 2. Safely locate targeted component paths if they weren't assigned in the scene hierarchy yet
        if (healthSlider == null)
        {
            GameObject healthGo = GameObject.Find("Health Slider");
            if (healthGo != null)
            {
                healthSlider = healthGo.GetComponent<Slider>();
            }
        }

        if (healthSlider != null && sliderRect == null) sliderRect = healthSlider.GetComponent<RectTransform>();

        if (healthSlider != null)
        {
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