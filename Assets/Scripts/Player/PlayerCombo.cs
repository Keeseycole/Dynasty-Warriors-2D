using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ComboState
{
    None,
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    // 🔥 THE MAPPED CHARGE ATTACK VARIATIONS:
    Charge1, // C1: Pure heavy opener from idle
    Charge2, // C2: Launcher branching off Attack1
    Charge3, // C3: Stun combo branching off Attack2
    Charge4, // C4: Crowd sweeper branching off Attack3
    Charge5  // C5: Mid-air bounce branching off Attack4
}
public class PlayerCombo : MonoBehaviour
{
    PlayerState playerState;
    PlayerController playerController;
    private CharecterAnimations attackAnim;
    private Rigidbody2D rb;
    public Animator myNativeAnimator;

    private bool ActivateResetTimer;
    private float defultComboTimer = .6f;
    private float currentComboTimer;
    private ComboState currentComboState;

    public bool isAttacking;

    [Header("Debug Live Feeds")]
    [Tooltip("Watch this value live while hitting enemies. It will snap to 0 on impact!")]
    public float liveAnimatorSpeedTracker;

    [Header("Attack Movement")]
    public float basicStepForce = 3f;
    public float finisherStepForce = 7f;

    [Header("Combat Tuning")]
    public float attackRange = 1.5f;
    public float finisherRangeMultiplier = 1.66f;
    public float basicHitLagDuration = 0.08f;
    public float finisherHitLagDuration = 0.15f;

    [Header("Combat Tuning (Loaded Dynamically)")]
    [Tooltip("This value is automatically overwritten at runtime by your chosen CharacterData profile!")]
    public float baseAttackRadius = 1.5f; // Acts as a fallback baseline


    [Header("Anti-Frame Skip Tuning")]
    private bool inputQueuedForNextAttack = false;

    [Tooltip("The absolute minimum time an animation MUST play before the next combo step is allowed to execute. Adjust this to match your sprite speeds!")]
    public float minimumTimeBetweenStrikes = 0.28f;
    private float lastStrikeTime;

    // Defines the precise frame window near the end of an animation where 
    // tapping the key saves your next attack (e.g., between 20% and 90% of the clip)
    public float comboBufferWindow = 0.45f;

    [Header("Rhythmic Pacing Tuning")]
    [Tooltip("How long each basic attack animation clip literally takes to play (in seconds).")]
    public float attackClipDuration = 0.35f;

    [Tooltip("The extra pause window the game MUST wait AFTER the animation finishes before allowing the next combo step to fire.")]
    public float postAttackPauseWindow = 0.15f;

    private float nextAllowedStrikeTime;

