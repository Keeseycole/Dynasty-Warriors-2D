using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ComboState
{
    None,
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5
}

public class PlayerCombo : MonoBehaviour
{
    PlayerState playerState;
    PlayerController playerController;
    private CharecterAnimations attackAnim;
    private Rigidbody2D rb;
    public Animator myNativeAnimator;

    private bool ActivateResetTimer;
    private float defultComboTimer = .6f;
    private float currentComboTimer;
    private ComboState currentComboState;

    public bool isAttacking;

    [Header("Debug Live Feeds")]
    [Tooltip("Watch this value live while hitting enemies. It will snap to 0 on impact!")]
    public float liveAnimatorSpeedTracker;

    [Header("Attack Movement")]
    public float basicStepForce = 3f;
    public float finisherStepForce = 7f;

    [Header("Combat Tuning")]
    public float attackRange = 1.5f;
    public float finisherRangeMultiplier = 1.66f;
    public float basicHitLagDuration = 0.08f;
    public float finisherHitLagDuration = 0.15f;

    void Awake()
    {
        attackAnim = GetComponent<CharecterAnimations>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        myNativeAnimator = GetComponent<Animator>();
        if (myNativeAnimator == null)
        {
            myNativeAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        currentComboTimer = defultComboTimer;
        currentComboState = ComboState.None;
    }

    void Update()
    {
        if (myNativeAnimator != null)
        {
            liveAnimatorSpeedTracker = myNativeAnimator.speed;
        }

        if (myNativeAnimator != null && myNativeAnimator.speed == 0f)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        ComboAttacks();
        ResetComboState();
    }

    public void ComboAttacks()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SoundManager.Instance.PlaySFX("swordswing", 0.8f, 0.05f);

            if (currentComboState == ComboState.Attack5) return;

            currentComboState++;
            ActivateResetTimer = true;
            currentComboTimer = defultComboTimer;
            isAttacking = true;

            float force = (currentComboState == ComboState.Attack5) ? finisherStepForce : basicStepForce;
            Vector2 stepDir = playerController.lastLookDir;

            if (rb != null)
            {
                rb.AddForce(stepDir * force, ForceMode2D.Impulse);
            }

            switch (currentComboState)
            {
                case ComboState.Attack1: attackAnim.Attack1(); break;
                case ComboState.Attack2: attackAnim.Attack2(); break;
                case ComboState.Attack3: attackAnim.Attack3(); break;
                case ComboState.Attack4: attackAnim.Attack4(); break;
                case ComboState.Attack5: attackAnim.Attack5(); break;
            }

        }
    }

    public void ResetComboState()
    {
        if (ActivateResetTimer)
        {
            currentComboTimer -= Time.deltaTime;

            if (currentComboTimer <= 0)
            {
                currentComboState = ComboState.None;
                ActivateResetTimer = false;
                currentComboTimer = defultComboTimer;
                isAttacking = false;
            }
        }
    }

    public void FinishAttack()
    {
        isAttacking = false;
    }

    public void CheckForHit()
    {
        float currentRange = attackRange;
        float damage = 10f;
        float knockbackForce = .5f;

        bool isFinisher = (currentComboState == ComboState.Attack5);

        if (isFinisher)
        {
            currentRange = attackRange * finisherRangeMultiplier;
            damage = 13f;
            knockbackForce = 1.5f;
        }

        LayerMask enemyLayer = LayerMask.GetMask("Enemy");
        Vector2 attackDir = playerController.lastLookDir;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;

        // --- DEBUG LOG 1 ---
        Debug.Log($"[COMBAT TRACE] Checking for hits at {attackPos} with range {currentRange} on layer mask '{enemyLayer.value}'");

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, currentRange, enemyLayer);

        // --- DEBUG LOG 2 ---
        Debug.Log($"[COMBAT TRACE] Physics overlap found {hits.Length} colliders on the Enemy layer.");

        List<MonoBehaviour> victimsThisFrame = new List<MonoBehaviour>();

        foreach (Collider2D enemy in hits)
        {
            Health enemyHealth = enemy.GetComponent<Health>();

            if (enemyHealth == null)
            {
                // --- DEBUG LOG 3 ---
                Debug.LogWarning($"[COMBAT TRACE] Found object '{enemy.gameObject.name}', but it is missing a Health component!");
                continue;
            }

            if (enemyHealth.currentHealth <= 0)
            {
                // --- DEBUG LOG 4 ---
                Debug.Log($"[COMBAT TRACE] Found '{enemy.gameObject.name}', but they are already dead (Health <= 0).");
                continue;
            }

            Vector2 dir = (enemy.transform.position - transform.position).normalized;

            Vector2 resultingForce = dir * knockbackForce;
            if (!isFinisher)
            {
                Vector2 pullVector = (attackPos - (Vector2)enemy.transform.position).normalized;
                resultingForce = (dir + pullVector * 0.8f).normalized * knockbackForce;
            }

            enemyHealth.TakeDamage(damage, transform.position, resultingForce, myNativeAnimator, rb);
            victimsThisFrame.Add(enemyHealth);

            if (HitParticleManager.Instance != null)
            {
                Vector2 sparkPos = Vector2.Lerp(enemy.transform.position, transform.position, 0.2f);

                // --- DEBUG LOG 5 ---
                Debug.Log($"[COMBAT TRACE] Attempting to spawn particle spark via HitParticleManager at {sparkPos}");

                HitParticleManager.Instance.SpawnHitSpark(sparkPos, isFinisher, attackDir);
            }
            else
            {
                // --- DEBUG LOG 6 ---
                Debug.LogError("[COMBAT TRACE] HitParticleManager.Instance is NULL! Is the script active on an object in your scene?");
            }
        }

        if (victimsThisFrame.Count > 0 && HitLagManager.Instance != null)
        {
            float hitStopDuration = isFinisher ?
                HitLagManager.Instance.heavyHitLagDuration :
                HitLagManager.Instance.standardHitLagDuration;

            Vector2 combinedStructuralKnockback = attackDir * (isFinisher ? 14f : 5f);

            HitLagManager.Instance.TriggerBasaraHitLag(
                myNativeAnimator,
                rb,
                victimsThisFrame,
                hitStopDuration,
                combinedStructuralKnockback
            );

            if (CameraShake.Instance != null)
            {
                if (isFinisher) CameraShake.Instance.HitPunch(attackDir, 0.6f, hitStopDuration + 0.08f);
                else CameraShake.Instance.HitPunch(attackDir, 0.2f, hitStopDuration + 0.04f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerController == null) return;
        Gizmos.color = Color.red;
        Vector2 attackDir = playerController.lastLookDir;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;
        float finalRange = (currentComboState == ComboState.Attack5) ? (attackRange * finisherRangeMultiplier) : attackRange;
        Gizmos.DrawWireSphere(attackPos, finalRange);
    }
}