using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusouUnit : sleepEnemy
{
    public enum Team { PlayerSide, EnemySide, Neutral }

    public enum AIMission { FollowLeader, CaptureBase, AttackCommander }
   
    public AIMission currentMission = AIMission.FollowLeader;
    public Transform missionTarget; // This could be a Base or the Commander

    [Header("Faction Settings")]
    public Team unitTeam;
    public bool followsPlayer;
    public LayerMask searchLayers;

    [Header("Movement & Detection")]
    public float detectionRange = 10f;
    public float followDistance = 3f;
    public float strafeSpeed = 3f;
    public float damageToGive = 10f;

    [Header("Aggression Settings")]
    [Range(0f, 1f)] public float aggressionScore = 0.6f;
    public float attackCooldown = 1.5f;
    private float nextAttackTime;

    [Header("Crowd Settings")]
    public float separationRadius = 0.8f;
    public float separationStrength = 3f;

    // --- INTERNAL VARIABLES ---
    private List<System.Func<IEnumerator>> comboList;
    private bool isBusy = false;
    private Vector2 myFormationSpot;
    private Health health;
    public Transform playerTransform;

    private Coroutine recoveryCoroutine;
    private Coroutine activeAction;

    [Header("Combat Movement")]
    public float attackStepForce = 4f; // How much they lunge forward

    public float hitForce = 4f; // How much they lunge forward

    // NEW INSPECTOR VARIABLE FOR ENEMY HIT-LAG
    [Tooltip("How long the game freezes when this unit hits a target")]
    public float enemyHitLagDuration = 0.06f;

    [Header("Squad Follower Settings")]
    public SquadLeader myLeader;
    public int squadIndex; // Assigned by the leader (0, 1, 2, etc.)
    public float stoppingDistance = 0.2f;


    [Header("Spacing Settings")]
    public float personalSpaceRadius = 1.5f; // How far they stay from your center
    private Vector2 attackOffset; // Their unique "slot" around you

    [Header("Starting Face Direction")]
    public Vector2 startingDirection = Vector2.down; // Default to facing Down

    public bool isOfficer;

    

    public virtual void Start()
    {
        health = GetComponent<Health>();
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // FORCE STARTING DIRECTION
        ChangeAnim(startingDirection.normalized);

        // If you use a Blend Tree, also set the parameters directly
        animator.SetFloat("moveX", startingDirection.x);
        animator.SetFloat("moveY", startingDirection.y);

        // Give each unit a unique offset so they surround targets/player
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        myFormationSpot = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;

        if (isOfficer)
        {
            // Officers get the full 5-hit chain
            comboList = new List<System.Func<IEnumerator>> { Combo1, Combo2, Combo3, Combo4, Combo5 };

            // Pro Tip: You can also boost officer stats here
            damageToGive *= 2f;
           // aggressionScore = 0.9f;
        }
        else
        {
            // Regular grunts are limited to 3-hit combos
            comboList = new List<System.Func<IEnumerator>> { Combo1, Combo2, Combo3 };
        }

        // Start the scanning loop (Don't run FindNearestTarget every single frame!)

        // Give the engine 0.25 seconds to stabilize physics before grunts scan layers!
        InvokeRepeating("FindNearestTarget", 0.25f, 0.5f);

        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        attackOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * personalSpaceRadius;
    }
 

    public override void CheckDistance()
    {
        if (isBusy || currentState == EnemyState.Stagger || animator.GetBool("isHit")) return;

        // 1. PRIORITY: COMBAT (Stay and Fight)
        if (currentTarget != null)
        {
            Health targetHealth = currentTarget.GetComponent<Health>();
            PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();

            bool isTargetDead = (targetHealth != null && targetHealth.currentHealth <= 0) ||
                                (playerHealth != null && playerHealth.currentHealth <= 0);

            if (isTargetDead)
            {
                currentTarget = null;
                StopMoving();
                return;
            }

            float trueDistToEnemy = Vector2.Distance(transform.position, currentTarget.position);
            Vector2 targetPosWithOffset = (Vector2)currentTarget.position + attackOffset;

            // =========================================================================
            // 🔥 THE ANTI-RUN FLUIDITY FIX:
            // Add a clean 0.35f buffer room to 'attackRadius'. This stops them from 
            // infinitely running into each other's colliders and forces the weapon swing!
            // =========================================================================
            float adjustedAttackRadius = attackRadius + 0.35f;

            if (trueDistToEnemy <= adjustedAttackRadius)
            {
                StopMoving(); // This cuts their velocity to 0 and stops the run animation

                if (!isBusy) StartCoroutine(BrainTick());
            }
            else if (trueDistToEnemy < detectionRange * 2.0f)
            {
                MoveTowards(targetPosWithOffset, true);
            }
            else
            {
                currentTarget = null;
                StopMoving();
            }

            return;
        }
    }

    // --- MOVEMENT ---
    public void MoveTowards(Vector2 targetPos, bool isChasing)
    {
        
        // FIX: Only check the tag if currentTarget is NOT null
        if (isChasing && currentTarget != null && currentTarget.CompareTag("Player"))
        {
            targetPos += attackOffset;
        }

        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        Vector2 separation = ComputeSeparationForce();

        // DW3 Optimization: Reduce separation strength when marching to a mission
        float sepWeight = isChasing ? 1.5f : 0.2f;

        Vector2 finalVelocity = (dir + (separation * sepWeight)).normalized * moveSpeed;
      
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, finalVelocity, Time.deltaTime * 10f);

        if (rb.linearVelocity.magnitude < 0.1f)
        {
            Debug.LogWarning($"{gameObject.name} has almost zero velocity!");
        }

        // Standard animation updates
        Vector2 faceDir = isChasing && currentTarget != null ? (Vector2)(currentTarget.position - transform.position) : dir;
        ChangeAnim(faceDir.normalized);
        animator.SetBool("isMoving", true);
    }

   public void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);

        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        animator.SetBool("isStrafing", false);

        ChangeState(EnemyState.Idle);


    }

    // --- BRAIN & COMBAT ---
    IEnumerator BrainTick()
    {
        isBusy = true;
        StopMoving();

        bool readyToSwing = Time.time >= nextAttackTime;
        float diceRoll = Random.value;

        if (readyToSwing && diceRoll < aggressionScore)
        {
            if (AttackDirector.instance != null && AttackDirector.instance.RequestAttackToken(currentTarget))
            {

                if (SoundManager.Instance != null)
                {
                    // Plays the slash sound with a subtle, natural pitch variation
                    SoundManager.Instance.PlaySFX("swordswing", 0.6f, 0.08f);
                }

                int randomCombo = Random.Range(0, comboList.Count);
                activeAction = StartCoroutine(comboList[randomCombo]());
                yield return activeAction;
                nextAttackTime = Time.time + attackCooldown;
                AttackDirector.instance.ReturnAttackToken(currentTarget);
            }
            else yield return StartCoroutine(Block(Random.Range(0.5f, 1f)));
        }
        else if (diceRoll > 0.8f) yield return StartCoroutine(Block(Random.Range(1f, 1.5f)));
        else yield return StartCoroutine(StrafeBehavior());

        isBusy = false;
    }

    IEnumerator PlayAttack(string animName)
    {
        if (currentTarget == null) yield break;

        // 1. Face the target
        Vector2 dirToTarget = (currentTarget.position - transform.position).normalized;
        ChangeAnim(dirToTarget);

        // 2. The Lunge: Apply a physical burst toward the target
        // This makes them "slide" into range like the player
        rb.AddForce(dirToTarget * attackStepForce, ForceMode2D.Impulse);

        // 3. Play Animation
        animator.SetBool(animName, true);

        // Note: Just like the player, the actual DAMAGE should 
        // come from an Animation Event calling a 'CheckForHit' function!

        yield return new WaitForSeconds(0.6f);
        animator.SetBool(animName, false);
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator StrafeBehavior()
    {
        ChangeState(EnemyState.Strafe);
        float timer = 0;
        float duration = Random.Range(1.5f, 2.5f); // Longer, slower strafe
        float strafeDir = Random.value > 0.5f ? 1f : -1f;
        animator.SetBool("isStrafing", true);

        while (timer < duration && currentTarget != null)
        {
            if (currentState == EnemyState.Stagger) break;

            // 1. Calculate direction to target
            Vector2 toTarget = (currentTarget.position - transform.position).normalized;

            // 2. Calculate the "Side" vector
            Vector2 sideDir = Vector2.Perpendicular(toTarget) * strafeDir;

            Vector2 targetVelocity = sideDir * strafeSpeed;

            // 3. Keep them at the "sweet spot" distance (don't drift)
            float dist = Vector2.Distance(transform.position, currentTarget.position);
            Vector2 correctionDir = Vector2.zero;
            if (dist > attackRadius) correctionDir = toTarget; // Move closer
            else if (dist < attackRadius - 1f) correctionDir = -toTarget; // Back up

            // 4. Combine movement (Mostly sideways, slightly forward/back)
            rb.linearVelocity = (sideDir + (correctionDir * 0.5f)).normalized * strafeSpeed;

            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);

            ChangeAnim(toTarget); // Always keep eyes on the prize
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero; // Hard stop after strafe
        animator.SetBool("isStrafing", false);
    }

    private IEnumerator Block(float blockTime)
    {
        ChangeState(EnemyState.Block);
        animator.SetBool("isBlocking", true);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(blockTime);
        animator.SetBool("isBlocking", false);
    }

    // --- SENSORS & CROWDS ---
    private void FindNearestTarget()
    {
        Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, detectionRange, searchLayers);
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var col in potentialTargets)
        {
            if (col.gameObject == this.gameObject) continue;
            MusouUnit other = col.GetComponent<MusouUnit>();

            if (other != null && other.unitTeam != this.unitTeam)
            {
                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < closestDist) { closestDist = d; bestTarget = col.transform; }
            }
            else if (this.unitTeam == Team.EnemySide && col.CompareTag("Player"))
            {
                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < closestDist) { closestDist = d; bestTarget = col.transform; }
            }
        }
        currentTarget = bestTarget;
    }

    private Vector2 ComputeSeparationForce()
    {
        Vector2 separation = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, separationRadius, searchLayers);

        foreach (var other in nearby)
        {
            if (other.gameObject == this.gameObject) continue;

            Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
            float distance = diff.magnitude;

            // THE FIX: If distance is 0, they are "inside" each other.
            // Give them a tiny random push so the math doesn't break.
            if (distance < 0.01f)
            {
                diff = Random.insideUnitCircle.normalized * 0.1f;
                distance = 0.1f;
            }

            if (distance < separationRadius)
            {
                // Use a clamped force so it never explodes to Infinity
                float force = (separationRadius - distance) / separationRadius;
                separation += diff.normalized * force;
            }
        }
        return separation * separationStrength;
    }

    // --- DAMAGE & STAGGER ---
    public void TriggerHit(Vector2 attackerPos)
    {

        if (activeAction != null) StopCoroutine(activeAction); // Stops BrainTick/Combos/Strafe
        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine); // Resets stagger timer if hit again

        Vector2 knockbackDir = ((Vector2)transform.position - attackerPos).normalized;

        // 1. Physical Knockback (Physics happens NOW)
        rb.linearVelocity = knockbackDir * hitForce;

        // 2. Visual Snap (Animation happens NOW)
        // "Hit" should be the exact name of your State in the Animator
        animator.Play("Hit", 0, 0f);

        // 3. State Management
        isBusy = false;
        currentState = EnemyState.Stagger;
        animator.SetBool("isHit", true);
        animator.SetBool("isMoving", false);

        recoveryCoroutine = StartCoroutine(RecoveryRoutine(0.15f));
    }

    private IEnumerator RecoveryRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        animator.SetBool("isHit", false);
        currentState = EnemyState.Idle;
    }

    // Call this via Animation Event in your attack frames!
    public void ApplyDamageToTarget()
    {
        if (currentTarget == null) return;

        Vector2 knockbackDir = (currentTarget.position - transform.position).normalized;
        float force = 5f;

        // Create a unified list of scripts that inherit from MonoBehaviour
        List<MonoBehaviour> victims = new List<MonoBehaviour>();

        // 1. Check for standard enemy units
        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null && targetHealth.currentHealth > 0)
        {
            targetHealth.TakeDamage(damageToGive, transform.position, knockbackDir * force, null, null);
            victims.Add(targetHealth);
        }

        // 2. Check for the Player unit
        PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.currentHealth > 0)
        {
            playerHealth.TakeDamage(damageToGive, transform.position, knockbackDir * force);
            victims.Add(playerHealth);
        }

        // BASARA HIT-STOP: Freeze this attacking unit and the hit target
        // BASARA HIT-STOP: Uses the exposed inspector variable now
        if (victims.Count > 0 && HitLagManager.Instance != null)
        {
            HitLagManager.Instance.TriggerBasaraHitLag(
                GetComponentInChildren<Animator>(),
                rb,
                victims,
                enemyHitLagDuration // Controlled in the Inspector per-grunt/per-officer
            );
        }
    }
    public virtual Vector2 GetSlotPosition(int index)
    {
        return transform.position; // Default: just return my own position
    }

    IEnumerator Combo1() 
    { 
        yield return StartCoroutine(PlayAttack("attack1")); 
    }
    IEnumerator Combo2()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
    }

    IEnumerator Combo3()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack3"));
    }

    IEnumerator Combo4()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack3"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack4"));
    }

    IEnumerator Combo5()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack3"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack4"));
        if (currentTarget == null || Vector2.Distance(transform.position, currentTarget.position) > attackRadius * 1.5f) yield break;
        yield return StartCoroutine(PlayAttack("attack5"));
    }
}