using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusouRam : MonoBehaviour
{
    [Header("Ram Settings")]
    public float damagePerHit = 50f;
    public float attackCooldown = 2.0f;

    // --- TRACKING QUEUE ---
    // Dynamic buffer list to track multiple active gate collisions simultaneously without events
    private List<Health> nearbyGates = new List<Health>();
    private Health currentTargetGate;

    private Health myHealth;
    private Coroutine rammingCoroutine;
    private bool reportedDestruction = false;

    private Animator ramAnimator;
    private Rigidbody2D rb;

    [Header("Breach Dialog Configuration")]
    [Tooltip("Assign a ScriptableObject line asset here to play dialogue when this gate goes down!")]
    public DialogData gateBreachedDialog;

    private void Awake()
    {
        ramAnimator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        myHealth = GetComponent<Health>();
    }

    private void Update()
    {
        // Continuous health evaluation check for the ram itself
        if (myHealth != null && myHealth.currentHealth <= 0 && !reportedDestruction)
        {
            HandleRamDestroyed();
        }

        // Clean up broken gates and check targets on every frame
        EvaluateTargetQueue();
    }

    private void HandleRamDestroyed()
    {
        reportedDestruction = true;
        StopRamming();
        this.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (reportedDestruction) return;

        if (other.CompareTag("Gate"))
        {
            Health gate = other.GetComponent<Health>();
            if (gate != null && !nearbyGates.Contains(gate))
            {
                nearbyGates.Add(gate);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Gate"))
        {
            Health gate = other.GetComponent<Health>();
            if (gate != null && nearbyGates.Contains(gate))
            {
                nearbyGates.Remove(gate);
                if (currentTargetGate == gate)
                {
                    currentTargetGate = null;
                }
            }
        }
    }

    private void EvaluateTargetQueue()
    {
        if (reportedDestruction) return;

        // Clean up empty objects or shattered gates from the master list tracking slot
        nearbyGates.RemoveAll(g => g == null || g.currentHealth <= 0 || !g.gameObject.activeInHierarchy);

        // If our current target died or left the trigger area, pick the next gate in line
        if (currentTargetGate == null || currentTargetGate.currentHealth <= 0 || !currentTargetGate.gameObject.activeInHierarchy)
        {
            currentTargetGate = null;

            if (nearbyGates.Count > 0)
            {
                currentTargetGate = nearbyGates[0];

                // Kick off the loop sequence if it's currently dormant
                if (rammingCoroutine == null)
                {
                    rammingCoroutine = StartCoroutine(RepeatedRammingRoutine());
                }
            }
            else
            {
                StopRamming();
            }
        }
    }

    private IEnumerator RepeatedRammingRoutine()
    {
        while (currentTargetGate != null && currentTargetGate.currentHealth > 0 && currentTargetGate.gameObject.activeInHierarchy)
        {
            if (ramAnimator != null)
            {
                ramAnimator.SetTrigger("SlamRam");
            }

            yield return new WaitForSeconds(attackCooldown);
        }

        rammingCoroutine = null;
    }

    // --- ANIMATION TIMELINE EVENT TARGET METHOD ---
    public void DealRamDamageEvent()
    {
        if (reportedDestruction) return;

        if (currentTargetGate != null && currentTargetGate.currentHealth > 0)
        {
            currentTargetGate.TakeDamage(damagePerHit, transform.position, Vector2.zero, ramAnimator, rb);
        }
    }

    private void StopRamming()
    {
        if (rammingCoroutine != null)
        {
            StopCoroutine(rammingCoroutine);
            rammingCoroutine = null;
        }

        if (ramAnimator != null)
        {
            ramAnimator.ResetTrigger("SlamRam");
        }

        nearbyGates.Clear();
        currentTargetGate = null;
    }
}