    void Awake()
    {
        attackAnim = GetComponent<CharecterAnimations>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        myNativeAnimator = GetComponent<Animator>();
        if (myNativeAnimator == null)
        {
            myNativeAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        currentComboTimer = defultComboTimer;
        currentComboState = ComboState.None;

        // 🔥 THE INITIALIZATION CLEANUP OVERRIDE:
        // Forcefully push the allowed strike checkpoint into the future on boot frame,
        // preventing the first manual key click from triggering a ghost double-input!
        nextAllowedStrikeTime = Time.time;
        inputQueuedForNextAttack = false;
    }
    void Update()
    {
        if (myNativeAnimator != null)
        {
            liveAnimatorSpeedTracker = myNativeAnimator.speed;
        }

        if (myNativeAnimator != null && myNativeAnimator.speed == 0f)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        ComboAttacks();
        ResetComboState();
    }

    public void InitializeCharacterRange(float customRangeData)
    {
        baseAttackRadius = customRangeData;
        Debug.Log($"<color=#00FFFF>[WEAPON INITIALIZED]:</color> Character weapon reach dynamically set to <b>{baseAttackRadius}</b> units based on character select profile choice.");
    }
    public void ProcessHeroAttackInput(InputAction.CallbackContext context)
    {
        // 🟢 FILTER 1: Only allow execution on the exact frame the key is pressed down!
        if (!context.performed) return;

        if (currentComboState == ComboState.Attack5) return;

        // 🟢 FILTER 2: Safe clock pacing checks
        if (Time.time < nextAllowedStrikeTime)
        {
            inputQueuedForNextAttack = true;
            Debug.Log("<color=yellow>[PACE GATE]:</color> Input successfully queued into the combo buffer layout.");
            return;
        }

        inputQueuedForNextAttack = false;
        ExecuteNextComboStrike();
    }
    public void ComboAttacks()
    {
        // 🟢 1. STANDARD NORMAL ATTACK SCANNER (Z KEY)
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetButtonDown("Fire1"))
        {
            if (currentComboState == ComboState.Attack5) return;

            if (isAttacking)
            {
                inputQueuedForNextAttack = true;
                return;
            }

            inputQueuedForNextAttack = false;
            ExecuteNextComboStrike(false); // Processes normal branch
        }

        // 🔥 2. THE MUSOU CHARGE ATTACK INTERCEPTOR (X KEY)
        if (Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Fire2"))
        {
            if (isAttacking && inputQueuedForNextAttack) return; // Prevent double-triggering inputs

            // Forcefully branch straight out of your active normal sequence into the heavy finisher!
            ExecuteNextComboStrike(true);
        }
    }

   private void ExecuteNextComboStrike(bool triggerChargeAttack)
    {
        CharecterAnimations animScript = GetComponent<CharecterAnimations>();
        if (animScript == null) animScript = GetComponentInChildren<CharecterAnimations>();

        // 1. Calculate what our upcoming target combo state WOULD be
        ComboState prospectiveState = currentComboState;
        if (triggerChargeAttack)
        {
            switch (currentComboState)
            {
                case ComboState.None:    prospectiveState = ComboState.Charge1; break;
                case ComboState.Attack1: prospectiveState = ComboState.Charge2; break;
                case ComboState.Attack2: prospectiveState = ComboState.Charge3; break;
                case ComboState.Attack3: prospectiveState = ComboState.Charge4; break;
                case ComboState.Attack4: prospectiveState = ComboState.Charge5; break;
                default: return;
            }
        }
        else
        {
            prospectiveState++;
        }

        // 🔥 THE INSPECTOR/ANIMATOR SAFETY GATES:
        // Converts the upcoming enum state directly to its exact string name parameter (e.g., "attack4" or "charge3")
        // and checks if that parameter actually exists in the active character's Animator component!
        string requiredParameterName = prospectiveState.ToString().ToLower();

        if (myNativeAnimator != null && !HasAnimatorParameter(myNativeAnimator, requiredParameterName))
        {
            // 🛑 CRITICAL RESCUE: If the character asset doesn't have this attack built yet,
            // gracefully break the combo chain, return to idle, and stop execution before a freeze happens!
            Debug.LogWarning($"<color=orange>[COMBO CAP OVERRIDE]:</color> Selected character does not have <b>{requiredParameterName}</b> configured yet! Safety reset triggered.");
            
            // If they pressed a normal attack button but hit a cap, loop back to the first attack
            if (!triggerChargeAttack && currentComboState != ComboState.None)
            {
                currentComboState = ComboState.None;
                ExecuteNextComboStrike(false); // Loops back to Attack1 cleanly
            }
            else
            {
                FinishAttack(); // Hard stop and return to run/idle
            }
            return;
        }

        // 2. STANDARD ATTACK EXECUTION (Only runs if the state successfully passes the animator check!)
        SoundManager.Instance.PlaySFX("swordswing", 0.8f, 0.05f);
        inputQueuedForNextAttack = false;

        if (animScript != null) animScript.ResetAllAttackStates();

        // Commit to our verified state
        currentComboState = prospectiveState;

        bool isHeavyStrike = currentComboState >= ComboState.Charge1 || currentComboState == ComboState.Attack5;
        float force = isHeavyStrike ? finisherStepForce : basicStepForce;
        Vector2 stepDir = playerController != null ? playerController.lastLookDir : Vector2.down;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(stepDir * force, ForceMode2D.Impulse);
        }

        ActivateResetTimer = true;
        currentComboTimer = defultComboTimer;
        isAttacking = true;

        if (playerController != null) playerController.currentState = PlayerState.attack;

        // Fire your character's adaptive animation controller layers safely
        switch (currentComboState)
        {
            case ComboState.Attack1: attackAnim.Attack1(); break;
            case ComboState.Attack2: attackAnim.Attack2(); break;
            case ComboState.Attack3: attackAnim.Attack3(); break;
            case ComboState.Attack4: attackAnim.Attack4(); break;
            case ComboState.Attack5: attackAnim.Attack5(); break;
            
            case ComboState.Charge1: attackAnim.Charge1(); break; 
            case ComboState.Charge2: attackAnim.Charge2(); break; 
            case ComboState.Charge3: attackAnim.Charge3(); break; 
            case ComboState.Charge4: attackAnim.Charge4(); break; 
            case ComboState.Charge5: attackAnim.Charge5(); break; 
        }
    }

