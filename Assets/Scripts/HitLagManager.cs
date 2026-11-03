using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitLagManager : MonoBehaviour
{
    // 🟩 SINGLE-PROPERTY ARCHITECTURE: 
    // Combined everything into a single field using your exact name style!
    public static HitLagManager Instance { get; private set; }

    [Header("Global Hit-Lag Durations")]
    public float standardHitLagDuration = 0.06f;
    public float heavyHitLagDuration = 0.18f;

    private void Awake()
    {
        // Forcefully claim the master tracking pointer link on frame one.
        // This completely prevents the script from vanishing or deleting itself at runtime!
        Instance = this;
    }

    public void TriggerBasaraHitLag(Animator attackerAnim, Rigidbody2D attackerRb, List<MonoBehaviour> victims, float duration, Vector2 structuralKnockback = default)
    {
        StartCoroutine(BasaraHitLagRoutine(attackerAnim, attackerRb, victims, duration, structuralKnockback));
    }

    private IEnumerator BasaraHitLagRoutine(Animator attackerAnim, Rigidbody2D attackerRb, List<MonoBehaviour> victims, float duration, Vector2 structuralKnockback)
    {
        float originalAttackerSpeed = 1f;

        if (attackerAnim != null)
        {
            originalAttackerSpeed = attackerAnim.speed;
            if (originalAttackerSpeed <= 0.01f) originalAttackerSpeed = 1f;

            attackerAnim.speed = 0f;
        }

        Vector2 originalAttackerVelocity = attackerRb != null ? attackerRb.linearVelocity : Vector2.zero;
        if (attackerRb != null) attackerRb.linearVelocity = Vector2.zero;

        List<float> originalVictimSpeeds = new List<float>();
        List<Vector2> originalVictimVelocities = new List<Vector2>();
        List<Animator> victimAnims = new List<Animator>();
        List<Rigidbody2D> victimRbs = new List<Rigidbody2D>();

        foreach (MonoBehaviour victim in victims)
        {
            if (victim == null) continue;

            Animator vAnim = victim.GetComponent<Animator>();
            if (vAnim == null) vAnim = victim.GetComponentInChildren<Animator>();

            Rigidbody2D vRb = victim.GetComponent<Rigidbody2D>();
            if (vRb == null) vRb = victim.GetComponentInChildren<Rigidbody2D>();

            MusouUnit unit = victim.GetComponent<MusouUnit>();
            if (unit == null) unit = victim.GetComponentInParent<MusouUnit>();

            if (unit != null && unit.animator != null)
            {
                unit.animator.SetBool("isHit", true);
            }

            victimAnims.Add(vAnim);
            victimRbs.Add(vRb);

            originalVictimSpeeds.Add(vAnim != null ? vAnim.speed : 1f);

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
            float shakeMagnitude = (duration > 0.1f) ? 0.35f : 0.12f;

            if (structuralKnockback != Vector2.zero)
            {
                float punchDistance = (duration > 0.1f) ? 0.5f : 0.2f;
                CameraShake.Instance.HitPunch(structuralKnockback, punchDistance, duration + 0.05f);
            }
            else
            {
                CameraShake.Instance.Shake(duration + 0.05f, shakeMagnitude);
            }
        }

        yield return new WaitForSecondsRealtime(duration);

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
                victimRbs[i].linearVelocity = originalVictimVelocities[i];
            }
        }
    }
}