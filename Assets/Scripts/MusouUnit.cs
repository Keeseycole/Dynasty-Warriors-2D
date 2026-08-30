using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusouUnit : sleepEnemy
{
    public enum Team { PlayerSide, EnemySide, Neutral }

    public enum AIMission { FollowLeader, CaptureBase, AttackCommander }

    // ========================================================================
    // 🟩 THE MASTER NPC STATS SYSTEM (NEW)
    // Bundles all core numerical properties into a highly optimized, single struct.
    // [System.Serializable] allows you to tune these numbers right inside the Inspector!
    // ========================================================================
    [System.Serializable]
    public struct NPCStats
    {
    
        [Header("Combat Attributes")]
        public float maxHealth;
        public int attackPower;
        public int defensePower;

        [Header("Tactical Mechanics")]
        [Range(0, 100)]
        [Tooltip("Higher morale increases attack speed and helps win off-screen background battles!")]
        public int morale;

    }

    [Header("Unit Stat Assignment Block")]
    public NPCStats stats; // Exposes your custom data struct to the editor window!


   
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
    [HideInInspector] public Vector2 combatOffset = Vector2.zero;


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

    public bool isLunging = false;

    [Header("Combat Lunge Juice Settings")]
    public float lungeForce = 8f;
    public float lungeDuration = 0.15f; // Short, snappy burst of forward speed

    public override void Start()
    {

        SafeSceneInitializationRoutine();

        if (GetComponent<PlayerController>() != null || GetComponentInParent<PlayerController>() != null)
        {
            isOfficer = true; // Forces the player to unlock their full 5-hit combos!
            unitTeam = Team.PlayerSide;

            // Bypass all downstream AI asset loops, base setups, and invoke cycles completely
            return;
        }

        base.Start();

        if (BattlefieldManager.Instance != null)
        {
            BattlefieldManager.Instance.RegisterUnit(this);
        }

        baseAggressionScore = aggressionScore;
   
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
        // 1. Execute standard distance tracking and path calculations safely out of combat frames
        CheckDistance();

     
        if (currentState == EnemyState.Strafe || currentState == EnemyState.Block)
        {
            return; // Let StrafeBehavior() or Block() drive the physical Rigidbody channels completely!
        }

        if (currentState == EnemyState.Death || currentState == EnemyState.Stagger)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            return;
        }

        if (currentState == EnemyState.Attack)
        {
            if (rb != null)
            {
                float speedSqr = rb.linearVelocity.sqrMagnitude;

                // The explosive lunge frame step has completed and slowed back down!
                if (speedSqr < 1.5f)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
            return; // Exit early safely out of standard pathfinding movement loops!
        }

        // 2. THE EXPANDED DESTINATION ANCHOR:
        if (currentState == EnemyState.Idle && myLeader != null)
        {
            if (myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude <= 0.01f)
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
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

        // ========================================================================
        // 🟩 THE MACRO MISSION INJECTOR (FIXED PLACEMENT):
        // We process your squad leader follows and macro point calculations BEFORE 
        // the follower protection layer checks for early returns! This ensures 
        // your path variable chains update perfectly on schedule.
        // ========================================================================
        if (currentTarget == null && currentMission == AIMission.FollowLeader)
        {
            ExecuteMacroMission();
        }

        // ========================================================================
        // 🟩 THE PATH OVERRIDE PROTECTION LAYER:
        // ========================================================================
        var pathFollower = GetComponent<GenericTransformFollower>() ?? GetComponentInParent<GenericTransformFollower>();
        if (pathFollower != null && pathFollower.enabled && pathFollower.isMoving)
        {
            // Forcefully keep your animator boolean parameter set to true!
            animator.SetBool("isMoving", true);

            // Optional Blend Tree support: If your script uses directional vectors on the march,
            // we can dynamically pass its current checkpoint direction headings straight to your hashes!
            Vector2 myPos = transform.position;
            if (pathFollower.pathPoints != null && pathFollower.currentPointIndex < pathFollower.pathPoints.Count)
            {
                Vector2 nextHeading = ((Vector2)pathFollower.pathPoints[pathFollower.currentPointIndex].position - myPos).normalized;
                animator.SetFloat("moveX", nextHeading.x);
                animator.SetFloat("moveY", nextHeading.y);
            }

            // If they are just marching and have no active combat target yet, exit safely now!
            if (currentTarget == null) return;
        }

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

        if (MoraleManager.Instance != null)
        {
            aggressionScore = MoraleManager.Instance.GetAdjustedAggression(this.unitTeam, baseAggressionScore);
        }

        if (readyToSwing && diceRoll < aggressionScore)
        {
            bool directorIsPresent = AttackDirector.instance != null;
            bool tokenAcquired = directorIsPresent && AttackDirector.instance.RequestAttackToken(this, currentTarget);

            if (!directorIsPresent || tokenAcquired)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("swordswing", 0.6f, 0.08f);
                }

                currentState = EnemyState.Attack;

             
                int randomCombo = Random.Range(0, comboList.Count);
                activeAction = StartCoroutine(comboList[randomCombo]());
                yield return activeAction;

                // 🔥 THE TACTICAL BREAK: Enforce a solid internal cooldown window after swinging!
                // Change 1.5f and 3.0f to adjust how long they wait before attacking again.
                attackCooldown = Random.Range(1.5f, 3.0f);
                nextAttackTime = Time.time + attackCooldown;

                if (directorIsPresent && tokenAcquired)
                {
                    AttackDirector.instance.ReturnAttackToken(this, currentTarget);
                }

                currentState = EnemyState.Idle;
            }
            else
            {
                // Attack director token queue was completely full, drop into a brief defensive block
                yield return StartCoroutine(Block(Random.Range(0.5f, 1f)));
            }
        }
        else
        {
  
            float tacticalDiceRoll = Random.value;

            if (tacticalDiceRoll > 0.8f)
            {
                yield return StartCoroutine(Block(Random.Range(0.8f, 1.5f)));
            }
            else
            {
                // This block will now execute perfectly during their cooldown windows!
                yield return StartCoroutine(StrafeBehavior());
            }
        }

        isBusy = false; // Release brain gate cleanly for the next evaluation pass
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

            Vector2 targetVelocity = (sideDir + (correctionDir * 0.5f)).normalized * strafeSpeed;


            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, strafeSpeed * Time.deltaTime * 8f);

            // Safety fallback velocity hard cap inside the frame loop
            if (rb.linearVelocity.sqrMagnitude > strafeSpeed * strafeSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * strafeSpeed;
            }

            ChangeAnim(toTarget);
            timer += Time.deltaTime;
            yield return null;
        }

        // 🔥 HARD FORCED HALT: Force physics to absolute zero before switching states!
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

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

        float baseDamage = (stats.attackPower > 0) ? stats.attackPower : 15f;

        // ========================================================================
        // 🔥 THE MORALE ATTACK POWER BOOST (NEW):
        // Turns your 0-100 morale number into a raw damage multiplier dynamically!
        // Highly motivated troops deal significantly heavier strikes.
        // ========================================================================
        float moraleMultiplier = 1.0f + ((stats.morale - 50f) / 100f);
        float activeStrikeDamage = baseDamage * moraleMultiplier;

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
            // Fallback guard: Uses 'force' multiplier if your script lacks an explicit 'hitForce' property
            Vector2 targetKnockbackForce = knockbackDir * (hitForce > 0.1f ? hitForce : force);

            // Route the damage pass cleanly into your player's accurate tracking script parameters
            playableHeroHealth.TakeDamage(activeStrikeDamage, myPos, targetKnockbackForce);
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

                // ========================================================================
                // 🔥 THE PATH INHERITANCE MOTOR:
                // If our leader dies or becomes disabled, inherit their macro blueprint route!
                // This transitions the grunt from a "Follower" into a "Pathfinder".
                // ========================================================================
                bool isLeaderDead = myLeader == null || !myLeader.enabled || myLeader.currentState == EnemyState.Death;

                if (isLeaderDead)
                {
                    // 1. Try to extract the path points straight from the leader component before losing it
                    if (myLeader != null && myLeader.pathWaypoints != null && myLeader.pathWaypoints.Count > 0)
                    {
                        // Convert our macro thinking type to point-to-point objective pathing
                        currentMission = AIMission.CaptureBase;
                        missionTarget = myLeader.pathWaypoints[Mathf.Clamp(myLeader.currentWaypointIndex, 0, myLeader.pathWaypoints.Count - 1)];

                        // Sever the connection so we don't query a dead object frame loop again
                        myLeader = null;
                    }
                    else
                    {
                        // 2. Fallback: If we can't find a path list, stand fast and hold our position!
                        myLeader = null;
                        StopMoving();
                        return;
                    }
                }

                // ========================================================================
                // STANDARD LEADER-FOLLOWING FORMATION OPERATIONS
                // ========================================================================
                if (myLeader != null)
                {
                    Vector2 leaderPos = (myLeader.rb != null) ? myLeader.rb.position : (Vector2)myLeader.transform.position;
                    Vector2 gruntPhysicalPos = (rb != null) ? rb.position : (Vector2)transform.position;

                    Vector2 exactSlotTarget = leaderPos + myFormationSpot;
                    Vector2 deltaToSlot = exactSlotTarget - gruntPhysicalPos;
                    float distToSlotSqr = deltaToSlot.sqrMagnitude;

                    float keepUpRadius = 1.2f;
                    bool isLeaderMoving = myLeader.rb != null && myLeader.rb.linearVelocity.sqrMagnitude > 0.1f;

                    if (!isLeaderMoving)
                    {
                        if (distToSlotSqr > 5f * 5f)
                        {
                            if (rb != null) rb.position = exactSlotTarget;
                            else transform.position = exactSlotTarget;
                        }

                        StopMoving();
                        return;
                    }

                    if (distToSlotSqr > keepUpRadius * keepUpRadius)
                    {
                        MoveTowards(exactSlotTarget, false);
                    }
                    else
                    {
                        if (rb != null && myLeader.rb != null) rb.linearVelocity = myLeader.rb.linearVelocity;
                        if (animator != null) animator.SetBool("isMoving", true);
                    }
                    return;
                }
                break;

            // ========================================================================
            // 🔥 NEW: INHERITED OBJECTIVE TRACKER
            // Handles guiding the remaining grunts down the inherited waypoint trail!
            // ========================================================================
            case AIMission.CaptureBase:
            case AIMission.AttackCommander:
                if (missionTarget != null)
                {
                    Vector2 physicalPos = (rb != null) ? rb.position : (Vector2)transform.position;
                    MoveTowards(missionTarget.position, false);

                    // Check if the individual grunt reached this point
                    float distToNodeSqr = ((Vector2)missionTarget.position - physicalPos).sqrMagnitude;
                    if (distToNodeSqr <= stoppingDistance * stoppingDistance)
                    {
                        // Stand fast at the objective spot! 
                        StopMoving();
                    }
                }
                break;
        }
    }
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

    public void TriggerLungeFromAnimationEvent()
    {
        // 1. Ensure our Rigidbody cache guard is completely populated on this frame
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 stepDir = Vector2.down;
        float force = 6f; // Standard default fallback step speed

        // 🔹 TRACK THE DIRECTION VECTOR
        // Check if this object is the player controller script layer
        var playerCtrl = GetComponent<PlayerController>() ?? GetComponentInChildren<PlayerController>();
        if (playerCtrl != null)
        {
            // Use the player's last recorded look/aim direction vector!
            stepDir = playerCtrl.lastLookDir != Vector2.zero ? playerCtrl.lastLookDir : Vector2.down;
        }
        else if (currentTarget != null)
        {
            // If it's an AI unit with an active combat target, step straight toward them!
            stepDir = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        }
        else
        {
            // Fallback: March in the direction they are currently moving
            stepDir = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : Vector2.down;
        }

        // 🔹 TRACK THE LUNGE FORCE VALUE
        var playerCombo = GetComponent<PlayerCombo>() ?? GetComponentInChildren<PlayerCombo>();
        if (playerCombo != null)
        {
            // Player reads their explicit 5-stage combo tree parameters
            int numericComboState = (int)playerCombo.currentComboState;
            force = (numericComboState == 5) ? playerCombo.finisherStepForce : playerCombo.basicStepForce;
        }
        else
        {
            // AI units cleanly scale their lunge speed based on their baseline movement speed property
            force = moveSpeed * 2.0f;
        }

        // 2. 🔥 EXPLOSIVE PHYSICAL IMPULSE SNAP: Launch the unit forward!
        rb.linearVelocity = stepDir * force;

        // 3. 🔥 SNAP-BRAKE SAFETY BUFFER:
        // Run a short time-delayed routine to bring them to a dead stop so they don't slide!
        StopCoroutine("ExecuteLungeBrakeRoutine");
        StartCoroutine(ExecuteLungeBrakeRoutine());


    }

    private IEnumerator ExecuteLungeBrakeRoutine()
    {
        // Keep the lunge travel window short and crisp (0.06 seconds)
        yield return new WaitForSeconds(0.06f);

        if (rb != null)
        {
            // Slam the hard arcade brakes instantly!
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.Sleep(); // Instantly clears all cached momentum calculations
        }
    }

    public void SyncIndividualWithGlobalMorale()
    {
        if (MoraleManager.Instance == null) return;

        // 1. Fetch the correct faction score from the global manager instance
        float globalFactionScore = (unitTeam == Team.PlayerSide) ? MoraleManager.Instance.playerFactionMorale : MoraleManager.Instance.enemyFactionMorale;

        // 2. Map the 0-100 global rating directly into this individual unit's local data struct!
        // We cast it to a whole number integer to match your NPCStats system requirements.
        stats.morale = Mathf.RoundToInt(globalFactionScore);

        // 3. Optional: Automatically adjust their animator speed based on confidence shifts!
        //ApplyMoraleSpeedModifiers();
    }

    private IEnumerator SafeSceneInitializationRoutine()
    {
        yield return null; // Wait for Awake passes to clear

        if (MoraleManager.Instance != null)
        {
            if (!MoraleManager.Instance.activeBattlefieldUnits.Contains(this))
            {
                MoraleManager.Instance.activeBattlefieldUnits.Add(this);
            }
            SyncIndividualWithGlobalMorale();
        }

        // ========================================================================
        // 🟩 TIMING CLOCK INITIALIZATION (FIXED):
        // Explicitly sets your next attack allowance frame directly to the current time,
        // completely destroying any unassigned frame-one timing blocks!
        // ========================================================================
        nextAttackTime = Time.time;
        if (attackCooldown <= 0.05f) attackCooldown = 2.0f; // Safe default pacing floor

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        isBusy = false; // Release the brain gate!
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