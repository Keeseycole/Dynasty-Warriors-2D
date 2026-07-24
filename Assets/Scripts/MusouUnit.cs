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
    public float baseAggressionScore;
    [Range(0f, 1f)] public float aggressionScore = 0.5f;
    public float attackCooldown = 1.5f;
    private float nextAttackTime;

    [Header("Crowd Settings")]
    public float separationRadius = 0.8f;
    public float separationStrength = 3f;

    // --- INTERNAL VARIABLES ---
    private List<System.Func<IEnumerator>> comboList;
    public bool isBusy = false;
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


    // 🔥 THE MULTI-COMMANDER TOGGLE:
    [Tooltip("Check this box in the Inspector if this unit is a main stage Commander. " +
        "If there are multiple, the level ends when all of them are defeated!")]
    public bool isStageCommander;

    // Inside MusouUnit.cs
    public override void Start()
    {
        // 🔥 THE HERO ARCHITECTURE SAFETY PADLOCK:
        // If this script is attached to your playable player character prefab,
        // instantly disable the enemy AI processing thread so it never fights your physics updates!
        if (GetComponent<PlayerController>() != null || GetComponentInParent<PlayerController>() != null)
        {
            isOfficer = true; // Forces the player to unlock their full 5-hit combos!
            unitTeam = Team.PlayerSide;

            // Bypass all downstream AI asset loops, base setups, and invoke cycles completely
            return;
        }

        // --- Standard AI Soldier / Officer Startup Configurations (Untouched) ---
        base.Start();

        baseAggressionScore = aggressionScore;
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInChildren<Health>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        ChangeAnim(startingDirection.normalized);

        if (animator != null)
        {
            animator.SetFloat("moveX", startingDirection.x);
            animator.SetFloat("moveY", startingDirection.y);
        }

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        myFormationSpot = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;

        if (isOfficer)
        {
            comboList = new List<System.Func<IEnumerator>> { Combo1, Combo2, Combo3, Combo4, Combo5 };
            //damageToGive *= 2f;
        }
        else
        {
            comboList = new List<System.Func<IEnumerator>> { Combo1, Combo2, Combo3 };
        }

        // Start the scanning loop for regular grunts
        InvokeRepeating("FindNearestTarget", 0.25f, 0.5f);

        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        attackOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * personalSpaceRadius;

        if (myLeader != null)
        {
            StartCoroutine(DelayPhysicsActivation());
        }
    }

    // Add this supporting coroutine function directly under your Start method
    private System.Collections.IEnumerator DelayPhysicsActivation()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) myCol = GetComponentInChildren<Collider2D>();

        // Temporarily freeze their physical overlaps on frame one
        if (myCol != null) myCol.enabled = false;

        yield return new WaitForSeconds(0.1f);

        if (myCol != null) myCol.enabled = true;
    }
    protected override void FixedUpdate()
    {
        // Execute standard distance tracking
        CheckDistance();

        // 🔥 THE DESTINATION ANCHOR PADLOCK:
        // If the grunt is Idle and its leader has come to a halt, 
        // freeze its velocity completely to stop ghost drift!
        if (currentState == EnemyState.Idle && myLeader != null)
        {
            if (myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude <= 0.1f)
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }
    public override void CheckDistance()
    {
        // 🔥 THE LOGICAL SAFETY GATE: 
        // If the animator component is missing or unassigned yet, exit instantly to prevent null crashes!
        if (animator == null) return;

        if (isBusy || currentState == EnemyState.Stagger || animator.GetBool("isHit")) return;

        // 🔥 THE CRITICAL CORRECTION: Always read the physical position of the moving child sprite body!
        Vector2 physicalPos = (rb != null) ? rb.position : (Vector2)transform.position;

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

            // Calculate distance from the physical body, NOT the unmoving parent pivot
            Vector2 deltaToEnemy = (Vector2)currentTarget.position - physicalPos;
            float sqrDistToEnemy = deltaToEnemy.sqrMagnitude;

            float adjustedAttackRadius = attackRadius + 0.35f;
            float sqrAttackRadius = adjustedAttackRadius * adjustedAttackRadius;

            if (sqrDistToEnemy <= sqrAttackRadius)
            {
                StopMoving();
                if (!isBusy)
                {
                    isBusy = true;
                    StartCoroutine(BrainTick());
                }
            }
            else if (sqrDistToEnemy < (detectionRange * 2.0f) * (detectionRange * 2.0f))
            {
                Vector2 targetPosWithOffset = (Vector2)currentTarget.position + attackOffset;
                MoveTowards(targetPosWithOffset, true);
            }
            else
            {
                currentTarget = null;
                StopMoving();
            }
            return;
        }

        if (currentMission == AIMission.FollowLeader)
        {
            ExecuteMacroMission();
        }
    }

    // --- MOVEMENT ---
    public void MoveTowards(Vector2 targetPos, bool isChasing)
    {
        if (rb == null) return;

        Vector2 physicalPos = rb.position;
        Vector2 targetDir = (targetPos - physicalPos).normalized;

        // 1. Calculate and strictly clamp separation forces to prevent sudden spikes
        Vector2 separationForce = ComputeSeparationForce();
        separationForce = Vector2.ClampMagnitude(separationForce, 1.2f);

        Vector2 desiredDirection = (targetDir + separationForce).normalized;

        if (!isChasing)
        {
            // Formation Marching: Assign direct velocity to eliminate acceleration drift
            rb.linearVelocity = desiredDirection * moveSpeed;
        }
        else
        {
            // Combat Chasing: Use clean, direct interpolation instead of AddForce
            Vector2 targetVelocity = desiredDirection * moveSpeed;

            // 🔥 THE ACCELERATION LOCK: 
            // Smoothly adjust velocity without letting physics forces multiply exponentially
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 12f);
        }

        // 2. HARD FRAME CAP: Clamp the velocity to ensure they never exceed their max speed
        float currentMaxSpeed = isChasing ? (moveSpeed * 1.2f) : moveSpeed;
        if (rb.linearVelocity.sqrMagnitude > currentMaxSpeed * currentMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }

        // Animation updates
        Vector2 faceDir = (isChasing && currentTarget != null) ? ((Vector2)currentTarget.position - physicalPos) : targetDir;
        ChangeAnim(faceDir.normalized);
        animator.SetBool("isMoving", true);
    }

    public void StopMoving()
    {
        if (rb != null)
        {
            // 🔥 THE COMPLETE PHYSICS RESET: 
            // Kill both linear velocity and any active physical forces on the body
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        animator.SetBool("isMoving", false);
        animator.SetBool("isStrafing", false);
        ChangeState(EnemyState.Idle);
    }

    // --- BRAIN & COMBAT ---
    // Inside MusouUnit.cs / MeleeEnemy.cs -> BrainTick()

    IEnumerator BrainTick()
    {
        isBusy = true;
        StopMoving();

        bool readyToSwing = Time.time >= nextAttackTime;
        float diceRoll = Random.value;

        // 🔥 THE VISUAL INSPECTOR FIX:
        // Instead of using a hidden local variable, overwrite the main class variable 
        // directly so Unity can display the active, changing value right in your Inspector!
     if (MoraleManager.Instance != null)
{
    // Pass the immutable base setting into the calculator, and store the output in our active tracker!
    aggressionScore = MoraleManager.Instance.GetAdjustedAggression(this.unitTeam, baseAggressionScore);
}

        if (readyToSwing && diceRoll < aggressionScore) // Evaluates against the newly visible score
        {
            if (AttackDirector.instance != null && AttackDirector.instance.RequestAttackToken(this, currentTarget))
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("swordswing", 0.6f, 0.08f);
                }

                int randomCombo = Random.Range(0, comboList.Count);
                activeAction = StartCoroutine(comboList[randomCombo]());
                yield return activeAction;
                nextAttackTime = Time.time + attackCooldown;
                AttackDirector.instance.ReturnAttackToken(this, currentTarget);
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
        float duration = Random.Range(1.5f, 2.5f);
        float strafeDir = Random.value > 0.5f ? 1f : -1f;
        animator.SetBool("isStrafing", true);

        float attackRadiusSqr = attackRadius * attackRadius;
        float backUpRadiusSqr = (attackRadius - 1f) * (attackRadius - 1f);

        while (timer < duration && currentTarget != null)
        {
            if (currentState == EnemyState.Stagger) break;

            Vector2 myPos = transform.position;
            Vector2 targetPos = currentTarget.position;
            Vector2 toTarget = (targetPos - myPos).normalized;
            Vector2 sideDir = Vector2.Perpendicular(toTarget) * strafeDir;

            float sqrDist = (targetPos - myPos).sqrMagnitude;
            Vector2 correctionDir = Vector2.zero;
            if (sqrDist > attackRadiusSqr) correctionDir = toTarget;
            else if (sqrDist < backUpRadiusSqr) correctionDir = -toTarget;

            // FIX: Calculate target velocity purely from static direction values, 
            // completely removing the broken double-assignment line!
            Vector2 targetVelocity = (sideDir + (correctionDir * 0.5f)).normalized * strafeSpeed;

            // Lerp smoothly from current physics velocity directly to the clean target velocity
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);

            // Safety fallback velocity hard cap inside the frame loop
            if (rb.linearVelocity.sqrMagnitude > strafeSpeed * strafeSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * strafeSpeed;
            }

            ChangeAnim(toTarget);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
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
    public void FindNearestTarget()
    {
        // 🔥 THE HERO SAFETY GATE: If this script is attached to the player, 
        // exit instantly to stop the AI brain from overriding your manual selections!
        if (GetComponent<PlayerController>() != null || GetComponentInParent<PlayerController>() != null)
        {
            return;
        }

        // Safety reset if the player somehow got assigned previously
        if (this.unitTeam == Team.PlayerSide && currentTarget != null && (currentTarget.CompareTag("Player") || currentTarget.root.CompareTag("Player")))
        {
            currentTarget = null;
        }

        Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, detectionRange, searchLayers);
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var col in potentialTargets)
        {
            if (col.gameObject == this.gameObject) continue;

            // 🔥 CRITICAL ALLY PROTECTION GATES:
            // 1. If this unit is an Ally, completely ignore anything tagged "Player"
            if (this.unitTeam == Team.PlayerSide && col.CompareTag("Player")) continue;
            // 2. If the player's hurtbox is a child object, check its root parent tag too!
            if (this.unitTeam == Team.PlayerSide && col.transform.root.CompareTag("Player")) continue;

            MusouUnit other = col.GetComponent<MusouUnit>();

            if (other != null)
            {
                // Fight units on an opposing faction (Allies vs Enemies)
                if (other.unitTeam != this.unitTeam && other.unitTeam != Team.Neutral)
                {
                    Health h = col.GetComponent<Health>();
                    if (h != null && h.currentHealth <= 0) continue;

                    float d = Vector2.Distance(transform.position, col.transform.position);
                    if (d < closestDist) { closestDist = d; bestTarget = col.transform; }
                }
            }
            // STRICT ENEMY-ONLY TRACKING FOR THE PLAYER CHARACTER:
            // Only allow actual EnemySide units to lock onto the Player or player hurtboxes
            else if (this.unitTeam == Team.EnemySide && (col.CompareTag("Player") || col.transform.root.CompareTag("Player")))
            {
                // Verify if it's a sub-hurtbox or the main player health component
                PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();
                if (ph != null && ph.currentHealth <= 0) continue;

                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < closestDist) { closestDist = d; bestTarget = col.transform; }
            }
        }

        currentTarget = bestTarget;
    }

    private Vector2 ComputeSeparationForce()
    {
        Vector2 separation = Vector2.zero;
        Vector2 myPhysicalPos = (rb != null) ? rb.position : (Vector2)transform.position;

        // 🔥 SAFE FALLBACK: Uses standard OverlapCircleAll to remove the buffer dependency
        Collider2D[] nearby = Physics2D.OverlapCircleAll(myPhysicalPos, separationRadius, searchLayers);
        int neighborsCount = 0;

        foreach (var other in nearby)
        {
            if (other == null || other.gameObject == this.gameObject) continue;

            // 🔥 THE INTERCEPTOR GATE:
            // Ignore the collider if it belongs to a child weapon hitbox or trigger.
            // It must have a MusouUnit component on its body to affect crowd spacing!
            MusouUnit otherUnit = other.GetComponent<MusouUnit>();
            if (otherUnit == null)
            {
                otherUnit = other.GetComponentInParent<MusouUnit>();
                if (otherUnit == null) continue; // Not a character body, skip it!
            }

            // Use the actual physical position of the neighbor's character body
            Rigidbody2D otherRb = otherUnit.rb;
            Vector2 otherPos = (otherRb != null) ? otherRb.position : (Vector2)otherUnit.transform.position;

            Vector2 diff = myPhysicalPos - otherPos;
            float distance = diff.magnitude;

            // Prevent division-by-zero errors if they overlap exactly
            if (distance < 0.2f)
            {
                Vector2 randomPush = Random.insideUnitCircle.normalized;
                if (randomPush == Vector2.zero) randomPush = Vector2.up;

                separation += randomPush * (separationStrength * 2f);
                neighborsCount++;
                continue;
            }

            if (distance < separationRadius)
            {
                float forceStrength = (separationRadius - distance) / distance;
                separation += diff.normalized * forceStrength;
                neighborsCount++;
            }
        }

        if (neighborsCount > 0)
        {
            return (separation / neighborsCount) * separationStrength;
        }

        return Vector2.zero;
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
    // Inside MusouUnit.cs
    public void ApplyDamageToTarget()
    {
        if (currentTarget == null) return;

        Vector2 myPos = (rb != null) ? rb.position : (Vector2)transform.position;
        Vector2 knockbackDir = ((Vector2)currentTarget.position - myPos).normalized;
        float force = 5f;

        float activeStrikeDamage = (damageToGive > 0.1f) ? damageToGive : 15f;

        // 🟢 DUAL-FACTION ARCHITECTURE SETUP:
        // First, check if the current target is an NPC/Enemy Unit (uses standard Health)
        Health npcHealth = currentTarget.GetComponent<Health>();
        if (npcHealth == null) npcHealth = currentTarget.GetComponentInChildren<Health>();

        if (npcHealth != null && npcHealth.currentHealth > 0)
        {
            Animator myAnim = animator != null ? animator : GetComponentInChildren<Animator>();
            npcHealth.TakeDamage(activeStrikeDamage, myPos, knockbackDir * force, myAnim, rb);
            return; // Target successfully damaged, exit early!
        }

        // 🟢 PLAYER HIT-DETECTION OVERRIDE:
        // If it isn't an NPC, check if it's the playable player hero (uses unique PlayerHealth)
        PlayerHealth playableHeroHealth = currentTarget.GetComponent<PlayerHealth>();
        if (playableHeroHealth == null) playableHeroHealth = currentTarget.GetComponentInParent<PlayerHealth>();

        if (playableHeroHealth != null && playableHeroHealth.currentHealth > 0)
        {
            Vector2 targetKnockbackForce = knockbackDir * hitForce; // Use enemy's custom impact values

            // Route the damage pass cleanly into your player's accurate tracking script parameters
            playableHeroHealth.TakeDamage(activeStrikeDamage, myPos, targetKnockbackForce);

            Debug.Log($"<color=red>[ENEMY ATTACK HIT]:</color> Unit <b>{gameObject.name}</b> successfully struck the player for {activeStrikeDamage} damage!");
        }
    }

    public void ExecuteMacroMission()
    {
        switch (currentMission)
        {
            case AIMission.FollowLeader:
                if (myLeader == null)
                {
                    myLeader = GetComponentInParent<SquadLeader>();
                    if (myLeader == null && transform.parent != null)
                    {
                        myLeader = transform.parent.GetComponentInChildren<SquadLeader>();
                    }
                }

                if (myLeader != null)
                {
                    Vector2 leaderPos = (myLeader.rb != null) ? myLeader.rb.position : (Vector2)myLeader.transform.position;
                    Vector2 gruntPhysicalPos = (rb != null) ? rb.position : (Vector2)transform.position;

                    Vector2 exactSlotTarget = leaderPos + myFormationSpot;
                    Vector2 deltaToSlot = exactSlotTarget - gruntPhysicalPos;
                    float distToSlotSqr = deltaToSlot.sqrMagnitude;

                    float keepUpRadius = 1.2f;
                    bool isLeaderMoving = myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude > 0.1f;

                    // 🔥 THE FRAME-ONE SQUAD ALIGNMENT SAVIOR:
                    // If the leader is completely stationary (like right at the start of the level)
                    // and the grunt is far away from its slot, do not let them run inward.
                    // Instead, instantly snap them to their proper slot position or make them wait!
                    if (!isLeaderMoving)
                    {
                        // If they are wildly far away (spawned wrong), snap them physically into formation
                        if (distToSlotSqr > 5f * 5f)
                        {
                            if (rb != null) rb.position = exactSlotTarget;
                            else transform.position = exactSlotTarget;
                        }

                        // Force them to stop ghost-marching on the spawn frame
                        StopMoving();
                        return;
                    }

                    // Standard marching logic resumes only once the leader begins moving
                    if (distToSlotSqr > keepUpRadius * keepUpRadius)
                    {
                        MoveTowards(exactSlotTarget, false);
                    }
                    else
                    {
                        rb.linearVelocity = myLeader.rb.linearVelocity;
                        if (animator != null) animator.SetBool("isMoving", true);
                    }
                    return;
                }
                else
                {
                    StopMoving();
                }
                break;
        }
    }

    // 🔥 FIXED FORMATION FEEDBACK LOOP: Returns the actual designated squad offset coordinates
    public virtual Vector2 GetSlotPosition(int index)
    {
        Vector2 origin = (rb != null) ? rb.position : (Vector2)transform.position;
        return origin + myFormationSpot;
    }

    // --- REFACTORED COMBO ENGINE (SQR MAGNITUDE OPTIMIZATION) ---
    // Swapped all Vector2.Distance calls out for lightning-fast sqrMagnitude calculations!
    private bool IsTargetInRangeForCombo()
    {
        if (currentTarget == null) return false;

        Vector2 myPos = (rb != null) ? rb.position : (Vector2)transform.position;
        float maxComboDist = attackRadius * 1.5f;

        return ((Vector2)currentTarget.position - myPos).sqrMagnitude <= (maxComboDist * maxComboDist);
    }


    IEnumerator Combo1()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
    }

    IEnumerator Combo2()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
    }

    IEnumerator Combo3()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack3"));
    }

    IEnumerator Combo4()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack3"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack4"));
    }

    IEnumerator Combo5()
    {
        yield return StartCoroutine(PlayAttack("attack1"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack2"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack3"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack4"));
        if (!IsTargetInRangeForCombo()) yield break;
        yield return StartCoroutine(PlayAttack("attack5"));
    }
}