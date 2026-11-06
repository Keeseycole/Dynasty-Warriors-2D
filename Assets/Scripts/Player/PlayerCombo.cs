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
   
    Charge1, 
    Charge2, 
    Charge3, 
    Charge4, 
    Charge5  
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
    public ComboState currentComboState;

    public bool isAttacking;


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

    [Header("🔥 Musou Special Gauge Matrix")]
    [SerializeField] private UnityEngine.UI.Slider musouBarSlider;

    public float maxMusouEnergy { get; set; } = 100f;
    public float _currentMusouEnergy = 0f;

    [Tooltip("How long your special invincibility and damage-boost attack flurry lasts (in seconds).")]
    public float musouSpecialDuration = 2.5f;
    private bool isExecutingMusouSpecial = false;

  
    // 🟢 FIXED: Expose the Fill Image directly to the inspector!
    [Tooltip("Drag and drop your slider's child 'Fill' Image component straight into this slot.")]
    [SerializeField] private UnityEngine.UI.Image musouFillImage;

    private bool alternateStrike = false;

    private Color originalBarColor;

    public CharacterData selectedCharacterProfile;

    [Header("🔥 Musou Flash Customization")]
    [Tooltip("The color the bar will vividly pop to the exact frame it hits maximum capacity.")]
    public Color maxFlashTargetColor = Color.white;

    [Tooltip("How long the complete flash cycle takes from start to finish (in seconds).")]
    public float maxFlashTotalDuration = 0.24f;

    private bool hasFlashedMax = false;
    private bool isCurrentlyFlashingBar = false; // Blocks UpdateMusouUI from fighting the flash

    // This securely buffers our custom flash intensity calculation apart from standard slider math
    private float customFlashIntensity = 0f;

    [Header("🔥 Musou Crisis Passive Fill Tuning")]
    [Tooltip("Amount of energy gained when taking a physical hit from an enemy unit.")]
    public float musouGainPerHitTaken = 5f;

    [Tooltip("The health percentage threshold (0.0 to 1.0) where passive regeneration activates (e.g., 0.30 = 30% health).")]
    [Range(0f, 1f)]
    public float passiveRegenHealthThreshold = 0.30f;

    [Tooltip("How much energy fills per second when sitting below the health threshold margin.")]
    public float passiveRegenEnergyPerSecond = 5f;

    // Cache reference to optimize health parameter checking frame-by-frame
    private PlayerHealth cachedPlayerHealth;

    public float musouPixelsPerPoint = 1.2f; // Match or adjust relative to your health bar scaling!

    private RectTransform musouSliderRect;

    [Header("🔥 Musou Time Scaling")]
    [Tooltip("How many additional seconds are added to the ultimate special duration for EVERY SINGLE individual unit of Max Musou Capacity gained over 100 (e.g., 0.05 means +0.5 seconds per +10 item upgrade).")]
    public float bonusSecondsPerMusouUp = 0.05f;

    // The absolute hard ceiling limit your capacity can ever reach
    [Tooltip("The absolute maximum limit your Max Musou Capacity can reach via permanent upgrades.")]
    public float universalMaxMusouCap = 200f;

    [Header("Combo Audio Pools")]
    [Tooltip("Drag and drop your raw audio clip files directly from your project folders into this list layout slot!")]
    public List<AudioClip> attackSoundClips = new List<AudioClip>(); // 🟩 FIXED: Changed from string to AudioClip assets!

    public AudioClip defaultHitImpactClip;

    private bool canAdvanceCombo = false; // ◄── THE INTERLOCK LOCK: Set to false by default!

    private bool hasStruckTargetsThisSwing = false;

    private float safetyUnlockTime = 0f;

    [Header("🔥 Custom Attack Lunge Matrix")]
    public float lightLungeSpeed = 8f;
    public float heavyLungeSpeed = 15f;
    [Range(0.01f, 0.5f)] public float lungeActiveDuration = 0.12f;
    private Coroutine activeLungeRoutine;

    [Header("🎯 Selective AOE Configuration")]
    [Tooltip("If checked, Charge 1 will act as a full circular crowd-clearing sweep.")]
    public bool charge1IsAOE = false;

    [Tooltip("If checked, Charge 2 will act as a full circular crowd-clearing sweep.")]
    public bool charge2IsAOE = false;

    [Tooltip("If checked, Charge 3 will act as a full circular crowd-clearing sweep.")]
    public bool charge3IsAOE = false;

    [Tooltip("If checked, Charge 4 will act as a full circular crowd-clearing sweep.")]
    public bool charge4IsAOE = true; // Enabled by default for your main finisher!

    [Tooltip("If checked, Charge 5 will act as a full circular crowd-clearing sweep.")]
    public bool charge5IsAOE = true;

    [Header("💥 AOE Blast Zone Tuning")]
    [Tooltip("Base damage dealt by standard light attacks.")]
    public float lightAttackDamage = 10f;

    [Tooltip("Base knockback multiplier applied to standard light attacks.")]
    public float lightAttackKnockback = 0.5f;

    [Tooltip("Damage multiplier applied to heavy finisher/charge attacks (e.g., 2.5 means 250% damage).")]
    public float heavyFinisherDamageMultiplier = 2.5f;

    [Tooltip("Knockback multiplier applied to heavy finisher/charge attacks.")]
    public float heavyFinisherKnockback = 2.5f;

    [Header("📐 Selective AOE Hitbox Radii Customization")]
    [Tooltip("The explicit circle radius size used if Charge 1 is checked as an AOE strike.")]
    public float charge1AOERadius = 2.5f;

    [Tooltip("The explicit circle radius size used if Charge 2 is checked as an AOE strike.")]
    public float charge2AOERadius = 3.0f;

    [Tooltip("The explicit circle radius size used if Charge 3 is checked as an AOE strike.")]
    public float charge3AOERadius = 3.5f;

    [Tooltip("The explicit circle radius size used if Charge 4 is checked as an AOE strike.")]
    public float charge4AOERadius = 5.0f;

    [Tooltip("The explicit circle radius size used if Charge 5 is checked as an AOE strike.")]
    public float charge5AOERadius = 5.5f;

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

        nextAllowedStrikeTime = Time.time;
        inputQueuedForNextAttack = false;

        // 🟢 THE HARD ORIENTATION FIX:
        // Forcefully wipe any cached editor/inspector references and query the Selection Manager directly
        // to guarantee your clone tracks the absolute active profile at runtime!
        if (CharacterSelectManager.Instance != null)
        {
            selectedCharacterProfile = CharacterSelectManager.Instance.GetSelectedCharacter();
        }

        // 2. 🔥 THE REINFORCED CAPACITY FAILURE GUARD:
        if (selectedCharacterProfile != null)
        {
          
            maxMusouEnergy = Mathf.Max(selectedCharacterProfile.maxMusouCapacity, 100f);

            float calculatedReach = Mathf.Max(selectedCharacterProfile.uniqueAttackRadius, 1.6f);
            InitializeCharacterRange(calculatedReach);
            attackRange = calculatedReach;

            if (myNativeAnimator != null && selectedCharacterProfile.animatorController != null)
            {
                myNativeAnimator.runtimeAnimatorController = selectedCharacterProfile.animatorController;
            }

            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.moveSpeed = selectedCharacterProfile.moveSpeed;
            }
        }
        else
        {
            maxMusouEnergy = 100f;
            attackRange = baseAttackRadius;
          
        }

        // 3. Initialize the slider visuals AFTER character data capacity metrics are verified
        InitMusouGauge();

        if (CharacterSelectManager.Instance != null)
        {
            CharacterSelectManager.Instance.BindActivePlayerToUI(this);
        }
    }
   void Update()
    {


        TrackMusouSpecialInput();

        // 🔥 THE PASSIVE EMERGENCY CRITICAL REGEN FILL:
        ProcessLowHealthPassiveGain();

        if (isExecutingMusouSpecial) return;

    
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
           
            return;
        }

        inputQueuedForNextAttack = false;
        ExecuteNextComboStrike();
    }


    // ========================================================================
    // 🟩 UNIFIED LOCK-STEP INPUT STREAM (FIXED FREEZING ON ATTACK1):
    // Simplified your button checks! Pressing buttons mid-swing now strictly sets 
    // your input queue flag. The script waits patiently for the swing to complete 
    // before advancing your counters, completely stopping combo skipping bugs!
    // ========================================================================
    public void ComboAttacks()
    {
        if (isExecutingMusouSpecial) return;

        // 1. LIGHT ATTACK INPUT (J Key / Fire1)
        if (Input.GetButtonDown("Fire1"))
        {
            if (currentComboState == ComboState.Attack5) return;

            if (isAttacking)
            {
                // Mash protection: Queue up your intent safely without skipping states!
                inputQueuedForNextAttack = true;
                return;
            }

            // Starting fresh from neutral idle stance
            inputQueuedForNextAttack = false;
            ExecuteNextComboStrike(false);
        }

        // 2. HEAVY CHARGE ATTACK INPUT (I Key / Fire2)
        if (Input.GetButtonDown("Fire2"))
        {
            if (isAttacking && inputQueuedForNextAttack) return;

            if (isAttacking)
            {
                switch (currentComboState)
                {
                    case ComboState.Attack1: currentComboState = ComboState.Charge1; break;
                    case ComboState.Attack2: currentComboState = ComboState.Charge2; break;
                    case ComboState.Attack3: currentComboState = ComboState.Charge3; break;
                    case ComboState.Attack4: currentComboState = ComboState.Charge4; break;
                    default: return;
                }

                inputQueuedForNextAttack = false;
                ExecuteNextComboStrike(true);
            }
            else
            {
                currentComboState = ComboState.Charge1;
                inputQueuedForNextAttack = false;
                ExecuteNextComboStrike(true);
            }
        }
    }


    private float comboFinisherSafetyGraceWindow = 0f;

    private void ExecuteNextComboStrike(bool triggerChargeAttack)
    {
        canAdvanceCombo = false;
        inputQueuedForNextAttack = false;

        // Fetch and reset CharacterAnimations abstraction layer cleanly
        CharecterAnimations animScript = GetComponent<CharecterAnimations>();
        if (animScript == null) animScript = GetComponentInChildren<CharecterAnimations>();
        if (animScript != null)
        {
            animScript.ResetAllAttackStates();
        }

        if (!triggerChargeAttack)
        {
            if (currentComboState == ComboState.None)
            {
                currentComboState = ComboState.Attack1;
            }
        }

        ComboState absoluteStrikeSnapshot = currentComboState;
        string requiredParameterName = absoluteStrikeSnapshot.ToString().ToLower();

        ResetHitboxGateForNextSwing();

        // Check animator integrity
        if (myNativeAnimator == null)
        {
            myNativeAnimator = GetComponent<Animator>();
            if (myNativeAnimator == null) myNativeAnimator = GetComponentInChildren<Animator>();
        }

        if (myNativeAnimator != null && !HasAnimatorParameter(myNativeAnimator, requiredParameterName))
        {
            if (!triggerChargeAttack && currentComboState != ComboState.None)
            {
                currentComboState = ComboState.None;
                ExecuteNextComboStrike(false);
            }
            else
            {
                FinishAttack();
            }
            return;
        }

        currentComboState = absoluteStrikeSnapshot;

        bool isHeavyStrike = currentComboState >= ComboState.Charge1 || currentComboState == ComboState.Attack5;
        float currentStrikeMaxDuration = isHeavyStrike ? 0.85f : attackClipDuration;
        float totalLockDuration = currentStrikeMaxDuration + postAttackPauseWindow;

        nextAllowedStrikeTime = Time.time + totalLockDuration;
        safetyUnlockTime = Time.time + currentStrikeMaxDuration;
        lastStrikeTime = Time.time;

        if (triggerChargeAttack)
        {
            // 🟢 EXTEND THE SAFETY WINDOW FOR HEAVY CHARGES
            comboFinisherSafetyGraceWindow = Time.time + 0.25f;
        }
        else
        {
            comboFinisherSafetyGraceWindow = 0f;
        }
        ActivateResetTimer = true;
        currentComboTimer = defultComboTimer;
        isAttacking = true;

        if (playerController != null)
        {
            playerController.currentState = PlayerState.attack;
        }

        if (activeLungeRoutine != null) StopCoroutine(activeLungeRoutine);
        activeLungeRoutine = StartCoroutine(SmoothLungeMovementCo(isHeavyStrike));

        if (myNativeAnimator != null)
        {
            // Master flush: wipe out competing bool variables cleanly 
            string[] allComboParams = new string[]
            {
            "attack1", "attack2", "attack3", "attack4", "attack5",
            "charge1", "charge2", "charge3", "charge4", "charge5"
            };

            foreach (string paramName in allComboParams)
            {
                if (paramName != requiredParameterName) // Protect current selection
                {
                    myNativeAnimator.SetBool(paramName, false);
                }
            }

            // Securely activate target charge condition line step
            myNativeAnimator.SetBool(requiredParameterName, true);
        }

        // Call your delayed hit scanner pass
        StartCoroutine(DelayedHitScanRoutine(absoluteStrikeSnapshot, isHeavyStrike));
    }
    private void ExecuteNextComboStrike()
    {
        inputQueuedForNextAttack = false;

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
            currentComboTimer -= Time.deltaTime;

        // ========================================================================
        // 🟩 SYNCHRONIZED CHARGE RECOVERY CONTROLLER:
        // Checks to ensure the microsecond grace window has safely cleared before 
        // allowing your safety clock to trigger FinishAttack() and flush parameters!
        // ========================================================================
        if (currentComboState >= ComboState.Charge1)
        {
            if (Time.time >= comboFinisherSafetyGraceWindow && Time.time >= safetyUnlockTime)
            {
                FinishAttack();
            }
            return; // Protection block shields your finishers from lower timeout wipes!
        }
        else
        {
            if (inputQueuedForNextAttack && Time.time >= nextAllowedStrikeTime)
            {
                inputQueuedForNextAttack = false;

                if (currentComboState >= ComboState.Attack1 && currentComboState < ComboState.Attack5)
                {
                    currentComboState++;
                }

                ExecuteNextComboStrike(false);
                return;
            }
        }

        if (currentComboTimer <= 0)
        {
            FinishAttack();
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

    public void ResetHitboxGateForNextSwing()
    {
        hasStruckTargetsThisSwing = false;
    }

    public void FinishAttack()
    {
        //isAnimationLocked = false;
        isAttacking = false;
        inputQueuedForNextAttack = false;
        currentComboState = ComboState.None;
        ActivateResetTimer = false;
        ResetHitboxGateForNextSwing();

        if (playerController != null)
        {
            playerController.currentState = PlayerState.idle; // Restores free-movement control cleanly!
        }

        // Master Flush: Set every single attack and charge bool back to false on combo expiration
        if (myNativeAnimator != null)
        {
            string[] allComboParams = {
                "attack1", "attack2", "attack3", "attack4", "attack5",
                "charge1", "charge2", "charge3", "charge4", "charge5"
            };

            for (int i = 0; i < allComboParams.Length; i++)
            {
                myNativeAnimator.SetBool(allComboParams[i], false);
            }
        }
    }

    public void UnlockComboWindow()
    {
        canAdvanceCombo = true;
    }

    /// <summary>
    /// Place this keyframe event at the absolute final frame of your attack animations.
    /// It ensures that if the player did NOT click any buttons, the state resets safely back to None.
    /// </summary>
    public void ClearComboLockOnAnimationEnd()
    {
        canAdvanceCombo = false;
        if (!inputQueuedForNextAttack)
        {
            currentComboState = ComboState.None;
            isAttacking = false;
            if (playerController != null) playerController.currentState = PlayerState.idle;
        }
    }


    private void InitMusouGauge()
    {
        if (selectedCharacterProfile == null && CharacterSelectManager.Instance != null)
        {
            selectedCharacterProfile = CharacterSelectManager.Instance.GetSelectedCharacter();
        }

        if (musouBarSlider == null)
        {
            GameObject musouGo = GameObject.Find("Musou Slider");
            if (musouGo != null) musouBarSlider = musouGo.GetComponent<UnityEngine.UI.Slider>();

            if (musouBarSlider == null)
            {
                GameObject taggedGo = GameObject.FindWithTag("MusouBar");
                if (taggedGo != null) musouBarSlider = taggedGo.GetComponent<UnityEngine.UI.Slider>();
            }
        }

        if (musouFillImage == null && musouBarSlider != null)
        {
            Transform explicitFillTransform = musouBarSlider.transform.Find("Fill Area/Fill");
            if (explicitFillTransform != null) musouFillImage = explicitFillTransform.GetComponent<UnityEngine.UI.Image>();
        }

        if (selectedCharacterProfile != null)
        {
            maxMusouEnergy = Mathf.Max(selectedCharacterProfile.maxMusouCapacity, 100f);
        }
        else
        {
            maxMusouEnergy = 100f;
        }

        _currentMusouEnergy = 0f;

        if (musouFillImage != null)
        {
            // Lock down a pristine full-opacity baseline color channel 
            Color solidAlphaColor = musouFillImage.color;
            solidAlphaColor.a = 1f;
            originalBarColor = solidAlphaColor;
        }

        // 🔥 THE RACE CONDITION BYPASS:
        // Instead of forcing value metrics onto the canvas this exact frame, 
        // start a short routine that waits for Unity's rendering loops to settle!
        StartCoroutine(DelayedUIRefreshRoutineCo());
    }

    // 🔥 THE INITIALIZATION REFRESH TIMELINE:
    private System.Collections.IEnumerator DelayedUIRefreshRoutineCo()
    {
        // Wait until all layout loops, Canvas builders, and parents are fully drawn
        yield return new WaitForEndOfFrame();

        if (maxMusouEnergy <= 0) maxMusouEnergy = 100f;

        // Forcefully push metrics once layout calculations are established [1]
        UpdateMusouUI();

      
    }
    public void TrackMusouSpecialInput()
  {
      // 🔥 Only pulse back and forth if it's already full AND the entry flash routine is done
      if (musouFillImage != null && currentMusouEnergy >= maxMusouEnergy && !isExecutingMusouSpecial && hasFlashedMax)
      {
          // Your existing pulse line
          float pulseWave = 0.3f + Mathf.PingPong(Time.unscaledTime * 4.5f, 0.7f);
          musouFillImage.color = Color.Lerp(Color.yellow, Color.white, pulseWave);
      }

      // Input listeners
      if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetButtonDown("Submit"))
      {
          if (currentMusouEnergy >= maxMusouEnergy && !isExecutingMusouSpecial)
          {
              StartCoroutine(ExecuteMusouSpecialAttackCo());
          }
      }
  }

    private void TriggerMusouFlurryStrike()
    {
        // 1. Process area damage via your existing hit scanner
        CheckForHit();

        // 2. Fetch your clean character animation abstraction layer
        CharecterAnimations animScript = GetComponent<CharecterAnimations>();
        if (animScript == null) animScript = GetComponentInChildren<CharecterAnimations>();

        if (animScript != null)
        {
            // Reset standard combo string checkmarks to clear structural locks
            animScript.ResetAllAttackStates();

            // 🟢 FIXED: Alternates perfectly between Attack1 and Attack2 instead of picking at random!
            if (alternateStrike)
            {
                animScript.Attack1();
            }
            else
            {
                animScript.Attack2();
            }

            // Flip the checkbox flag so the very next tick plays the opposite animation strike
            alternateStrike = !alternateStrike;
        }
        else if (myNativeAnimator != null)
        {
            // Fallback emergency safety net: Use your parameter panel settings directly if script is missing
            string fallbackParam = alternateStrike ? "attack1" : "attack2";
            myNativeAnimator.SetTrigger(fallbackParam);
            alternateStrike = !alternateStrike;
        }

        // 3. Keep applying forward lunges so your character cuts through dense groups of grunts
        float force = basicStepForce * 1.2f;
        Vector2 stepDir = playerController != null ? playerController.lastLookDir : Vector2.down;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(stepDir * force, ForceMode2D.Impulse);
        }
    }

    private IEnumerator ExecuteMusouSpecialAttackCo()
    {
        isExecutingMusouSpecial = true;

        // 🔥 STEP 1: CALCULATE THE DURATION PER INDIVIDUAL POINT EXTENSION
        // 100f remains our standard baseline starting capacity.
        // We isolate the exact amount of single raw capacity points gained over that limit.
        float singlePointsEarnedOverBaseline = maxMusouEnergy - 100f;

        // Safeguard to ensure it doesn't subtract time if max capacity is somehow under 100
        if (singlePointsEarnedOverBaseline < 0) singlePointsEarnedOverBaseline = 0f;

        // Total Dynamic Duration = Baseline Duration + (Raw Single Points * Your Inspector Setting)
        float calculatedDynamicDuration = musouSpecialDuration + (singlePointsEarnedOverBaseline * bonusSecondsPerMusouUp);

       

        // Grant the player absolute combat invincibility using the new calculated length
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null) health = GetComponentInChildren<PlayerHealth>();
        if (health != null) health.invincibilityDuration = calculatedDynamicDuration + 0.5f;

        // 2. Heavy cinematic freeze-frame presentation
        if (SoundManager.Instance != null) //SoundManager.Instance.PlaySFX("MusouActivateScream", 1f);
        Time.timeScale = 0.4f;
        yield return new WaitForSecondsRealtime(0.4f);
        Time.timeScale = 1f; // Restore normal speed

        // 3. THE FRAME-BY-FRAME DRAIN AND BUTTON-HOLD LOOP:
        float continuousAttackTimer = 0f;
        float musouHeldAttackRate = 0.28f;

        while (currentMusouEnergy > 0f)
        {
            bool isHoldingAttack = Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Submit") || Input.GetButton("Fire1");

            if (!isHoldingAttack)
            {
               
                break;
            }

            // Uses your customized dynamic duration bounds so the slider gauge drains flawlessly
            float burnAmountPerFrame = (maxMusouEnergy / calculatedDynamicDuration) * Time.deltaTime;
            currentMusouEnergy -= burnAmountPerFrame;

            if (continuousAttackTimer > 0f) continuousAttackTimer -= Time.deltaTime;

            if (continuousAttackTimer <= 0f)
            {
                TriggerMusouFlurryStrike();
                continuousAttackTimer = musouHeldAttackRate;
            }

            yield return null; // Wait for the very next frame
        }

        // =========================================================================
        // 4. Safe recovery cleanup
        // =========================================================================
        isExecutingMusouSpecial = false;
        isAttacking = false;
        currentComboState = ComboState.None;
        inputQueuedForNextAttack = false;

        if (health != null) health.invincibilityDuration = 0.5f;

        if (playerController != null)
        {
            playerController.currentState = PlayerState.idle;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (myNativeAnimator != null)
        {
            myNativeAnimator.Rebind();
            myNativeAnimator.Update(0f);
        }

        UpdateMusouUI();

       
    }
    public float currentMusouEnergy
    {
       get => _currentMusouEnergy;
        set
        {
            // 🌟 CHANGED: Lowers the absolute minimum filter down to 10f 
            // so base stats like 50 don't get forced back to 100!
            if (maxMusouEnergy <= 0) maxMusouEnergy = 10f; 

            float safeMax = Mathf.Max(maxMusouEnergy, 10f);
            _currentMusouEnergy = Mathf.Clamp(value, 0f, safeMax);

            if (_currentMusouEnergy >= safeMax)
            {
                if (!hasFlashedMax && !isExecutingMusouSpecial)
                {
                    hasFlashedMax = true;

                    // 🌟 THE INSTANT INTERCEPT PLUG:
                    // We lock down control here to stop UpdateMusouUI from fighting the coroutine!
                    isCurrentlyFlashingBar = true;

                    StopCoroutine("FlashBarRoutineCo");
                    StartCoroutine("FlashBarRoutineCo");
                }
            }
            else
            {
                hasFlashedMax = false;
            }

            // Standard sliders updates can still scale the fill area width handle,
            // but the color channel values are protected by our intercept flag!
            UpdateMusouUI();
        }
    }

    private void UpdateMusouUI()
    {
        if (maxMusouEnergy > universalMaxMusouCap) maxMusouEnergy = universalMaxMusouCap;

        // 🌟 CHANGED: Lowers layout fallback safety down to match your minimum base
        if (maxMusouEnergy <= 0) maxMusouEnergy = 10f;

        if (musouBarSlider != null)
        {
            if (musouSliderRect == null)
            {
                musouSliderRect = musouBarSlider.GetComponent<RectTransform>();
            }

            // 🌟 MATCHES HEALTH BAR DIMENSIONS PERFECTLY:
            if (musouSliderRect != null)
            {
                float calculatedWidth = maxMusouEnergy * musouPixelsPerPoint;
                musouSliderRect.sizeDelta = new Vector2(calculatedWidth, musouSliderRect.sizeDelta.y);
            }

            musouBarSlider.maxValue = maxMusouEnergy;
            musouBarSlider.value = currentMusouEnergy;

            if (musouBarSlider.fillRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(musouBarSlider.fillRect);
            }
        }

        if (musouFillImage != null && !isCurrentlyFlashingBar)
        {
            musouFillImage.color = (currentMusouEnergy >= maxMusouEnergy) ? Color.yellow : originalBarColor;
        }
    }
    public void GainMusouEnergy(float energyAmount)
    {
       


        if (isExecutingMusouSpecial) return;

        currentMusouEnergy += energyAmount;

        if (musouBarSlider != null)
        {
            musouBarSlider.value = currentMusouEnergy;
            musouBarSlider.Select();
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
    public void CheckForHit()
    {
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

        Vector2 attackDir = (playerController != null) ? playerController.lastLookDir : Vector2.down;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, currentRange, enemyLayer);

        List<MonoBehaviour> victimsThisFrame = new List<MonoBehaviour>();
        int hitSuccessCount = 0;

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
            hitSuccessCount++;
            victimsThisFrame.Add(enemyHealth);

    
            if (SoundManager.Instance != null)
            {
                if (attackSoundClips != null && attackSoundClips.Count > 0)
                {
                    int randomIndex = Random.Range(0, attackSoundClips.Count);
                    AudioClip chosenClip = attackSoundClips[randomIndex];

                    if (chosenClip != null)
                    {
                        float randomPitchVariance = Random.Range(-0.05f, 0.05f);
                        
                        Debug.Log($"<color=cyan>[PLAYER AUDIO] Striking target. Randomly selected asset: '{chosenClip.name}' from index {randomIndex}. Sending to SoundManager.</color>");
                        SoundManager.Instance.PlaySFX(chosenClip, 0.7f, randomPitchVariance);
                    }
                }
                else if (defaultHitImpactClip != null)
                {
                    Debug.Log($"<color=cyan>[PLAYER AUDIO] Striking target. List empty, falling back to default asset: '{defaultHitImpactClip.name}'. Sending to SoundManager.</color>");
                    SoundManager.Instance.PlaySFX(defaultHitImpactClip, 0.7f, 0.02f);
                }
                else
                {
                    Debug.LogWarning("[PLAYER AUDIO] Target hit, but both attackSoundClips and defaultHitImpactClip are unassigned in the Inspector!");
                }
            }

            if (HitParticleManager.Instance != null)
            {
                Vector2 sparkPos = Vector2.Lerp(enemy.transform.position, transform.position, 0.2f);
                HitParticleManager.Instance.SpawnHitSpark(sparkPos, isFinisher, attackDir);
            }
        }

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
        }

        if (victimsThisFrame.Count > 0)
        {
            if (ComboCounter.Instance != null)
            {
                ComboCounter.Instance.AddHit(victimsThisFrame.Count);
            }
        }

        if (hitSuccessCount > 0)
        {
            float totalEnergyEarned = hitSuccessCount * 1.5f;
            GainMusouEnergy(totalEnergyEarned);
        }
    }

    private System.Collections.IEnumerator DelayedHitScanRoutine(ComboState attackSnapshot, bool isHeavyAttack)
    {
        // Wait for weapon swing frames to travel outwards cleanly
        yield return new WaitForSeconds(0.05f);

        if (isHeavyAttack)
        {
            yield return new WaitForSeconds(0.04f);
        }

        // 🟢 PASS SNAPSHOT: Forward the locked attack state step down to the area scan math!
        CheckForHitWithSnapshot(attackSnapshot);
    }

    private float GetCustomAOEHitboxRadius(ComboState attackSnapshot, float fallbackBaselineRadius)
    {
        switch (attackSnapshot)
        {
            case ComboState.Charge1: return charge1AOERadius;
            case ComboState.Charge2: return charge2AOERadius;
            case ComboState.Charge3: return charge3AOERadius;
            case ComboState.Charge4: return charge4AOERadius;
            case ComboState.Charge5: return charge5AOERadius;
            case ComboState.Attack5: return fallbackBaselineRadius * finisherRangeMultiplier;
            default: return fallbackBaselineRadius;
        }
    }

    public void CheckForHitWithSnapshot(ComboState attackSnapshot)
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null) playerController = GetComponentInParent<PlayerController>();
        }

        // 1. Evaluate whether this attack uses a specialized AOE blast zone
        bool isAOEBlast = IsCurrentAttackConfiguredAsAOE(attackSnapshot);
        float currentRange = attackRange;

        // Pull individual customized hit radius settings from fields if AOE is toggled on
        if (isAOEBlast)
        {
            currentRange = GetCustomAOEHitboxRadius(attackSnapshot, attackRange);
        }

        LayerMask enemyLayer = LayerMask.GetMask("Enemy");

        // 2. Resolve tracking directions for the overlap scanning center
        Vector2 attackDir = Vector2.right;
        if (playerController != null && playerController.lastLookDir != Vector2.zero)
        {
            attackDir = playerController.lastLookDir.normalized;
        }
        else
        {
            attackDir = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;
        }

        // Centered bursts occur directly over the player; standard cuts shift outward
        Vector2 attackPos = (Vector2)transform.position;
        if (!isAOEBlast)
        {
            attackPos += attackDir * 1.0f;
        }

        // 3. Sweep the physics layer purely to count targets caught in the zone
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, currentRange, enemyLayer);
        int hitSuccessCount = 0;

        foreach (Collider2D enemy in hits)
        {
            if (enemy == null || enemy.gameObject == this.gameObject) continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) enemyHealth = enemy.GetComponentInChildren<Health>();

            // FILTER: Simply verify the target is valid and alive without dealing damage
            if (enemyHealth == null || enemyHealth.currentHealth <= 0) continue;

            hitSuccessCount++;
        }

        // 4. 🔥 THE FUEL INJECTION GATE:
        // Only rewards energy points to your Musou gauge based on enemy crowd density
        if (hitSuccessCount > 0)
        {
            float totalEnergyEarned = hitSuccessCount * 1.5f;
            GainMusouEnergy(totalEnergyEarned);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerController == null) return;
        Gizmos.color = Color.red;

        Vector2 attackDir = playerController.lastLookDir != Vector2.zero ? playerController.lastLookDir.normalized : Vector2.right;
        Vector2 attackPos = (Vector2)transform.position;

        bool isAOEBlast = IsCurrentAttackConfiguredAsAOE(currentComboState);

        if (!isAOEBlast)
        {
            attackPos += attackDir * 1.0f;
        }

        // 🟢 UPDATED: Force editor wireframe to pull the exact configured radius metrics
        float finalRange = isAOEBlast ? GetCustomAOEHitboxRadius(currentComboState, attackRange) : attackRange;

        Gizmos.DrawWireSphere(attackPos, finalRange);
    }

    // 🔥 RE-ALIGN FLUID FLASH COROUTINE:
    private System.Collections.IEnumerator FlashBarRoutineCo()
    {
        if (musouFillImage == null) yield break;

        // (Flag is already forced to true from the property block override above)
        float halfDuration = maxFlashTotalDuration * 0.5f;
        float elapsed = 0f;

        // 1. Smoothly transition up to your custom color choice
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musouFillImage.color = Color.Lerp(Color.yellow, maxFlashTargetColor, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;

        // 2. Smoothly scale back down to standard yellow
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musouFillImage.color = Color.Lerp(maxFlashTargetColor, Color.yellow, elapsed / halfDuration);
            yield return null;
        }

        // Release the hard-lock state
        isCurrentlyFlashingBar = false;
        UpdateMusouUI(); // Safely return the baseline color mix
    }

    private void ProcessLowHealthPassiveGain()
    {
        // Don't add juice if the ultimate is currently executing or if maximum cap is cleared
        if (isExecutingMusouSpecial || currentMusouEnergy >= maxMusouEnergy) return;

        // Lazy-load reference to your player's separate health matrix file component
        if (cachedPlayerHealth == null)
        {
            cachedPlayerHealth = GetComponent<PlayerHealth>();
            if (cachedPlayerHealth == null) cachedPlayerHealth = GetComponentInChildren<PlayerHealth>();
        }

        if (cachedPlayerHealth != null)
        {
            // Assuming your PlayerHealth script exposes currentHealth and maxHealth values:
            float maxHealthValue = Mathf.Max(cachedPlayerHealth.maxHealth, 1f);
            float currentHealthPct = (float)cachedPlayerHealth.currentHealth / maxHealthValue;

            // Check if the current safety margins have dipped below your slider cap threshold
            if (currentHealthPct <= passiveRegenHealthThreshold && cachedPlayerHealth.currentHealth > 0)
            {
                // Multiply our parameter by DeltaTime to maintain consistent growth across framerates
                float frameEnergyRegen = passiveRegenEnergyPerSecond * Time.deltaTime;
                GainMusouEnergy(frameEnergyRegen);
            }
        }
    }

    // 🔥 THE DAMAGE INJECTION RECEIVER INTERFACE:
    // Call this public method directly from your PlayerHealth script inside its TakeDamage routines!
    public void NotifyPlayerTookDamage()
    {
        // Block energy generation from getting hit if the player is currently executing their ultimate fury
        if (isExecutingMusouSpecial) return;

        GainMusouEnergy(musouGainPerHitTaken);
    }

    // 🔥 THE EDITOR HOT-RELOAD LINK:
    // This special Unity hook triggers automatically the exact millisecond you change any 
    // variables inside your Inspector panel, ensuring real-time UI stretching updates!
    private void OnValidate()
    {
        // Safety lock to prevent executing editor checks when the application isn't physically running
        if (!Application.isPlaying) return;

        // Forcefully re-pull profile stats in case you modified the ScriptableObject values directly
        if (selectedCharacterProfile != null)
        {
            maxMusouEnergy = selectedCharacterProfile.maxMusouCapacity;
        }

        // Instantly force your canvas elements to re-evaluate their width dimensions
        UpdateMusouUI();
    }


    private IEnumerator SmoothLungeMovementCo(bool isHeavyFinisher)
    {
        if (rb == null) yield break;

        // 🟢 2D VECTOR CALCULATION:
        // Read the complete direction vector from your player controller tracking system
        Vector2 lungeDirection = Vector2.zero;

        if (playerController != null && playerController.lastLookDir != Vector2.zero)
        {
            // Normalize the vector so diagonal lunges don't accidentally move faster than straight ones
            lungeDirection = playerController.lastLookDir.normalized;
        }
        else
        {
            // Fallback: If no directional data is cached, default along your horizontal transform scale axis
            float fallbackX = transform.localScale.x > 0f ? 1f : -1f;
            lungeDirection = new Vector2(fallbackX, 0f);
        }

        // Pull your speed parameters directly from your custom Inspector properties
        float targetSpeed = isHeavyFinisher ? heavyLungeSpeed : lightLungeSpeed;
        float remainingDuration = lungeActiveDuration;

        // Drive the physical velocity tracking loop across frames
        while (remainingDuration > 0f)
        {
            remainingDuration -= Time.deltaTime;

            // Overwrite both horizontal and vertical axes to apply a clean directional punch
            rb.linearVelocity = lungeDirection * targetSpeed;

            yield return null;
        }

        // Cleanly stop all momentum when the lunge timer expires
        rb.linearVelocity = Vector2.zero;
        activeLungeRoutine = null;
    }

    private bool IsCurrentAttackConfiguredAsAOE(ComboState attackSnapshot)
    {
        // Attack 5 is your primary natural light-combo finisher, so it always sweeps
        if (attackSnapshot == ComboState.Attack5) return true;

        switch (attackSnapshot)
        {
            case ComboState.Charge1: return charge1IsAOE;
            case ComboState.Charge2: return charge2IsAOE;
            case ComboState.Charge3: return charge3IsAOE;
            case ComboState.Charge4: return charge4IsAOE;
            case ComboState.Charge5: return charge5IsAOE;
            default: return false; // Standard Light Attacks 1-4 remain focused cuts
        }
    }

}