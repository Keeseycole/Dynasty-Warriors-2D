using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MeleeEnemy : sleepEnemy
{
    // --- SETTINGS ---
    [Header("Detection")]
    public LayerMask detectionLayers; // Set to "Player" and "Ally"
    public float detectionRadius = 10f;

    [Header("Crowd Settings")]
    public float separationRadius = 0.8f;
    public float separationStrength = 3f;

    // --- INTERNAL VARIABLES ---
    private List<System.Func<IEnumerator>> comboList;
    private bool isBusy = false; // True if we are currently attacking or circling
    private Vector2 myPersonalSpot; // My unique offset so I don't clump with others

    public float damageToGive;

    [Header("Aggression Settings")]
    [Range(0f, 1f)] public float aggressionScore = 0.6f; // 0.6 = 60% chance to attack
    public float attackCooldown = 1.5f; // Time to wait between combos
    private float nextAttackTime;

    public Transform target;

    public float strafeSpeed;

    private Health health;
    private void Start()
    {

        InvokeRepeating("FindNearestTarget", 0f, 0.5f); // Scan for targets twice a second, not 50 times!

        // 1. Give this enemy a unique angle so they surround the target
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        myPersonalSpot = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;

        // 2. Load attacks into a list
        comboList = new List<System.Func<IEnumerator>> { Combo1, Combo2, Combo3 };
    
    }

  
    void Awake()
    {
        health = GetComponent<Health>(); // Get reference to the Health script on the parent
    }

    private void Update()
    {
        // If the unit is off-screen, skip the AI logic
        if (health != null && health.isSimulating)
        {
            return;
        }
    }


    private void FixedUpdate()
    {
        // Run the "Brain" logic every physics frame
        CheckDistance();
    }

    public override void CheckDistance()
    {

        // Always look for the closest person every frame
        FindNearestTarget();

        // If there is truly no one on the map, stop moving
        if (currentTarget == null)
        {
            StopMoving();
            return;
        }

        // Measure the actual target we found (could be Ally or Player)
        float dist = Vector2.Distance(transform.position, currentTarget.position);

        // 3. NEW: If the target is DEAD, find a new one immediately
        Health targetHealth = currentTarget.GetComponent<Health>();

        if (targetHealth != null && targetHealth.currentHealth <= 0)
        {
            currentTarget = null;
            FindNearestTarget();
            return;
        }

        if(currentState == EnemyState.Stagger || animator.GetBool("isHit"))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // STEP 2: GUARDS - Stop if dead, flinching, or already busy attacking
        if (currentTarget == null || isBusy || currentState == EnemyState.Stagger)
        {
            if (!isBusy) StopMoving();
            return;
        }

        // STEP 3: MEASURE - How far is the target?
        float distance = Vector2.Distance(transform.position, currentTarget.position);
        Vector2 direction = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;

        // STEP 4: DECIDE
        if (distance <= attackRadius)
        {
            // Close enough! Start the attack/strafe routine
            StartCoroutine(BrainTick());
        }
        else if (distance <= chaseRadius)
        {
            // Too far! Walk toward my spot near the target
            MoveToTarget(direction);
        }
        else
        {
            // No one is around
            StopMoving();
        }
    }

    // --- MOVEMENT METHODS ---

    void MoveToTarget(Vector2 dirToTarget)
    {
        // Calculate: Spot near player + Pushing away from other enemies
        Vector2 targetSpot = (Vector2)currentTarget.position + myPersonalSpot;
        Vector2 path = (targetSpot - (Vector2)transform.position).normalized;
        Vector2 separation = ComputeSeparationForce();

        // Apply movement to Rigidbody
        rb.linearVelocity = (path + (separation * 0.4f)).normalized * moveSpeed;

        animator.SetBool("isMoving", true);
        ChangeAnim(dirToTarget); // Face target while walking
        ChangeState(EnemyState.Walk);
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        animator.SetBool("isStrafing", false);
        ChangeState(EnemyState.Idle);
    }

    // --- BRAIN & COMBAT METHODS ---

    IEnumerator BrainTick()
    {
        isBusy = true;
        StopMoving();

        // 1. Check if we are ready to swing
        bool readyToSwing = Time.time >= nextAttackTime;

        // 2. Roll for Aggression
        float diceRoll = Random.value;

        if (readyToSwing && diceRoll < aggressionScore)
        {
            // TRY TO ATTACK
            if (AttackDirector.instance != null && AttackDirector.instance.RequestAttackToken(currentTarget))
            {
                int randomCombo = Random.Range(0, comboList.Count);
                yield return StartCoroutine(comboList[randomCombo]());
                nextAttackTime = Time.time + attackCooldown;
            }
            else
            {
                // Director said NO? Instead of just walking, let's BLOCK
                yield return StartCoroutine(Block(Random.Range(1f, 1.5f)));
            }
        }
        else if (diceRoll > 0.8f) // 20% chance to block if not attacking
        {
            // THE CALL: Start the blocking behavior
            yield return StartCoroutine(Block(Random.Range(1f, 2f)));
        }
        else
        {
            // Just circle the player
            yield return StartCoroutine(StrafeBehavior());
        }

        yield return new WaitForSeconds(0.2f);
        isBusy = false;
    }

    // Beginner Tip: Put your Strafe code in its own function so you don't repeat it!
    IEnumerator StrafeBehavior()
    {
        ChangeState(EnemyState.Strafe);
        yield return StartCoroutine(CircleTarget(Random.Range(1f, 1.5f)));
    }
    IEnumerator PlayAttack(string animName)
    {
        // SAFETY: If the target died while we were preparing to swing, DON'T SWING
        if (currentTarget == null) yield break;

        Health h = currentTarget.GetComponent<Health>();
        if (h != null && h.currentHealth <= 0) yield break;

        // Face the victim and swing
        ChangeAnim((currentTarget.position - transform.position).normalized);
        animator.SetBool(animName, true);

        yield return new WaitForSeconds(0.8f);
        animator.SetBool(animName, false);
    }

    IEnumerator CircleTarget(float time)
    {
        float timer = 0;
        float strafeDir = Random.value > 0.5f ? 1f : -1f;
        animator.SetBool("isStrafing", true);

        while (timer < time && currentTarget != null)
        {
            if (currentState == EnemyState.Stagger) break;

            // Move sideways relative to the target
            Vector2 sideDir = Vector2.Perpendicular((currentTarget.position - transform.position).normalized) * strafeDir;
            rb.linearVelocity = sideDir * strafeSpeed * 0.7f;

            ChangeAnim((currentTarget.position - transform.position).normalized);
            timer += Time.deltaTime;
            yield return null;
        }
        animator.SetBool("isStrafing", false);
    }

    // --- COMBOS ---
    IEnumerator Combo1() 
    { 
        yield return StartCoroutine(PlayAttack("attack1")); 
    }
    IEnumerator Combo2() 
    { 
        yield return StartCoroutine(PlayAttack("attack1"));
        yield return StartCoroutine(PlayAttack("attack2"));
    }
    IEnumerator Combo3() 
    {
        yield return StartCoroutine(PlayAttack("attack1")); 
        yield return StartCoroutine(PlayAttack("attack2"));
        yield return StartCoroutine(PlayAttack("attack3")); 
    }

    // --- SENSORS ---

    private void FindNearestTarget()
    {
        // 1. SCAN: Get everyone in range on the Player AND Ally layers
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectionLayers);

        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var t in targets)
        {
            // Skip myself
            if (t.gameObject == this.gameObject) continue;

            // Skip if they are already dead
            Health h = t.GetComponent<Health>();
            if (h != null && h.currentHealth <= 0) continue;

            float d = Vector2.Distance(transform.position, t.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                bestTarget = t.transform;
            }
        }

        // 2. ASSIGN: If we found someone (Ally or Player), they are the target.
        // If the circle is empty, we fallback to the main Player 'target'.
        currentTarget = (bestTarget != null) ? bestTarget : target;
    }

    private Vector2 ComputeSeparationForce()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, separationRadius, LayerMask.GetMask("Enemy"));

        foreach (var other in nearby)
        {
            if (other.gameObject == this.gameObject) continue;
            Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
            separation += diff.normalized / (diff.magnitude + 0.1f);
        }
        return separation * separationStrength;
    }

    void OnDestroy()
    {
        if (AttackDirector.instance != null && currentTarget != null)
        {
            AttackDirector.instance.ReturnAttackToken(currentTarget);
        }
    }

    public void TriggerHit()
    {
        // 1. Logic Guards
        StopAllCoroutines();
        isBusy = false;
        currentState = EnemyState.Stagger;

        if (animator != null)
        {
            animator.SetBool("isHit", true);
            animator.SetBool("isMoving", false);
        }

        StartCoroutine(RecoveryRoutine(0.4f));
    }

    private IEnumerator RecoveryRoutine(float waitTime)
{
    yield return new WaitForSeconds(waitTime);
    
    // 5. THE FIX: Release the animation lock
    if (animator != null)
    {
        animator.SetBool("isHit", false);
    }

    // 6. Return to Idle so CheckDistance can run again
    currentState = EnemyState.Idle;
}

    private IEnumerator Block(float blockTime)
    {
        ChangeState(EnemyState.Block);
        animator.SetBool("isBlocking", true);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(blockTime);
        animator.SetBool("isBlocking", false);
    }

    public void ApplyDamageToTarget()
    {
        if (currentTarget == null) return;
        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null)
        {
            // Calculate knockback direction (from the attacker to the target)
            Vector2 knockbackDir = (currentTarget.position - transform.position).normalized;
            float force = 5f; // Or add a 'knockbackForce' variable to your header

            targetHealth.TakeDamage(damageToGive, transform.position, knockbackDir * force);
        }
    }
}