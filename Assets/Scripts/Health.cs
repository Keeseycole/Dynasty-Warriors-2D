using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MusouUnit;

public class Health : MonoBehaviour
{

    private MusouUnit myUnitComponent;

    public float maxHealth;
    public FloatingHealthBar healthBar;
    public CharacterData stats;

    public float currentHealth;

    private Animator anim;
    private Rigidbody2D rb;
    private MusouUnit unitAI;

    [Tooltip("True = Active/Near Player, False = Culled/Far away")]
    public bool isSimulating = true;

    public SpriteRenderer minimapIconRenderer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Tooltip("The pickable item prefab for the permanent Max Health upgrade (Dim Sum)")]
    [SerializeField] private GameObject healthDropPrefab;

    [Tooltip("The pickable item prefab for the permanent Attack upgrade (Sword)")]
    [SerializeField] private GameObject attackDropPrefab;

    [Tooltip("The pickable item prefab for the permanent Defense upgrade (Shield)")]
    [SerializeField] private GameObject defenseDropPrefab;

    [Range(0f, 100f)]
    [Tooltip("The overall percentage chance that this unit drops ANY item when killed (e.g., 15% for regular grunts).")]
    [SerializeField] private float baseDropChance = 15f;

    // FIX: Separated coroutine trackers so they don't cancel each other out!
    private Coroutine minimapFlashCoroutine;
    private Coroutine hitFlashCoroutine;


    [HideInInspector] public bool blockedOnThisFrame = false;

    private Collider2D myCollider;
    public GameObject flashOverlay;

    public bool isGate;

    // Static variables are shared by ALL instances of Health.cs across the scene
    private static bool hasAnyGateBreachedConversationPlayed = false;

    [Header("Dialogue Object Settings")]
    [Tooltip("Drag and drop your active Conversation GameObject here from the hierarchy window!")]
    public GameObject gateBreachedConversationObject;

    [Header("Background Combat Balance")]
    [HideInInspector]
    public float accumulatedDamageThisTick = 0f; // Tracks combined off-screen hits inside the current frame pass
    private void Awake()
    {
        myUnitComponent = GetComponent<MusouUnit>() ?? GetComponentInChildren<MusouUnit>();

        myCollider = GetComponent<Collider2D>();
        unitAI = GetComponent<MusouUnit>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            Debug.LogError("SpriteRenderer still not found on " + gameObject.name + " or its children!");
        }

        if (flashOverlay != null)
        {
            SpriteRenderer mainRenderer = GetComponentInChildren<SpriteRenderer>();
            SpriteRenderer overlayRenderer = flashOverlay.GetComponent<SpriteRenderer>();

            if (mainRenderer != null && overlayRenderer != null)
            {
                overlayRenderer.sprite = mainRenderer.sprite;
                overlayRenderer.color = Color.white;
                flashOverlay.SetActive(false);
            }
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (BattleManager.Instance != null)
            BattleManager.Instance.activeUnits.Add(this);

        // BUG PREVENTER: Reset the global flag when the level initializes
        if (isGate || gameObject.CompareTag("Gate"))
        {
            hasAnyGateBreachedConversationPlayed = false;
        }
    }

    private void Update()
    {
        // Smoothly clear out accumulated background damage pools on every new physics frame pass
        if (accumulatedDamageThisTick > 0f) accumulatedDamageThisTick = Mathf.MoveTowards(accumulatedDamageThisTick, 0f, Time.fixedDeltaTime * 100f);
    }



    public void TakeDamage(float damage, Vector2 attackerPosition, Vector2 knockback, Animator attackerAnim, Rigidbody2D attackerRb)
    {
        float finalMitigatedDamage = damage;
        int roundedTextValue = Mathf.RoundToInt(finalMitigatedDamage);
        Vector2 textPopupSpawn = (Vector2)transform.position + Vector2.up * 1.2f;
        Color textOutputColor = Color.white;


        if (currentHealth <= 0) return;

        if (unitAI != null)
        {
            // 🔥 THE PERFORMANCE FLOATING CAP: Clamp baseline defense to prevent massive crowd densities
            // from compounding their armor values exponentially behind the scenes!
            int defenseBuffer = Mathf.Clamp(unitAI.stats.defensePower, 0, 40);

            float currentUnitMorale = unitAI.stats.morale;

            // 50 Morale = 1.0x baseline. 100 Morale = 1.5x defense power. 0 Morale = 0.5x defense power.
            float moraleMultiplier = 1.0f + ((currentUnitMorale - 50f) / 100f);

            // Hard clamp the multiplier so their effective defense can never cross unstable limits
            moraleMultiplier = Mathf.Clamp(moraleMultiplier, 0.3f, 1.8f);
            float effectiveDefense = defenseBuffer * moraleMultiplier;

            // Arcade formula: Final Damage = Raw Attack - (Defense / 2)
            float calculatedMitigation = damage - (effectiveDefense * 0.5f);

            // ========================================================================
            // 🟩 THE RETRO ARCADE DAMAGE FLOOR (FIXED FOR 10+ CROWD BLOCKADES):
            // Instead of snapping negative values down to a microscopic 1 HP, we enforce
            // a hard damage floor! A hit is mathematically GUARANTEED to always deal at 
            // least 25% of the raw incoming weapon power. Wipes out crowd invulnerability!
            // ========================================================================
            float minimumDamageFloor = damage * 0.25f;

            finalMitigatedDamage = Mathf.Max(minimumDamageFloor, calculatedMitigation);

            // Final safety round conversion pass
            roundedTextValue = Mathf.RoundToInt(finalMitigatedDamage);
        }

        // REDUCE HEALTH: Subtracted exactly ONCE per combat impact frame!
        currentHealth -= finalMitigatedDamage;

        // ========================================================================
        // 🟩 THE FLOATING DAMAGE TEXT VARIABLE DECLARATIONS (FIXED):
        // This block provides the exact local declarations that your factory lines 
        // need to clear error CS0103 and compile flawlessly!
        // ========================================================================
        if (isSimulating && DamageNumberFactory.Instance != null)
        {

            if (gameObject.CompareTag("Player"))
            {
                textOutputColor = new Color(1f, 0.25f, 0.25f); // Red alert color for player damage
            }
            else if (roundedTextValue > 12)
            {
                textOutputColor = new Color(1f, 0.85f, 0f); // Critical gold text for heavy hits!
            }

            // Calls your factory method pass safely now that variables exist!
            DamageNumberFactory.Instance.BurstDamageNumber(roundedTextValue, textPopupSpawn, textOutputColor);
        }

        if (isSimulating && HitLagManager.Instance != null && attackerAnim != null)
        {
            if (!attackerAnim.CompareTag("Player"))
            {
                List<global::UnityEngine.MonoBehaviour> victimsList = new List<global::UnityEngine.MonoBehaviour> { this };
                bool isHeavy = (finalMitigatedDamage > 12f || knockback.magnitude > 1.2f);
                float selectedDuration = isHeavy ? HitLagManager.Instance.heavyHitLagDuration : HitLagManager.Instance.standardHitLagDuration;

                HitLagManager.Instance.TriggerBasaraHitLag(attackerAnim, attackerRb, victimsList, selectedDuration);
            }
        }

        if (healthBar != null)
        {
            healthBar.UpdateBar(currentHealth, maxHealth);
        }

        // THE AUDIO CULLING GATE
        if (isSimulating && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("HitImpact", 0.7f);

            if (finalMitigatedDamage > 12f)
            {
                SoundManager.Instance.PlaySFX("HeavyImpact", 1f);
            }
        }

        // 4. COMBAT FEEDBACK & SIMULATION
        if (isSimulating)
        {
            if (unitAI != null)
            {
                unitAI.TriggerHit(attackerPosition);
            }

            if (rb != null)
            {
                StartCoroutine(DelayedKnockbackRoutine(knockback));
            }

            if (minimapFlashCoroutine == null)
            {
                minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
            }

            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }
        else
        {
            if (minimapFlashCoroutine == null)
            {
                minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
            }
        }

        // 5. DEATH CHECK & CLEANUP
        if (currentHealth <= 0)
        {
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            if (spriteRenderer != null) spriteRenderer.color = originalColor; // Reset tint

            if (!isGate)
            {
                anim.SetBool("isHit", false);
                anim.SetBool("isBlocking", false);
                anim.SetBool("isDead", true);
            }

            // Standard enemy verification lines
            bool isEnemyByTag = gameObject.CompareTag("Enemy") || gameObject.name.Contains("Soldier");
            bool isEnemyByAI = (unitAI != null && (unitAI.unitTeam == MusouUnit.Team.EnemySide || unitAI.unitTeam == Team.EnemySide));
            bool isAnEnemyUnit = isEnemyByTag || isEnemyByAI;

            if (isAnEnemyUnit)
            {
                // ========================================================================
                // 🟩 THE RE-ENGINEERED RADAR MATRIX (DIAGNOSTIC FIXED):
                // We check if the attackerAnim exists, then climb up its transform hierarchy
                // to pull the true root GameObject name and script properties.
                // ========================================================================
                bool killedByPlayerDirectly = false;

                if (attackerAnim != null)
                {
                    GameObject rootAttacker = attackerAnim.gameObject;

                    // Trace up to find the true root object if the animator is nested inside a child layer
                    if (attackerAnim.transform.parent != null)
                    {
                        // Check if the parent or root carries a custom AI movement component
                        var isAIComponent = attackerAnim.GetComponentInParent<GenericTransformFollower>() ?? attackerAnim.GetComponent<GenericTransformFollower>();

                        // 🔥 THE ULTIMATE AI ALLY EXCLUSION FILTER:
                        // Even if their tag says "Player" by mistake, if they have a path follower script,
                        // they are mathematically PROVEN to be an automated ally, not the real player!
                        if (isAIComponent == null && (rootAttacker.CompareTag("Player") || attackerAnim.transform.parent.CompareTag("Player")))
                        {
                            killedByPlayerDirectly = true;
                        }
                    }
                    else if (rootAttacker.CompareTag("Player") && rootAttacker.GetComponent<GenericTransformFollower>() == null)
                    {
                        killedByPlayerDirectly = true;
                    }


                }

                // Increments your text strings ONLY if the true player struck the blow!
                if (killedByPlayerDirectly && KOCounter.instance != null)
                {
                    KOCounter.instance.AddKO();
                }
            }

            Die();
        }
    }

    public void TakeDamage(float damageAmount, Vector2 attackerPosition, Vector2 knockbackVelocity, GameObject visualEffect = null, string customParameter = null)
    {
        // 1. Convert the float stat cleanly to a whole number integer
        int calculatedDamageValue = Mathf.RoundToInt(damageAmount);

        // ========================================================================
        // 🟩 REDIRECT SIGNATURE ALIGNMENT (FIXED SYNTAX):
        // Explicitly passes component type definitions to the primary method instead of
        // naked null fields, eliminating signature compilation crashes!
        // ========================================================================
        TakeDamage(calculatedDamageValue, attackerPosition, knockbackVelocity, (Animator)null, (Rigidbody2D)null);

        // 3. HARD TRIGGER STAGGER: Force the enemy to break out of their AI pathfinding loops instantly!
        if (unitAI != null)
        {
            unitAI.TriggerHit(attackerPosition);

        }
    }
    public void Die()
    {
        // ========================================================================
        // 🟩 ENDLESS CHAIN PROMOTION & SQUAD MEMBER CARRIER ENGINE (RECURSIVE FIXED):
        // Must run first! Sweeps the entire faction list to find a living fallback grunt
        // that shares this specific commander node instance. It transfers all path parameters,
        // assigns all remaining squad members, and resets references so the chain
        // repeats beautifully on autopilot until the entire column is completely wiped out!
        // ========================================================================
        if (unitAI != null && (gameObject.name.Contains("Leader") || unitAI.isOfficer))
        {
            if (BattlefieldManager.Instance != null)
            {
                var myFactionList = (unitAI.unitTeam == MusouUnit.Team.EnemySide) ?
                                     BattlefieldManager.Instance.enemySideUnits :
                                     BattlefieldManager.Instance.playerSideUnits;

                MusouUnit nextInCommand = null;

                // Find the first available living follower who points to this dying leader
                for (int m = 0; m < myFactionList.Count; m++)
                {
                    MusouUnit candidate = myFactionList[m];

                    if (candidate != null && candidate != unitAI && candidate.currentState != EnemyState.Death)
                    {
                        // Check if this grunt belongs to the dying commander's team context
                        if (candidate.myLeader != null && candidate.myLeader.gameObject == gameObject)
                        {
                            nextInCommand = candidate;
                            break; // Successor found! Exit early to process inheritance channels
                        }
                    }
                }

                if (nextInCommand != null)
                {
                    // Active-Component Tracker Extraction for Dual-Paths
                    GenericTransformFollower oldFollower = null;
                    GenericTransformFollower[] allOldFollowers = GetComponents<GenericTransformFollower>();

                    if (allOldFollowers != null && allOldFollowers.Length > 0)
                    {
                        for (int o = 0; o < allOldFollowers.Length; o++)
                        {
                            if (allOldFollowers[o] != null && allOldFollowers[o].enabled)
                            {
                                oldFollower = allOldFollowers[o];
                                break;
                            }
                        }
                    }

                    if (oldFollower == null)
                    {
                        oldFollower = GetComponent<GenericTransformFollower>()
                                   ?? GetComponentInChildren<GenericTransformFollower>()
                                   ?? GetComponentInParent<GenericTransformFollower>();
                    }

                    // Attach or find the single exclusive SquadLeader component script on the successor
                    SquadLeader promotedLeaderScript = nextInCommand.GetComponent<SquadLeader>();
                    if (promotedLeaderScript == null)
                    {
                        promotedLeaderScript = nextInCommand.gameObject.AddComponent<SquadLeader>();
                    }

                    // Force the new commander command script completely awake right now!
                    promotedLeaderScript.enabled = true;

                    // Ensure their SquadLeader component initializes its member tracking list array
                    // (Change "squadMembers" below to match the exact List variable string in your SquadLeader.cs file!)
                    if (promotedLeaderScript.squadMembers == null)
                    {
                        promotedLeaderScript.squadMembers = new System.Collections.Generic.List<MusouUnit>();
                    }
                    else
                    {
                        promotedLeaderScript.squadMembers.Clear(); // Flush old junk values
                    }

                    if (oldFollower != null)
                    {
                        // Synchronize structural path parameters straight into the SquadLeader script data fields
                        promotedLeaderScript.pathWaypoints = oldFollower.pathPoints;

                        // Fetch EVERY duplicate path follower script running on the grunt prefab to populate them completely!
                        GenericTransformFollower[] allNewFollowers = nextInCommand.gameObject.GetComponents<GenericTransformFollower>();

                        if (allNewFollowers == null || allNewFollowers.Length == 0)
                        {
                            allNewFollowers = new GenericTransformFollower[] { nextInCommand.gameObject.AddComponent<GenericTransformFollower>() };
                        }

                        for (int f = 0; f < allNewFollowers.Length; f++)
                        {
                            GenericTransformFollower currentFollowerScript = allNewFollowers[f];
                            if (currentFollowerScript == null) continue;

                            currentFollowerScript.pathPoints = oldFollower.pathPoints;
                            currentFollowerScript.currentPointIndex = oldFollower.currentPointIndex;
                            currentFollowerScript.movementSpeed = oldFollower.movementSpeed;
                            currentFollowerScript.arrivalThreshold = oldFollower.arrivalThreshold;
                            currentFollowerScript.pathPriority = oldFollower.pathPriority;
                            currentFollowerScript.isMoving = oldFollower.isMoving;
                            currentFollowerScript.rb = nextInCommand.gameObject.GetComponent<Rigidbody2D>();
                            currentFollowerScript.enabled = true;
                        }

                        // Re-brand their identity name string cleanly so they are recognized as a valid leader if *they* fall later!
                        string originalGruntName = nextInCommand.gameObject.name;
                        nextInCommand.gameObject.name = originalGruntName + "_Promoted_Leader";

                        nextInCommand.myLeader = null; // Becomes their own independent master leader root node!

                        // Physical Mass/Weight Inheritance
                        Rigidbody2D oldLeaderRb = GetComponent<Rigidbody2D>();
                        Rigidbody2D nextRb = nextInCommand.gameObject.GetComponent<Rigidbody2D>();

                        if (oldLeaderRb != null && nextRb != null)
                        {
                            nextRb.mass = oldLeaderRb.mass;
                        }

                        if (nextRb != null) nextRb.simulated = true;

                        // ========================================================================
                        // 🟩 SQUAD TRANSFER CYCLER GATELINE (THE INFINITE REPEAT KEY):
                        // Loops through all remaining units on this faction side. If a grunt was 
                        // following the old dead leader, we forcefully point their 'myLeader' handle
                        // to the new 'promotedLeaderScript' component, and log them into the new leader's
                        // 'squadMembers' tracking list array! This seals the endless loop recursion.
                        // ========================================================================
                        for (int m = 0; m < myFactionList.Count; m++)
                        {
                            MusouUnit sibling = myFactionList[m];

                            // Check if this sibling was a teammate under the old dying manager script component
                            if (sibling != null && sibling != nextInCommand && sibling.currentState != EnemyState.Death)
                            {
                                if (sibling.myLeader != null && sibling.myLeader.gameObject == gameObject)
                                {
                                    // 1. Re-anchor their internal target leader node pointer
                                    sibling.myLeader = promotedLeaderScript;

                                    // 2. Register them into the new commander's live squad member collection tracker list
                                    promotedLeaderScript.squadMembers.Add(sibling);
                                }
                            }
                        }

                        UnitCuller nextCuller = nextInCommand.GetComponent<UnitCuller>();
                        if (nextCuller != null) nextCuller.enabled = true;

                        Debug.Log($"[ENDLESS PROMOTION LINKED]: '{nextInCommand.gameObject.name}' has successfully taken command of ({promotedLeaderScript.squadMembers.Count}) surviving squad members!");
                    }
                }
            }
        }

        // ========================================================================
        // 🟩 GLOBAL LIST PURGE (WIPES OUT OFF-SCREEN GHOSTS)
        // ========================================================================
        if (BattlefieldManager.Instance != null && unitAI != null)
        {
            if (BattlefieldManager.Instance.playerSideUnits.Contains(unitAI))
            {
                BattlefieldManager.Instance.playerSideUnits.Remove(unitAI);
            }
            if (BattlefieldManager.Instance.enemySideUnits.Contains(unitAI))
            {
                BattlefieldManager.Instance.enemySideUnits.Remove(unitAI);
            }

            BattlefieldManager.Instance.UnregisterUnit(unitAI);
        }

        if (MoraleManager.Instance != null && unitAI != null)
        {
            if (MoraleManager.Instance.activeBattlefieldUnits.Contains(unitAI))
            {
                MoraleManager.Instance.activeBattlefieldUnits.Remove(unitAI);
            }
        }

        if (unitAI != null)
        {
            unitAI.enabled = false;
            unitAI.ChangeState(EnemyState.Death);
        }

        // Disable physical body shapes immediately so players don't run in place against invisible walls
        var myCollider = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        var localBoxCollider = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
        if (localBoxCollider != null)
        {
            localBoxCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Put physics updates completely to sleep!
        }

        // Clean up any detached floating health bar objects instantly so allies don't attack ghost UI canvas spots
        if (healthBar != null && healthBar.gameObject != null)
        {
            Destroy(healthBar.gameObject);
        }

        if (KOCounter.instance != null && unitAI != null && unitAI.unitTeam == MusouUnit.Team.EnemySide)
        {
            if (unitAI.isStageCommander && BattleEndManager.Instance != null)
            {
                BattleEndManager.Instance.NotifyCommanderDefeated(this);
            }
        }

        // ========================================================================
        // 🟩 UNIVERSAL SYMMETRICAL DEATH PRESENTATION OVERRIDE:
        // ========================================================================
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        if (!isGate && anim != null)
        {
            anim.SetBool("isMoving", false);
            anim.SetBool("isHit", false);
            anim.SetBool("isBlocking", false);
            anim.SetBool("isDead", true);
        }

        StartCoroutine(DeathFadeRoutine());
    }

    private IEnumerator DeathFadeRoutine()
    {
        // 1. Establish absolute physical visibility flags natively
        bool visuallyVisibleOnScreen = (spriteRenderer != null && spriteRenderer.isVisible);
        bool isALeaderUnit = (unitAI != null && (unitAI.isOfficer || gameObject.name.Contains("Leader")));

        // ========================================================================
        // 🟩 THE MASTER LEADER BYPASS GATELINE (FIXED FROZEN ENEMY LEADERS):
        // If a master Squad Leader or Officer is defeated completely off-screen,
        // we strictly FORBID them from entering the frame-by-frame Lerp wait loop!
        // Because leader script loops can fight transparency variable adjustments,
        // we force an immediate hard engine deletion to keep your corridors completely clean!
        // ========================================================================
        bool shouldBypassFadeAndDestroyInstantly = !isSimulating && !visuallyVisibleOnScreen && isALeaderUnit;

        if (!shouldBypassFadeAndDestroyInstantly)
        {
            // Standard On-Screen / Simulating character wait pass
            if (isSimulating || visuallyVisibleOnScreen)
            {
                yield return new WaitForSeconds(4f);
            }

            if (spriteRenderer != null)
            {
                float fadeTime = 1f;
                float startAlpha = spriteRenderer.color.a;

                if (isSimulating || visuallyVisibleOnScreen)
                {
                    for (float t = 0; t < fadeTime; t += Time.deltaTime)
                    {
                        if (spriteRenderer == null) break;
                        Color c = spriteRenderer.color;
                        c.a = Mathf.Lerp(startAlpha, 0f, t / fadeTime);
                        spriteRenderer.color = c;
                        yield return null;
                    }
                }
            }
        }


        if (gameObject.CompareTag("Gate") || isGate)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ForceMinimapFlash()
    {
        if (minimapIconRenderer == null) return;

        // If the coroutine isn't already active, spin up a fresh pulse track!
        if (minimapFlashCoroutine == null)
        {
            minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
        }
    }

    private IEnumerator MinimapFlashTick()
    {
        if (minimapIconRenderer == null) yield break;

        Color teamColor = minimapIconRenderer.color;
        float timer = Random.Range(0f, 2f);
        float pulseDuration = 2f;

        while (unitAI != null && currentHealth > 0)
        {
            // ========================================================================
            // 🟩 THE PLATOON RADAR INTERLOCK (FIXED CONDITION):
            // We check if THIS grunt has a target, OR if their commanding squad leader
            // has acquired a combat target! If either is true, the icon keeps pulsing.
            // ========================================================================
            bool iHaveTarget = unitAI.currentTarget != null;
            bool leaderHasTarget = unitAI.myLeader != null && unitAI.myLeader.currentTarget != null;

            // If the whole squad has completely dropped out of combat, break the loop cleanly!
            if (!iHaveTarget && !leaderHasTarget)
            {
                break;
            }

            timer += Time.deltaTime;
            float t = (Mathf.Sin(timer * (Mathf.PI * 2) / pulseDuration) + 1f) / 2f;

            // ========================================================================
            // 🟩 THE RETRO LERP OVERLAY (FIXED MATH):
            // Swapped your addition math over to a clean Color.Lerp pass.
            // This cleanly interpolates between your baseline teamColor and a solid,
            // radiant bright yellow without creating muddy or greenish tints!
            // ========================================================================
            minimapIconRenderer.color = Color.Lerp(teamColor, Color.lightGoldenRodYellow, t);

            yield return null;
        }

        // Restore their clean team identity colors when the battle field clears out
        minimapIconRenderer.color = teamColor;
        minimapFlashCoroutine = null;
    }
    public void SetCulling(bool isVisible)
    {
        isSimulating = isVisible;
        spriteRenderer.enabled = isVisible;
        myCollider.enabled = isVisible;

        if (minimapIconRenderer != null)
        {
            minimapIconRenderer.enabled = true;
        }
    }


    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;

        hitFlashCoroutine = null;
    }

    void OnEnable()
    {
        if (unitAI != null && unitAI.currentTarget != null && minimapFlashCoroutine == null)
        {
            minimapFlashCoroutine = StartCoroutine(MinimapFlashTick());
        }
    }

    // 2. ADD this new helper Coroutine to the bottom of your Health.cs script:
    private IEnumerator DelayedKnockbackRoutine(Vector2 knockbackForce)
    {
        // Wait until the end of the current frame so the Animator has 
        // evaluated and locked into its new "Hit" sprite state.
        yield return new WaitForEndOfFrame();

        // If the hitlag manager froze our physics velocity to zero, 
        // we must wait until the hitlag freeze window finishes completely 
        // before executing our launch trajectory!
        while (anim != null && anim.speed == 0)
        {
            yield return null;
        }

        // Now unleash the explosive Basara knockback push!
        if (rb != null && currentHealth > 0)
        {
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        }
    }

    private void OnDestroy()
    {
        // 🔥 THE CRITICAL UN-LINK: If this unit vanishes from the scene, 
        // it must strip itself from the global lists instantly so other systems don't look for it!
        if (BattleManager.Instance != null && BattleManager.Instance.activeUnits.Contains(this))
        {
            BattleManager.Instance.activeUnits.Remove(this);
        }

        // Forcefully drop any tokens this physical unit was holding
        if (AttackDirector.instance != null)
        {
            AttackDirector.instance.ForceReleaseAllTokensForAttacker(GetComponent<MusouUnit>());
        }
    }

    // --- THE BATTLEFIELD LOOT GENERATOR ---
    private void EvaluateItemDrop()
    {
        // 1. Safety check: Exit instantly if you haven't assigned the attack or defense assets yet
        if (attackDropPrefab == null && defenseDropPrefab == null) return;

        MusouUnit unitAI = GetComponent<MusouUnit>();
        bool isOfficer = (unitAI != null && unitAI.isOfficer);

        float diceRoll = Random.Range(0f, 100f);
        float activeChance = isOfficer ? 100f : baseDropChance;

        if (diceRoll <= activeChance)
        {
            // 🔥 THE ISOLATED 50/50 WEIGH-SCALE MATRIX:
            // Random.value returns a float between 0.0 and 1.0. 
            // Splitting it perfectly at 0.5 guarantees a crisp, fair half-and-half chance!
            float coinFlip = Random.value;
            GameObject selectedItemToSpawn = null;

            if (coinFlip <= 0.5f)
            {
                selectedItemToSpawn = attackDropPrefab;  // 🗡️ Permanent Attack Sword
            }
            else
            {
                selectedItemToSpawn = defenseDropPrefab; // 🛡️ Permanent Defense Shield
            }

            // 2. Instantiate the item cleanly into your scene coordinates
            if (selectedItemToSpawn != null)
            {
                Vector3 spawnPos = transform.position;
                Vector2 randomPopOffset = Random.insideUnitCircle * 0.3f;
                Vector3 finalSpawnPos = spawnPos + new Vector3(randomPopOffset.x, randomPopOffset.y, 0f);

                // Spawns the physical sword or shield prop silently into the world map
                Instantiate(selectedItemToSpawn, finalSpawnPos, Quaternion.identity);
            }
        }
    }

}
