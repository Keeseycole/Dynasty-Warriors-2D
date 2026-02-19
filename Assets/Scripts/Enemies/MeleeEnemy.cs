using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Random = UnityEngine.Random;

public class MeleeEnemy : sleepEnemy
{

    private List<IEnumerator> ComboChains;

    [SerializeField] bool comboStarted = false;

    public float strafeMagnitude = 1f; // How far to strafe sideway

    public float strafeDuration = 1.5f;

    public float strafeSpeed;

    public float strafeTimer = 2f;

    private int strafeDirection = 1; // 1 for right, -1 for left

    public int randomValue;

    public bool chooseCombo;

    public bool isStrafing;

    private float chooseTimer;

    // Store a reference to the coroutine so we can stop it later
private Coroutine brainRoutine;


    private void Update()
    {
        ComboChains = new List<IEnumerator>
        {
            Combo1(),
            Combo2(),
            Combo3(),

        };

        strafeTimer -= Time.deltaTime;

        chooseTimer -= Time.deltaTime;

    }

    public override void CheckDistance()
    {
        float currentDist = Vector3.Distance(target.position, transform.position);

        if (comboStarted)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ATTACK/STRAFE RANGE
        if (currentDist <= chaseRadius && currentDist <= attackRadius)
        {
            // Only start if we aren't already in the Attack state
            if (currentState != EnemyState.Attack && currentState != EnemyState.Stagger)
            {
                ChangeState(EnemyState.Attack);
                // Safety: Stop any existing routine before starting a new one
                if (brainRoutine != null) StopCoroutine(brainRoutine);
                brainRoutine = StartCoroutine(StartRandomCoroutine());
            }
        }
        // CHASE RANGE (Out of attack range)
        else if (currentDist <= chaseRadius && currentDist > attackRadius)
        {
            // If we were attacking/strafing, stop the brain to chase
            if (currentState == EnemyState.Attack)
            {
                if (brainRoutine != null) StopCoroutine(brainRoutine);
                brainRoutine = null;
                ChangeState(EnemyState.Idle);
            }

            if (currentState == EnemyState.Idle || (currentState == EnemyState.Walk && currentState != EnemyState.Stagger))
            {
                Vector3 temp = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                ChangeAnim(temp - transform.position);
                rb.MovePosition(temp);
                ChangeState(EnemyState.Walk);
            }
        }
    }
   
    public IEnumerator Attack1()
    {
        currentState = EnemyState.Attack;
        animator.SetBool("attack1", true);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Idle;
        animator.SetBool("attack1", false);
    }

    public IEnumerator Attack2()
    {
        currentState = EnemyState.Attack;
        animator.SetBool("attack2", true);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Idle;
        animator.SetBool("attack2", false);
    }

    public IEnumerator Attack3()
    {
        currentState = EnemyState.Attack;
        animator.SetBool("attack3", true);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Idle;
        animator.SetBool("attack3", false);
    }

    public IEnumerator Combo1()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
        StartCoroutine(Attack1());
        yield return new WaitForSeconds(1f);
        comboStarted = false;
    }

    public IEnumerator Combo2()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
        StartCoroutine(Attack1());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack2());
        yield return new WaitForSeconds(1f);
        comboStarted = false;
    }

    public IEnumerator Combo3()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
        StartCoroutine(Attack1());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack2());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack3());
        yield return new WaitForSeconds(1f);
        comboStarted = false;
    }

    public IEnumerator ChooseCombo()
    {
        comboStarted = true;
        yield return StartCoroutine(ComboChains[Random.Range(0, ComboChains.Count)]);

    }

    public IEnumerator StartRandomCoroutine()
    {
        while (true)
        {
            // 1. Pause before picking the next action
            yield return new WaitForSeconds(chooseTimer);

            // 2. Only pick a new action if a combo isn't currently running
            if (!comboStarted)
            {
                if (Random.value > 0.5f)
                {
                    // Strafe for a random duration; this routine waits until finished
                    yield return StartCoroutine(StrafeDuration(Random.Range(1f, 3f)));
                }
                else
                {
                    // Start the combo; this routine waits until the combo is done
                    yield return StartCoroutine(ChooseCombo());
                }
            }

            // 3. Optional: Brief recovery wait after an action completes
            yield return new WaitForSeconds(chooseTimer);
        }
    }

    public IEnumerator StrafeDuration(float duration)
    {
        
        float timer = 0;

        // Randomly choose to strafe left (-1) or right (1)
        float strafeDir = Random.value > 0.5f ? 1f : -1f;

        while (timer < duration)
        {
          
            // 1. Safety: If a combo starts or enemy is staggered, stop strafing
            if (comboStarted || currentState == EnemyState.Stagger) yield break;

            // 2. Calculate direction to player
            Vector2 dirToPlayer = (target.position - transform.position).normalized;

            // 3. Create perpendicular "Strafe" vector
            // In 2D: Perpendicular of (x, y) is (-y, x)
            Vector2 strafeVector = new Vector2(-dirToPlayer.y, dirToPlayer.x) * strafeDir;

            // 4. Calculate new position
            Vector2 newPos = rb.position + (strafeVector * moveSpeed * Time.deltaTime);

            // 5. Update animation and move
            ChangeAnim(dirToPlayer); // Keep facing the player while moving sideways
            animator.SetBool("isStrafing", true);
            rb.MovePosition(newPos);

            timer += Time.deltaTime;
            yield return null; // Wait for next frame
        }

        // Stop movement when done
        animator.SetBool("isStrafing", false);
        rb.linearVelocity = Vector2.zero;

    }
}
