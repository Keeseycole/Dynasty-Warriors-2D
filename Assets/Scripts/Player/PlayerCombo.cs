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
    public void ComboAttacks()
    {
        if (isExecutingMusouSpecial) return;

        // 🟢 1. STANDARD NORMAL ATTACK SCANNER (Z KEY)
        if (Input.GetButtonDown("Fire1"))
        {
            if (currentComboState == ComboState.Attack5) return;

            // 🟢 THE ADVANCEMENT FIX:
            // If we are already mid-attack, advance your combo tracking counter state 
            // IMMEDIATELY before hitting your early return shields! 
            // This guarantees Attack 2, 3, 4, and 5 successfully lock into system memory.
            if (isAttacking)
            {
                inputQueuedForNextAttack = true;

                if (currentComboState >= ComboState.Attack1 && currentComboState < ComboState.Attack5)
                {
                    currentComboState++;
                   
                }
                return;
            }

            inputQueuedForNextAttack = false;
            ExecuteNextComboStrike(false); // Fires standard Attack 1 from neutral idle
        }

        // 🔥 2. THE MUSOU CHARGE ATTACK INTERCEPTOR (X KEY)
        if (Input.GetButtonDown("Fire2"))
        {
            if (isAttacking && inputQueuedForNextAttack) return;

            ExecuteNextComboStrike(true);
        }
    }

    private void ExecuteNextComboStrike(bool triggerChargeAttack)
    {
        // 🔥 THE INPUT DOUBLE-TRIGGER SHIELD:
        inputQueuedForNextAttack = false;

        ComboState prospectiveState = currentComboState;
        if (triggerChargeAttack)
        {
            switch (currentComboState)
            {
                case ComboState.None: prospectiveState = ComboState.Charge1; break;
                case ComboState.Attack1: prospectiveState = ComboState.Charge2; break;
                case ComboState.Attack2: prospectiveState = ComboState.Charge3; break;
                case ComboState.Attack3: prospectiveState = ComboState.Charge4; break;
                case ComboState.Attack4: prospectiveState = ComboState.Charge5; break;
                default: return;
            }
        }
        else
        {
            // 🟢 FIX: If we are not attacking yet, start at Attack1. 
            // If we are mid-combo, currentComboState was already advanced cleanly by your ComboAttacks() buffer loop!
            if (prospectiveState == ComboState.None)
            {
                prospectiveState = ComboState.Attack1;
            }
        }

        // =========================================================================
        // 🟢 IMMUTABLE LOCAL SNAPSHOT LOGIC
        // This freezes the exact attack index step into an unchangeable local variable!
        // No matter how fast the player mashes keys, this specific instance cannot change.
        // =========================================================================
        ComboState absoluteStrikeSnapshot = prospectiveState;

        string requiredParameterName = absoluteStrikeSnapshot.ToString().ToLower(); // e.g. "attack1"

        // 🔥 THE VALIDATION GATE: Keep spelling checks independent of case-sensitivity
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

        // Standard audio feedback
      //  if (SoundManager.Instance != null)
       // {
        //    SoundManager.Instance.PlaySFX("swordswing", 0.8f, 0.05f);
       // }

        // Sync your global structural tracking state
        currentComboState = absoluteStrikeSnapshot;

        bool isHeavyStrike = currentComboState >= ComboState.Charge1 || currentComboState == ComboState.Attack5;
        float totalLockDuration = attackClipDuration + postAttackPauseWindow;
        nextAllowedStrikeTime = Time.time + totalLockDuration;
        lastStrikeTime = Time.time;

        ActivateResetTimer = true;
        currentComboTimer = defultComboTimer;
        isAttacking = true;

        if (playerController != null)
        {
            playerController.currentState = PlayerState.attack;
        }


        // Value routers matching parameters dynamically based on your configuration parameters setup
        if (myNativeAnimator != null)
        {
            string[] allComboParams = new string[]
            {
            "attack1", "attack2", "attack3", "attack4", "attack5",
            "charge1", "charge2", "charge3", "charge4", "charge5"
            };

            foreach (string paramName in allComboParams)
            {
                foreach (AnimatorControllerParameter parameterObject in myNativeAnimator.parameters)
                {
                    if (parameterObject.name.ToLower() == paramName)
                    {
                        if (parameterObject.type == AnimatorControllerParameterType.Bool)
                        {
                            myNativeAnimator.SetBool(parameterObject.name, false);
                        }
                        else if (parameterObject.type == AnimatorControllerParameterType.Trigger)
                        {
                            myNativeAnimator.ResetTrigger(parameterObject.name);
                        }
                    }
                }
            }

            foreach (AnimatorControllerParameter parameterObject in myNativeAnimator.parameters)
            {
                if (parameterObject.name.ToLower() == requiredParameterName)
                {
                    if (parameterObject.type == AnimatorControllerParameterType.Bool)
                    {
                        myNativeAnimator.SetBool(parameterObject.name, true);
                    }
                    else if (parameterObject.type == AnimatorControllerParameterType.Trigger)
                    {
                        myNativeAnimator.SetTrigger(parameterObject.name);
                    }
                    break;
                }
            }
        }

        // 🟢 PASS SNAPSHOT: Feed your frozen local variable directly down into the timing routine!
        StartCoroutine(DelayedHitScanRoutine(absoluteStrikeSnapshot, isHeavyStrike));
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

        // 🟢 THE BUFFER UNLOADER FIX:
        // Unleash the pre-advanced combo strike step the exact millisecond the pacing clock clears!
        if (inputQueuedForNextAttack && Time.time >= nextAllowedStrikeTime)
        {
            inputQueuedForNextAttack = false;
            
            // Fires your pre-calculated Attack 2, 3, or 4 string cleanly!
            ExecuteNextComboStrike(false); 
            return;
        }

        // Only clear out tracking variables back to None if the player has completely stopped pressing keys
        if (currentComboTimer <= 0)
        {
            inputQueuedForNextAttack = false;
            currentComboState = ComboState.None;
            isAttacking = false;
            currentComboTimer = defultComboTimer;

            CharecterAnimations animScript = GetComponent<CharecterAnimations>();
            if (animScript == null) animScript = GetComponentInChildren<CharecterAnimations>();

            if (animScript != null)
            {
                animScript.ResetAllAttackStates();
                animScript.AnimationFinished();
            }

            if (playerController != null)
            {
                playerController.currentState = PlayerState.idle;
            }
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
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("MusouActivateScream", 1f);
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
        // 🔥 THE EMERGENCY RUNTIME REFERENCE GATE:
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

        // 🟢 TRACKER: Counts exactly how many individual enemy units were struck this swing
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

            // 🟢 FIXED: Increments our hit counter instead of trying to write to the bar inside the freeze block!
            hitSuccessCount++;

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

            if (ComboCounter.Instance != null)
            {
                ComboCounter.Instance.AddHit(victimsThisFrame.Count);
            }

            if (CameraShake.Instance != null)
            {
                if (isFinisher) CameraShake.Instance.HitPunch(attackDir, 0.6f, hitStopDuration + 0.08f);
                else CameraShake.Instance.HitPunch(attackDir, 0.2f, hitStopDuration + 0.04f);
            }
        }

 
        if (hitSuccessCount > 0)
        {
            // Multiply your baseline energy (1.5f) by the total number of enemies struck!
            // Swinging into a dense crowd will now fill up your special meter massively.
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

    public void CheckForHitWithSnapshot(ComboState attackSnapshot)
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null) playerController = GetComponentInParent<PlayerController>();
        }

        // 1. Establish weapon reach parameters dynamically based on the current attack state
        float currentRange = attackRange;
        bool isFinisher = (attackSnapshot == ComboState.Attack5 || (attackSnapshot >= ComboState.Charge1 && attackSnapshot <= ComboState.Charge5));

        if (isFinisher)
        {
            currentRange = attackRange * finisherRangeMultiplier;
        }

        LayerMask enemyLayer = LayerMask.GetMask("Enemy");

        // 2. Calculate the directional vector based on the player's last look direction
        Vector2 attackDir = Vector2.right;
        if (playerController != null && playerController.lastLookDir != Vector2.zero)
        {
            attackDir = playerController.lastLookDir;
        }
        else
        {
            attackDir = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;
        }

        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;

        // 3. Sweep the localized combat area for targets on the Enemy layer
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, currentRange, enemyLayer);
        int hitSuccessCount = 0;

        foreach (Collider2D enemy in hits)
        {
            if (enemy == null || enemy.gameObject == this.gameObject) continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null) enemyHealth = enemy.GetComponentInChildren<Health>();

            // 🟢 FILTER: Ensure the unit is a valid, living target
            if (enemyHealth == null || enemyHealth.currentHealth <= 0) continue;

            // Increment our success counter for this specific frame event pass
            hitSuccessCount++;
        }

        // 4. 🔥 THE FUEL INJECTION GATE:
        // All damage physics, screen shakes, and hitlags are stripped away. 
        // This now ONLY rewards energy points based on the exact amount of enemies caught!
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
        Vector2 attackDir = playerController.lastLookDir;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;
        float finalRange = (currentComboState == ComboState.Attack5) ? (attackRange * finisherRangeMultiplier) : attackRange;
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

}