    private void ExecuteNextComboStrike()
    {
        // 🔥 THE INPUT DOUBLE-TRIGGER SHIELD:
        // Forcefully wipe the queue buffer flag to FALSE the exact frame a hit initiates.
        // This stops overlapping inputs from triggering a ghost Attack2 string automatically!
        inputQueuedForNextAttack = false;

        SoundManager.Instance.PlaySFX("swordswing", 0.8f, 0.05f);

        CharecterAnimations animScript = GetComponent<CharecterAnimations>();
        if (animScript == null) animScript = GetComponentInChildren<CharecterAnimations>();
        if (animScript != null)
        {
            animScript.ResetAllAttackStates();
        }

        currentComboState++; // Moves from Attack1 -> Attack2 -> Attack3 fluidly

        float totalLockDuration = attackClipDuration + postAttackPauseWindow;
        nextAllowedStrikeTime = Time.time + totalLockDuration;

        ActivateResetTimer = true;
        currentComboTimer = defultComboTimer;
        isAttacking = true;

        if (playerController != null)
        {
            playerController.currentState = PlayerState.attack;
        }

        float force = (currentComboState == ComboState.Attack5) ? finisherStepForce : basicStepForce;
        Vector2 stepDir = playerController != null ? playerController.lastLookDir : Vector2.down;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(stepDir * force, ForceMode2D.Impulse);
        }

