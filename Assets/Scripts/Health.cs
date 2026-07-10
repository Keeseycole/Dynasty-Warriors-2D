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

    
    [HideInInspector] public bool blockedOnThisFrame = false;

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

 

    public void TakeDamage(float damage, Vector2 attackerPosition, Vector2 knockback, Animator attackerAnim, Rigidbody2D attackerRb)
    {
       

        if (currentHealth <= 0) return;

        // ========================================================
        // 🔥 THE BLOCK INTERCEPTION SYSTEM:
        // Intercepts and completely mitigates damage if unit is holding up a guard!
        // ========================================================
        if (unitAI != null && unitAI.currentState == EnemyState.Block)
        {
            // Set the shield flag to true so the HitParticleManager drops regular flesh splashes!
            blockedOnThisFrame = true;

            if (isSimulating && SoundManager.Instance != null)
            {
                // Plays a crisp metallic blade clash sound instead of standard flesh slice impact
                SoundManager.Instance.PlaySFX("ShieldBlock", 0.8f, 0.05f);
            }

            if (HitParticleManager.Instance != null)
            {
                // Calculate point of contact slightly out in front of the defender's center pivot
                Vector2 attackDirection = ((Vector2)transform.position - attackerPosition).normalized;
                Vector2 blockImpactPoint = (Vector2)transform.position - (attackDirection * 0.4f);

                // Calls the upgraded manager using the custom Block enum for custom metal sparks & reverse shard spray
                HitParticleManager.Instance.SpawnHitSparkUniversal(blockImpactPoint, HitParticleManager.AttackType.Block, attackDirection);
            }

            // Perfect Block mitigation (Change to 'damage *= 0.1f;' if you want 10% chip damage instead)
            damage = 0f;

            // Safely exit the method right here! This prevents any health reductions, hit-lag, audio triggers, 
            // hit flash coroutines, or knockback physical trajectory pushes from evaluating.
            return;
        }
 

        // Standard raw damage calculations proceed cleanly if they were caught off-guard
        currentHealth -= damage;

        // ========================================================
        // OPTIMIZED HIT-LAG GATE: Only freeze physics and animation timelines 
        // if the battle is actively simulating on-screen near the player view!
        // ========================================================
        if (isSimulating && HitLagManager.Instance != null && attackerAnim != null)
        {
            if (!attackerAnim.CompareTag("Player"))
            {
                List<global::UnityEngine.MonoBehaviour> victimsList = new List<global::UnityEngine.MonoBehaviour> { this };
                bool isHeavy = (damage > 12f || knockback.magnitude > 1.2f);
                float selectedDuration = isHeavy ? HitLagManager.Instance.heavyHitLagDuration : HitLagManager.Instance.standardHitLagDuration;

                HitLagManager.Instance.TriggerBasaraHitLag(attackerAnim, attackerRb, victimsList, selectedDuration);
            }
        }
  

        Debug.Log($"{gameObject.name} took damage! Current health after hit: {currentHealth}");

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);

        // ========================================================
        // THE AUDIO CULLING GATE: Completely silences clashing impact 
        // sounds when units take damage out of the camera view range.
        // ========================================================
        if (isSimulating && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("HitImpact", 0.7f);

            if (damage > 12f)
            {
                SoundManager.Instance.PlaySFX("HeavyImpact", 1f);
            }
        }
        // ========================================================

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
            // Culled Units: Still blink on the minimap radar map layout when damaged!
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
    }

    // Inside Health.cs -> DeathFadeRoutine()
    // --- INSIDE HEALTH.CS ---

    // Inside Health.cs -> Die()

    void Die()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.activeUnits.Remove(this);

        // 🔥 THE TUG-OF-WAR FEEDBACK:
        // If an enemy dies, give the Player Side morale points. If an ally dies, give the Enemy Side points!
        if (MoraleManager.Instance != null && unitAI != null)
        {
            // Grunts grant 0.25f points. If it's an Officer, give them a massive 8.0f swing boost!
            float pointsGranted = unitAI.isOfficer ? 8f : 0.25f;

            // The surviving opposing faction claims the tactical point boost
            MusouUnit.Team victoriousTeam = (unitAI.unitTeam == MusouUnit.Team.PlayerSide) ? MusouUnit.Team.EnemySide : MusouUnit.Team.PlayerSide;

            MoraleManager.Instance.ChangeMorale(victoriousTeam, pointsGranted);
        }

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

        if (KOCounter.instance != null) KOCounter.instance.AddKO();

        StartCoroutine(DeathFadeRoutine());
    }

    private IEnumerator DeathFadeRoutine()
    {
        yield return new WaitForSeconds(10f);

        if (spriteRenderer != null)
        {
            float fadeTime = 1f;
            float startAlpha = spriteRenderer.color.a;

            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                if (spriteRenderer == null) yield break;
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(startAlpha, 0f, t / fadeTime);
                spriteRenderer.color = c;
                yield return null;
            }
        }

        // =========================================================================
        // 🔥 THE COMPREHENSIVE SAFETIED DESTRUCTION BLOCK:
        // This stops Unity from accidentally destroying your parent 'Unit Sword' 
        // container asset while other living troops are still nesting inside it!
        // =========================================================================
        if (gameObject.CompareTag("Gate"))
        {
            gameObject.SetActive(false); // Safe disable for structural level assets
            Debug.Log($"[BATTLEFIELD] {gameObject.name} breached! Path cleared.");
        }
        else
        {
            // ⚠️ CRITICAL FIX: Only destroy THIS specific character's GameObject.
            // DO NOT call Destroy(transform.parent.gameObject) here or in your BattleEventManager!
            // This ensures the empty parent folder container stays intact for the survivors.
            Destroy(gameObject);
        }
        // =========================================================================
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

    private void OnDestroy()
    {
        // 🔥 THE CRITICAL UN-LINK: If this unit vanishes from the scene, 
        // it must strip itself from the global lists instantly so other systems don't look for it!
        if (BattleManager.Instance != null && BattleManager.Instance.activeUnits.Contains(this))
        {
            BattleManager.Instance.activeUnits.Remove(this);
        }

        // Forcefully drop any tokens this physical unit was holding
        if (AttackDirector.instance != null)
        {
            AttackDirector.instance.ForceReleaseAllTokensForAttacker(GetComponent<MusouUnit>());
        }
    }
}