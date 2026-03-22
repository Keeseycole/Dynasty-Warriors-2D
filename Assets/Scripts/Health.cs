using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public float  maxHealth;
    public FloatingHealthBar healthBar;

    public CharacterData stats; // Drag your ScriptableObject here

    public float currentHealth;

    private Animator anim;
    private Rigidbody2D rb;

    // UPDATED: Now points to the unified script
    private MusouUnit unitAI;

    [Tooltip("True = Active/Near Player, False = Culled/Far away")]
    public bool isSimulating = true;

    public SpriteRenderer minimapIconRenderer;
   

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    public GameObject flashOverlay;

    private RectTransform sliderRect;

    private void Awake()
    {
       

        unitAI = GetComponent<MusouUnit>(); // Updated Reference
        rb = GetComponent<Rigidbody2D>();

        // Find animator in children because of your 'Visuals' object
        anim = GetComponentInChildren<Animator>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            Debug.LogError("SpriteRenderer still not found on " + gameObject.name + " or its children!");
        }

        if (flashOverlay != null)
        {
            SpriteRenderer mainRenderer = GetComponentInChildren<SpriteRenderer>();
            SpriteRenderer overlayRenderer = flashOverlay.GetComponent<SpriteRenderer>();

            if (mainRenderer != null && overlayRenderer != null)
            {
                // This ensures the flash ALWAYS matches the enemy's current sprite
                overlayRenderer.sprite = mainRenderer.sprite;

                // Set the flash color (Solid White or Yellow)
                overlayRenderer.color = Color.white;

                // Make sure the flash is initially hidden
                flashOverlay.SetActive(false);
            }
        }
    }

    void Start()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.activeUnits.Add(this);
    }

    public void TakeDamage(float damage, Vector2 attackerPosition, Vector2 knockback)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;

        // This is the missing link!
        if (healthBar != null)
        {
            healthBar.UpdateBar(currentHealth, maxHealth);
        }

        if (isSimulating)
        {
            // APPLY THE KNOCKBACK
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Impulse is best for sudden "hit" forces
                rb.AddForce(knockback, ForceMode2D.Impulse);
            }

            if (unitAI != null)
            {
               unitAI.TriggerHit(attackerPosition);
            }
    
    

            StartCoroutine(HitlagRoutine(0.05f));

            // Trigger the flash!
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }
        else
        {
            // If culled, still flash the minimap to show a fight is happening
            if (flashCoroutine == null) flashCoroutine = StartCoroutine(MinimapFlashTick());
        }

        // 4. DEATH CHECK
        if (currentHealth <= 0)
        {
            anim.SetBool("isHit", false);
            anim.SetBool("isBlocking", false);
            anim.SetBool("isDead", true);
            Die();
        }
    }
      void Die()
    {
        // 1. Unregister from the battle manager immediately
        if (BattleManager.Instance != null)
            BattleManager.Instance.activeUnits.Remove(this);

        // 2. Shut down the AI and Physics so the "ghost" doesn't keep fighting
        if (unitAI != null)
        {
            unitAI.StopAllCoroutines();
            unitAI.enabled = false; // Turn off the brain
            unitAI.ChangeState(EnemyState.Death); // If you have a death state
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Stop physics collisions
        }

        if (KOCounter.instance != null)
        {
            KOCounter.instance.AddKO();
        }
        // 4. Start the fade-out effect
        StartCoroutine(DeathFadeRoutine());
    }

    private IEnumerator DeathFadeRoutine()
    {
        // Wait a moment for the death animation to play (e.g., falling over)
        yield return new WaitForSeconds(1f);

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            float fadeTime = 1f;
            float startAlpha = sr.color.a;

            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(startAlpha, 0, t / fadeTime);
                sr.color = c;
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private IEnumerator MinimapFlashTick()
    {
        if (minimapIconRenderer == null) yield break;
        Color originalColor = minimapIconRenderer.color;
        minimapIconRenderer.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        minimapIconRenderer.color = originalColor;
        flashCoroutine = null;
    }

    private IEnumerator HitlagRoutine(float duration)
    {
        if (anim == null || rb == null) yield break;
        float originalSpeed = anim.speed;
        anim.speed = 0;
        yield return new WaitForSeconds(duration);
        anim.speed = originalSpeed;
    }

    private IEnumerator HitFlashRoutine()
    {
        Debug.Log("Flash Started on " + gameObject.name); // Check your Console for this!
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.2f); // Longer for testing

        spriteRenderer.color = originalColor;
        Debug.Log("Flash Ended");
    }

  
}