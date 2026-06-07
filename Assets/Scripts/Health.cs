using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class Health : MonoBehaviour
{
    public float maxHealth;
    public FloatingHealthBar healthBar;
    public CharacterData stats;

    public float currentHealth;

    private Animator anim;
    private Rigidbody2D rb;
    private MusouUnit unitAI;

    [Tooltip("True = Active/Near Player, False = Culled/Far away")]
    public bool isSimulating = true;

    public SpriteRenderer minimapIconRenderer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // FIX: Separated coroutine trackers so they don't cancel each other out!
    private Coroutine minimapFlashCoroutine;
    private Coroutine hitFlashCoroutine;

    private Collider2D myCollider;
    public GameObject flashOverlay;

    public bool isGate;
    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        unitAI = GetComponent<MusouUnit>();
        rb = GetComponent<Rigidbody2D>();
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
                overlayRenderer.sprite = mainRenderer.sprite;
                overlayRenderer.color = Color.white;
                flashOverlay.SetActive(false);
            }
        }
    }

    void Start()
    {
        currentHealth = maxHealth; // Ensure health is fully initialized
        if (BattleManager.Instance != null)
            BattleManager.Instance.activeUnits.Add(this);
    }

    // FIXED: Updated header to accept 5 arguments from your player's combat script!
    public void TakeDamage(float damage, Vector2 attackerPosition, Vector2 knockback, Animator attackerAnim, Rigidbody2D attackerRb)
    {
        if (currentHealth <= 0) return;
        currentHealth -= damage;

        // ========================================================
        // FIXED HIT-LAG NESTING: Only fire local hit-lag if this unit is attacking 
        // another NPC off-screen (where attackerAnim != null and it's NOT the player)
        // ========================================================
        if (HitLagManager.Instance != null && attackerAnim != null)
        {
            // If an external system called this without a group list, run a single freeze
            if (!attackerAnim.CompareTag("Player"))
            {
                List<global::UnityEngine.MonoBehaviour> victimsList = new List<global::UnityEngine.MonoBehaviour> { this };
                bool isHeavy = (damage > 12f || knockback.magnitude > 1.2f);
                float selectedDuration = isHeavy ? HitLagManager.Instance.heavyHitLagDuration : HitLagManager.Instance.standardHitLagDuration;

                HitLagManager.Instance.TriggerBasaraHitLag(attackerAnim, attackerRb, victimsList, selectedDuration);
            }
        }
        // ========================================================

        Debug.Log($"{gameObject.name} took damage! Current health after hit: {currentHealth}");

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
        SoundManager.Instance.PlaySFX("HitImpact", 0.7f);

        if (damage > 12f) // Tuned down to match your player's damage outputs
        {
            SoundManager.Instance.PlaySFX("HeavyImpact", 1f);
        }

        // 4. COMBAT FEEDBACK & SIMULATION
        if (isSimulating)
        {
            if (unitAI != null)
            {
                unitAI.TriggerHit(attackerPosition);
            }

            // Delayed knockback so the unit flies backward AFTER the hit-lag freeze frame ends
            if (rb != null)
            {
                StartCoroutine(DelayedKnockbackRoutine(knockback));
            }

            // Visual Flash Trackers
            if (minimapFlashCoroutine == null)
            {
                minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
            }

            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }
        else
        {
            if (minimapFlashCoroutine == null)
            {
                minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
            }
        }

        // 5. DEATH CHECK & CLEANUP
        if (currentHealth <= 0)
        {
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            if (spriteRenderer != null) spriteRenderer.color = originalColor; // Reset tint

            if (!isGate)
            {
                anim.SetBool("isHit", false);
                anim.SetBool("isBlocking", false);
                anim.SetBool("isDead", true);
            }
            Die();
        }


    void Die()
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.activeUnits.Remove(this);

            if (unitAI != null)
            {
                unitAI.StopAllCoroutines();
                unitAI.enabled = false;
                unitAI.ChangeState(EnemyState.Death);
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }

            if (KOCounter.instance != null)
            {
                KOCounter.instance.AddKO();
            }

            StartCoroutine(DeathFadeRoutine());
        }
    }

    // Inside Health.cs -> DeathFadeRoutine()
    private IEnumerator DeathFadeRoutine()
    {
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

        // FIX: Look explicitly for the Gate tag now!
        if (gameObject.CompareTag("Gate"))
        {
            gameObject.SetActive(false); // Safe disable for structures
            Debug.Log($"[BATTLEFIELD] {gameObject.name} breached! Path cleared.");
        }
        else
        {
            Destroy(gameObject); // Permanent removal for standard grunt units
        }
    }
   

    private IEnumerator MinimapFlashTick()
    {
        if (minimapIconRenderer == null) yield break;

        Color teamColor = minimapIconRenderer.color;
        float timer = Random.Range(0f, 2f);
        float pulseDuration = 1.2f;

        while (unitAI != null && unitAI.currentTarget != null && currentHealth > 0)
        {
            timer += Time.deltaTime;
            float t = (Mathf.Sin(timer * (Mathf.PI * 2) / pulseDuration) + 1f) / 2f;
            minimapIconRenderer.color = teamColor + (Color.yellow * t * 0.5f);
            yield return null;
        }

        minimapIconRenderer.color = teamColor;
        minimapFlashCoroutine = null;
    }

    public void SetCulling(bool isVisible)
    {
        isSimulating = isVisible;
        spriteRenderer.enabled = isVisible;
        myCollider.enabled = isVisible;

        if (minimapIconRenderer != null)
        {
            minimapIconRenderer.enabled = true;
        }
    }


    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;

        hitFlashCoroutine = null;
    }

    void OnEnable()
    {
        if (unitAI != null && unitAI.currentTarget != null && minimapFlashCoroutine == null)
        {
            minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
        }
    }

    // 2. ADD this new helper Coroutine to the bottom of your Health.cs script:
    private IEnumerator DelayedKnockbackRoutine(Vector2 knockbackForce)
    {
        // Wait until the end of the current frame so the Animator has 
        // evaluated and locked into its new "Hit" sprite state.
        yield return new WaitForEndOfFrame();

        // If the hitlag manager froze our physics velocity to zero, 
        // we must wait until the hitlag freeze window finishes completely 
        // before executing our launch trajectory!
        while (anim != null && anim.speed == 0)
        {
            yield return null;
        }

        // Now unleash the explosive Basara knockback push!
        if (rb != null && currentHealth > 0)
        {
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        }
    }
}