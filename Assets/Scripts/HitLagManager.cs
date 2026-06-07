using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitLagManager : MonoBehaviour
{
    public static HitLagManager Instance { get; private set; }

    [Header("Global Hit-Lag Durations")]
    [Tooltip("How long the game freezes for a standard sword slice (Classic: 0.04 to 0.06)")]
    public float standardHitLagDuration = 0.06f;

    [Tooltip("How long the game freezes for a massive combo finisher or Musou attack")]
    public float heavyHitLagDuration = 0.18f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Unified Basara-style hit-lag that handles any mix of players and enemies,
    /// with built-in explosive release physics and camera shake hooks.
    /// </summary>
    public void TriggerBasaraHitLag(Animator attackerAnim, Rigidbody2D attackerRb, List<MonoBehaviour> victims, float duration, Vector2 structuralKnockback = default)
    {
        StartCoroutine(BasaraHitLagRoutine(attackerAnim, attackerRb, victims, duration, structuralKnockback));
    }

    private IEnumerator BasaraHitLagRoutine(Animator attackerAnim, Rigidbody2D attackerRb, List<MonoBehaviour> victims, float duration, Vector2 structuralKnockback)
    {
        // 1. FREEZE ATTACKER (HARD LOCK)
        float originalAttackerSpeed = 1f;

        if (attackerAnim != null)
        {
            originalAttackerSpeed = attackerAnim.speed;
            if (originalAttackerSpeed <= 0.01f) originalAttackerSpeed = 1f;

            attackerAnim.speed = 0f;
        }

        Vector2 originalAttackerVelocity = attackerRb != null ? attackerRb.linearVelocity : Vector2.zero;
        if (attackerRb != null) attackerRb.linearVelocity = Vector2.zero;

        // 2. CACHE & FREEZE ALL VICTIMS
        List<float> originalVictimSpeeds = new List<float>();
        List<Vector2> originalVictimVelocities = new List<Vector2>();
        List<Animator> victimAnims = new List<Animator>();
        List<Rigidbody2D> victimRbs = new List<Rigidbody2D>();

        foreach (MonoBehaviour victim in victims)
        {
            if (victim == null) continue;

            Animator vAnim = victim.GetComponentInChildren<Animator>();
            Rigidbody2D vRb = victim.GetComponent<Rigidbody2D>();

            // Put them into their Stagger state immediately so they don't break out into walking on release
            MusouUnit unit = victim as MusouUnit;
            if (unit != null)
            {
                unit.animator.SetBool("isHit", true);
                // Tip: set state to Stagger here if you track an enum state machine
            }

            victimAnims.Add(vAnim);
            victimRbs.Add(vRb);

            originalVictimSpeeds.Add(vAnim != null ? vAnim.speed : 1f);

            // BASARA TWEAK: If we are passing an explicit knockback force, we don't want to preserve
            // their old walking velocity. We want them to explode outward.
            if (structuralKnockback != Vector2.zero)
            {
                originalVictimVelocities.Add(structuralKnockback);
            }
            else
            {
                originalVictimVelocities.Add(vRb != null ? vRb.linearVelocity : Vector2.zero);
            }

            if (vAnim != null) vAnim.speed = 0f;
            if (vRb != null) vRb.linearVelocity = Vector2.zero;
        }

        if (CameraShake.Instance != null && victims.Count > 0)
        {
            // Scale shake intensity depending on how long the freeze lasts
            float shakeMagnitude = (duration > 0.1f) ? 0.35f : 0.12f;

            if (structuralKnockback != Vector2.zero)
            {
                // BASARA EXTRA CRUNCH: Punch the camera forward in the exact direction the enemies fly!
                float punchDistance = (duration > 0.1f) ? 0.5f : 0.2f;
                CameraShake.Instance.HitPunch(structuralKnockback, punchDistance, duration + 0.05f);
            }
            else
            {
                // Fallback to a standard screen rumble if no directional force was passed
                CameraShake.Instance.Shake(duration + 0.05f, shakeMagnitude);
            }
        }

        // 4. HOLD (Bypasses regular game time scales completely)
        yield return new WaitForSecondsRealtime(duration);

        // 5. UNFREEZE EVERYONE (HARD RELEASE + EXPLOSIVE MOMENTUM)
        if (attackerAnim != null)
        {
            attackerAnim.speed = originalAttackerSpeed;
        }
        if (attackerRb != null) attackerRb.linearVelocity = originalAttackerVelocity;

        for (int i = 0; i < victimAnims.Count; i++)
        {
            if (victimAnims[i] != null)
            {
                victimAnims[i].speed = (originalVictimSpeeds[i] <= 0.01f) ? 1f : originalVictimSpeeds[i];
            }

            if (victimRbs[i] != null)
            {
                // Assign the final force. If a knockback vector was supplied, they fly back instantly on this frame!
                victimRbs[i].linearVelocity = originalVictimVelocities[i];
            }
        }
    }
}