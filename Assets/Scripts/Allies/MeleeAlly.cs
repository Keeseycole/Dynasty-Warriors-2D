using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAlly : sleepEnemy
{
    // --- VARIABLES ---
    private List<System.Func<IEnumerator>> comboList;
    private bool isBusy = false;
    private Coroutine brainRoutine;
    private Vector2 myFormationSpot; // Unique spot around the player

    [Header("Ally Settings")]
    public float detectionRange = 10f;
    public float followDistance = 3f;
    public Transform playerTransform;
    public LayerMask enemyLayer;
    public float damageToGive = 10f;

    [Header("Aggression Settings")]
    [Range(0f, 1f)] public float aggressionScore = 0.6f; // 0.6 = 60% chance to attack
    public float attackCooldown = 1.5f; // Time to wait between combos
    private float nextAttackTime;

    private void Start()
    {
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Give each ally a unique offset so they don't clump when following
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        myFormationSpot = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2f;

        comboList = new List<System.Func<IEnumerator>> { Combo1, Combo2, Combo3 };
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    private void FixedUpdate() => CheckDistance();

    public override void CheckDistance()
    {


        // 1. SENSES: Look for enemies
        FindNearestTarget();

        // 2. GUARDS: Stop if busy or staggered
        if (isBusy || currentState == EnemyState.Stagger)
        {
            if (currentState == EnemyState.Stagger) rb.linearVelocity = Vector2.zero;
            return;
        }

        // 3. DECISION TREE
        if (currentTarget != null)
        {
            float distToEnemy = Vector2.Distance(transform.position, currentTarget.position);

            if (distToEnemy <= attackRadius)
            {
                // FIGHT: Start the combat brain
                if (!isBusy) brainRoutine = StartCoroutine(BrainTick());
            }
            else
            {
                // CHASE: Run toward the enemy
                MoveTowards(currentTarget.position, true);
            }
        }
        else if (playerTransform != null)
        {
            // FOLLOW: No enemies? Return to player
            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distToPlayer > followDistance)
            {
                MoveTowards((Vector2)playerTransform.position + myFormationSpot, false);
            }
            else
            {
                StopMoving();
            }
        }
    }

    // --- MOVEMENT ---
    void MoveTowards(Vector2 targetPos, bool isChasing)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        ChangeAnim(isChasing ? (currentTarget.position - transform.position).normalized : dir);
        animator.SetBool("isMoving", true);
        ChangeState(EnemyState.Walk);
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        ChangeState(EnemyState.Idle);
    }

    // --- COMBAT BRAIN (Matches MeleeEnemy) ---
    IEnumerator BrainTick()
    {
        isBusy = true;
        StopMoving();

        // 1. THE AGGRESSION CHECK (Move this to the TOP)
        // Roll a dice (0.0 to 1.0). If it's HIGHER than our score, we circle instead of attacking.
        // Example: If aggression is 0.2, there's an 80% chance we just circle.
        bool feelBrave = Random.value < aggressionScore;

        // 2. THE COOLDOWN CHECK
        bool readyToSwing = Time.time >= nextAttackTime;

        if (feelBrave && readyToSwing)
        {
            // 3. THE DIRECTOR CHECK (Only ask if we actually WANT to attack)
            if (AttackDirector.instance != null && AttackDirector.instance.RequestAttackToken(currentTarget))
            {
                ChangeState(EnemyState.Attack);
                int randomCombo = Random.Range(0, comboList.Count);
                yield return StartCoroutine(comboList[randomCombo]());

                // Set the cooldown: Braver units wait less time
                nextAttackTime = Time.time + (attackCooldown * (1f - aggressionScore));

                AttackDirector.instance.ReturnAttackToken(currentTarget);
            }
            else
            {
                // Director said no? Circle instead.
                yield return StartCoroutine(StrafeBehavior());
            }
        }
        else
        {
            // Not brave or on cooldown? Circle instead.
            yield return StartCoroutine(StrafeBehavior());
        }

        yield return new WaitForSeconds(0.2f);
        isBusy = false;
    }

    // Beginner Tip: Put your Strafe code in its own function so you don't repeat it!
    IEnumerator StrafeBehavior()
    {
        ChangeState(EnemyState.Strafe);
        yield return StartCoroutine(StrafeDuration(Random.Range(1f, 1.5f)));
    }

    // --- ATTACKS ---
    IEnumerator Combo1() { yield return StartCoroutine(PlayAttack("attack1")); }
    IEnumerator Combo2() { yield return StartCoroutine(PlayAttack("attack1")); yield return StartCoroutine(PlayAttack("attack2")); }
    IEnumerator Combo3() { yield return StartCoroutine(PlayAttack("attack1")); yield return StartCoroutine(PlayAttack("attack2")); yield return StartCoroutine(PlayAttack("attack3")); }

    IEnumerator PlayAttack(string animName)
    {
        if (currentTarget) ChangeAnim((currentTarget.position - transform.position).normalized);

        animator.SetBool(animName, true);
        yield return new WaitForSeconds(0.8f); // Adjust based on your anim length
        animator.SetBool(animName, false);
        yield return new WaitForSeconds(0.2f);
    }

    public IEnumerator StrafeDuration(float duration)
    {
        float timer = 0;
        float dir = Random.value > 0.5f ? 1f : -1f;
        animator.SetBool("isStrafing", true);

        while (timer < duration && currentTarget != null)
        {
            if (currentState == EnemyState.Stagger) break;

            Vector2 orbitDir = Vector2.Perpendicular((currentTarget.position - transform.position).normalized) * dir;
            rb.linearVelocity = orbitDir * moveSpeed * 0.7f;
            ChangeAnim((currentTarget.position - transform.position).normalized);

            timer += Time.deltaTime;
            yield return null;
        }
        animator.SetBool("isStrafing", false);
    }

     private void FindNearestTarget()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform tempTarget = null;

        foreach (var t in targets)
        {
            if (t.gameObject == this.gameObject) continue;

            float d = Vector2.Distance(transform.position, t.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                tempTarget = t.transform;
            }
        }
        currentTarget = tempTarget;
    }

    void ApplyDamageToTarget()
    {
        if (currentTarget == null) return;

        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null)
        {

            targetHealth.TakeDamage(damageToGive);
        }
    }
}