        switch (currentComboState)
        {
            case ComboState.Attack1: attackAnim.Attack1(); break;
            case ComboState.Attack2: attackAnim.Attack2(); break;
            case ComboState.Attack3: attackAnim.Attack3(); break;
            case ComboState.Attack4: attackAnim.Attack4(); break;
            case ComboState.Attack5: attackAnim.Attack5(); break;
        }
    }
    public void ResetComboState()
    {
        if (isAttacking)
        {
            currentComboTimer -= Time.deltaTime;

            // If an input is waiting in our queue, and real-time has safely crossed 
            // our future allowed strike checkpoint, launch the next hit automatically!
            if (inputQueuedForNextAttack && Time.time >= nextAllowedStrikeTime)
            {
                ExecuteNextComboStrike();
                return;
            }

            // 🔥 THE AUTOMATED ANIMATION CLEAN-UP SAFETY NET:
            // If the 0.6s combo cooldown window completely expires with no more inputs,
            // we forcefully clear out all visual locks and parameter flags!
            if (currentComboTimer <= 0)
            {
                inputQueuedForNextAttack = false;
                currentComboState = ComboState.None;
                isAttacking = false;
                currentComboTimer = defultComboTimer; // Reset clock baseline

                // 1. Forcefully turn OFF your character's adaptive animation states (Bools/Triggers)
                CharecterAnimations animScript = GetComponent<CharecterAnimations>();
                if (animScript == null) animScript = GetComponentInChildren<CharecterAnimations>();

                if (animScript != null)
                {
                    // This flips Sun Shang Xiang's active attack bool checkboxes back to FALSE!
                    animScript.ResetAllAttackStates();
                    animScript.AnimationFinished(); // Releases internal state references
                }
                else if (myNativeAnimator != null)
                {
                    // Local fallback if the script component wasn't found
                    myNativeAnimator.SetBool("isMoving", false);
                    myNativeAnimator.ResetTrigger("attack");
                }

                // 2. Restore manual running controls to your main player controller script
                if (playerController == null)
                {
                    playerController = GetComponent<PlayerController>();
                    if (playerController == null) playerController = GetComponentInParent<PlayerController>();
                }

                if (playerController != null)
                {
                    playerController.currentState = PlayerState.idle;
                }

                Debug.Log("<color=orange>[COMBO CLEANUP]:</color> Character attack frame cleared. Visuals successfully reset back to idle.");
            }
        }
    }
   private bool HasAnimatorParameter(Animator animatorComponent, string paramName)
    {
        foreach (AnimatorControllerParameter param in animatorComponent.parameters)
        {
            // Verifies spelling completely independent of case-sensitivity
            if (param.name.ToLower() == paramName.ToLower()) return true;
        }
        return false;
    }

    public void FinishAttack()
    {
        // 🔥 THE AUTOMATED EXECUTION SPLICE:
        // The current swing animation has completed its physical frames. 
        // If the player mashed the key early, unleash the next combo strike instantly!
        if (inputQueuedForNextAttack)
        {
            inputQueuedForNextAttack = false; // Reset the buffer checkbox flag
            ExecuteNextComboStrike(); // Advances to your next hit smoothly
        }
        else
        {
            // If they stopped mashing keys, safely shut down the combat variables
            isAttacking = false;
            currentComboState = ComboState.None;

            if (playerController != null)
            {
                playerController.currentState = PlayerState.idle;
            }
        }
    }
   
    public void CheckForHit()
    {
        // 🔥 THE EMERGENCY RUNTIME REFERENCE GATE:
        // If the character select manager instantiated this script and playerController 
        // hasn't bound yet, forcefully cache it right now to prevent a fatal Null crash!
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null) playerController = GetComponentInParent<PlayerController>();
        }

        float currentRange = attackRange;
        float damage = 10f;
        float knockbackForce = .5f;

        bool isFinisher = (currentComboState == ComboState.Attack5);

        if (isFinisher)
        {
            currentRange = attackRange * finisherRangeMultiplier;
            damage = 13f;
            knockbackForce = 1.5f;
        }

        LayerMask enemyLayer = LayerMask.GetMask("Enemy");

        // Safe ternary operator fallback to ensure attackDir never fails if the controller is missing
        Vector2 attackDir = (playerController != null) ? playerController.lastLookDir : Vector2.down;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;

        // 🟢 LIVE FEEDBACK DEBUG ROW:
        Debug.Log($"<color=#00FFFF>[COMBO SCANNER]:</color> Checking circle sweep at {attackPos} with range {currentRange} on Enemy mask.");

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, currentRange, enemyLayer);

        // 🟢 CRITICAL LIVE NUMBERS VISUALLY DISPATCHED TO CONSOLE:
        Debug.Log($"<color=#FFFF00>[OVERLAP COLLISION RESULT]:</color> Physics sweep returned <b>{hits.Length}</b> target bodies on layer '{LayerMask.LayerToName(hits.Length > 0 ? hits[0].gameObject.layer : 0)}'.");

        List<MonoBehaviour> victimsThisFrame = new List<MonoBehaviour>();

        foreach (Collider2D enemy in hits)
        {
            if (enemy == null || enemy.gameObject == this.gameObject) continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) enemyHealth = enemy.GetComponentInChildren<Health>();

            if (enemyHealth == null) continue;
            if (enemyHealth.currentHealth <= 0) continue;

            Vector2 dir = (enemy.transform.position - transform.position).normalized;
            Vector2 resultingForce = dir * knockbackForce;

            if (!isFinisher)
            {
                Vector2 pullVector = (attackPos - (Vector2)enemy.transform.position).normalized;
                resultingForce = (dir + pullVector * 0.8f).normalized * knockbackForce;
            }

            enemyHealth.TakeDamage(damage, transform.position, resultingForce, myNativeAnimator, rb);
            victimsThisFrame.Add(enemyHealth);

            if (HitParticleManager.Instance != null)
            {
                Vector2 sparkPos = Vector2.Lerp(enemy.transform.position, transform.position, 0.2f);
                HitParticleManager.Instance.SpawnHitSpark(sparkPos, isFinisher, attackDir);
            }
        }

        // Inside PlayerCombo.cs -> CheckForHit() at the very bottom
        if (victimsThisFrame.Count > 0 && HitLagManager.Instance != null)
        {
            float hitStopDuration = isFinisher ?
                HitLagManager.Instance.heavyHitLagDuration :
                HitLagManager.Instance.standardHitLagDuration;

            Vector2 combinedStructuralKnockback = attackDir * (isFinisher ? 14f : 5f);

            HitLagManager.Instance.TriggerBasaraHitLag(
                myNativeAnimator,
                rb,
                victimsThisFrame,
                hitStopDuration,
                combinedStructuralKnockback
            );

            if (ComboCounterHUD.Instance != null)
            {
                ComboCounterHUD.Instance.AddHit(victimsThisFrame.Count);
            }

            if (CameraShake.Instance != null)
            {
                if (isFinisher) CameraShake.Instance.HitPunch(attackDir, 0.6f, hitStopDuration + 0.08f);
                else CameraShake.Instance.HitPunch(attackDir, 0.2f, hitStopDuration + 0.04f);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (playerController == null) return;
        Gizmos.color = Color.red;
        Vector2 attackDir = playerController.lastLookDir;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;
        float finalRange = (currentComboState == ComboState.Attack5) ? (attackRange * finisherRangeMultiplier) : attackRange;
        Gizmos.DrawWireSphere(attackPos, finalRange);
    }
}