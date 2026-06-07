using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusouRam : MonoBehaviour
{
    [Header("Ram Settings")]
    public float damagePerHit = 50f;
    public float attackCooldown = 2.0f;

    private Health targetGateHealth;
    private Coroutine rammingCoroutine;
    private bool reportedDestruction = false; // Prevents double-reporting bugs on death frames

    // Cached references for hit-lag support
    private Animator ramAnimator;
    private Rigidbody2D rb;

    private void Awake()
    {
        // Cache these components on startup so we can cleanly feed them to the damage system
        ramAnimator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // --- HEALTH CHECK FOR ENEMY RAM DESTRUCTION ---
        Health myHealth = GetComponent<Health>();
        if (myHealth != null && myHealth.currentHealth <= 0 && !reportedDestruction)
        {
            reportedDestruction = true;
            StopRamming();

            this.enabled = false; // Gracefully shut down this script component
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Gate"))
        {
            Health gate = other.GetComponent<Health>();
            if (gate != null)
            {
                targetGateHealth = gate;

                if (rammingCoroutine == null)
                {
                    rammingCoroutine = StartCoroutine(RepeatedRammingRoutine());
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Gate"))
        {
            StopRamming();
        }
    }

    private IEnumerator RepeatedRammingRoutine()
    {
        while (targetGateHealth != null && targetGateHealth.currentHealth > 0)
        {
            if (ramAnimator != null)
            {
                // 1. Tell the animator to play the slam sequence
                ramAnimator.SetTrigger("SlamRam");
            }

            // 2. Simply wait out the cooldown block before prompting the next slam animation frame
            yield return new WaitForSeconds(attackCooldown);
        }

        StopRamming();
    }

    // --- ANIMATION EVENT TARGET METHOD ---
    // Make this public so the Unity Animation timeline can see it!
    public void DealRamDamageEvent()
    {
        if (targetGateHealth != null && targetGateHealth.currentHealth > 0)
        {
            Debug.Log($"[RAM EVENT] Impact frame reached! Dealing {damagePerHit} damage.");

            // FIXED LINE: Added 'ramAnimator' and 'rb' to satisfy the updated 5-argument method signature!
            // This means when the ram hits a massive wooden gate, the ram itself will freeze frame slightly on impact, selling the heavy weight!
            targetGateHealth.TakeDamage(damagePerHit, transform.position, Vector2.zero, ramAnimator, rb);
        }
    }

    private void StopRamming()
    {
        if (rammingCoroutine != null)
        {
            StopCoroutine(rammingCoroutine);
            rammingCoroutine = null;
        }
        targetGateHealth = null;
    }
}