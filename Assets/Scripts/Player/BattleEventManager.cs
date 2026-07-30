using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleEventManager : MonoBehaviour
{
    public static BattleEventManager Instance;

    [Header("Mission Parameters")]
    private int totalObjectsNeeded = 0;      // Automatically calculated at Start
    private int objectsDestroyedCount = 0;   // Current count tracker
    private bool eventTriggered = false;

    [Header("Ambush Reinforcements")]
    public Transform finalObjectiveTarget;

    [Tooltip("Drag your sequential path nodes here in the exact order they should march")]
    public List<Transform> pathNodes;

    [Header("Dynamic Objective Tracking")]
    [Tooltip("Drag ANY number of GameObjects that must be destroyed to complete this mission (Rams, towers, gates, etc.)")]
    public List<GameObject> targetObjectsToDestroy;

    [Tooltip("Drag your inactive Ambush Squad GameObjects here (Match the order of your target objects list)")]
    public List<GameObject> ambushSquadContainers;

    // Track which indexes we have already activated so we don't loop-spam them
    private HashSet<int> activatedIndexes = new HashSet<int>();

    [Header("Retreat Strategy")]
    [Tooltip("Drag the Enemy General / SquadLeader who should flee after the fire attack")]
    public SquadLeader enemyGeneralToRetreat;

    [Tooltip("Drag sequential nodes the general must follow to safely escape the map")]
    public List<Transform> retreatPathNodes;

    [Tooltip("The final gate where the general completely despawns and escapes")]
    public Transform finalEscapePoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // ORGANIC COUNTING: Automatically matches mission scale to your Inspector list size!
        if (targetObjectsToDestroy != null)
        {
            totalObjectsNeeded = targetObjectsToDestroy.Count;
            Debug.Log($"[MISSION START] Target Objective Initialized. Total items to destroy: {totalObjectsNeeded}");
        }
    }
    private void Update()
    {
        if (targetObjectsToDestroy == null || ambushSquadContainers == null || eventTriggered) return;

        for (int i = 0; i < targetObjectsToDestroy.Count; i++)
        {
            if (activatedIndexes.Contains(i)) continue;

            if (targetObjectsToDestroy[i] == null || !targetObjectsToDestroy[i].activeInHierarchy)
            {
                activatedIndexes.Add(i);
                objectsDestroyedCount++;

                Debug.Log($"[OBJECTIVE] Item at list position {i} eliminated! Progress: {objectsDestroyedCount}/{totalObjectsNeeded}");

                if (i < ambushSquadContainers.Count && ambushSquadContainers[i] != null)
                {
                    StartCoroutine(DeployAmbushSequence(ambushSquadContainers[i]));
                }

                if (objectsDestroyedCount >= totalObjectsNeeded)
                {
                    eventTriggered = true;
                   // CompleteSiegeMission();
                }
            }
        }
    }

    public void ExecuteFireAttackCutscene(Vector3 bombCenter, List<Health> victimsToScorched)
    {
        StartCoroutine(FireAttackCinematicRoutine(bombCenter, victimsToScorched));
    }

    private IEnumerator FireAttackCinematicRoutine(Vector3 bombCenter, List<Health> victimsToScorched)
    {
        // 1. TIME FREEZE: Stop the rest of the battlefield dead in their tracks
        Time.timeScale = 0f;

        // 2. CINEMATIC CUTSCENE BANNER (Text Display Log)
     
        Debug.LogWarning("★ CRITICAL STRATAGEM ACTIVATE: FIRE ATTACK SUCCESS ★");
        Debug.LogWarning("Commander: 'The trap is sprung! Burn them to ashes!'");
   

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(1.5f, 0.4f);

        // Wait in absolute RealTime while the screen is frozen so the player can witness the vibration
        yield return new WaitForSecondsRealtime(2.0f);

        // 3. UNFREEZE PHYSICS: Wake the game clock back up to calculate forces
        Time.timeScale = 1f;

        // 4. SPAWN EXPLOSION ARTWORK: Trigger your custom 2D sprite hit animation prefab
        if (HitParticleManager.Instance != null)
        {
            // FIXED: Added Vector2.up as a neutral parameter to support the Basara direction checker
            HitParticleManager.Instance.SpawnHitSpark(bombCenter, true, Vector2.up);
        }

        // 5. THE TRIGGER BOARD WIPE: Wipe out exactly who was caught in your custom Trigger zone boundaries
        Debug.Log($"[STRATAGEM] Detonating! Eliminating {victimsToScorched.Count} enemies caught inside the trigger volume zone.");

        foreach (Health victim in victimsToScorched)
        {
            if (victim != null && victim.currentHealth > 0)
            {
                // Calculate an explosive blow away vector outward from the epicenter
                Vector2 blastDirection = ((Vector2)victim.transform.position - (Vector2)bombCenter).normalized;
                float lethalBlastForce = 25f;
                // Pass total lethal damage (9999) to activate their health death/fade coroutines
        
                victim.TakeDamage(9999f, bombCenter, blastDirection * lethalBlastForce, (Animator)null, (Rigidbody2D)null); ;
            }
        }

        if (enemyGeneralToRetreat != null && retreatPathNodes.Count > 0)
        {
            StartCoroutine(ExecuteGeneralRetreatPath(enemyGeneralToRetreat));
        }
    }

    private IEnumerator DeployAmbushSequence(GameObject squadRoot)
    {
        // 1. Wait out your core mechanical delay window
        yield return new WaitForSeconds(2.0f);

        // 2. Wake up the physical folder container inside your castle room layout
        squadRoot.SetActive(true);
        Debug.Log($"[EVENT] Activated hidden squad container: {squadRoot.name}");

        // 3. Gather all the child soldiers nested inside it
        MusouUnit[] soldiersInSquad = squadRoot.GetComponentsInChildren<MusouUnit>(true);

        foreach (MusouUnit soldier in soldiersInSquad)
        {
            if (soldier != null)
            {
                soldier.unitTeam = MusouUnit.Team.PlayerSide;
                soldier.isOfficer = false;

                // ========================================================================
                // 🔥 FIXED: THE REINFORCEMENT SEPARATION BUFFER
                // We pass the soldier to a routing method that applies a clean 0.1-second 
                // physics delay. This ensures they register their true room positions 
                // before calculating paths, preventing clipping or warping!
                // ========================================================================
                StartCoroutine(RouteUnitSafely(soldier));
            }
        }
    }

    // Add this quick helper routing coroutine right below your sequence block:
    private IEnumerator RouteUnitSafely(MusouUnit unit)
    {
        // Force the unit to stay put for a tiny split second to register its environment
        yield return new WaitForSeconds(0.1f);

        if (unit != null && unit.enabled)
        {
            // Now it's safe to start dragging them down your path node lines!
            StartCoroutine(DeployUnitOnPath(unit));
        }
    }
    private IEnumerator DeployUnitOnPath(MusouUnit unit)
    {
        // ========================================================================
        // 🔥 FIXED: THE POSITION INITIALIZATION BUFFER
        // Yields control back to Unity for one frame so the physics engine and Rigidbody
        // can fully wake up, register, and lock into their true spawn positions!
        // ========================================================================
        yield return null;

        // Safety check in case the unit was destroyed during the initialization frame buffer
        if (unit == null) yield break;

        if (unit != null)
        {
            unit.ChangeState(EnemyState.Walk);
        }

        foreach (Transform node in pathNodes)
        {
            if (unit == null || !unit.enabled) yield break;

            unit.missionTarget = node;

            float checkTimer = 0f;
            while (unit != null && node != null)
            {
                if (unit.currentTarget == null)
                {
                    unit.MoveTowards(node.position, false);
                }

                checkTimer += Time.deltaTime;
                if (checkTimer >= 0.2f)
                {
                    checkTimer = 0f;
                    if (Vector2.Distance(unit.transform.position, node.position) <= 1.5f)
                    {
                        break;
                    }
                }

                yield return null;
            }
        }

        if (unit != null)
        {
            unit.missionTarget = finalObjectiveTarget;
            Debug.Log($"[SQUAD] {unit.gameObject.name} cleared the layout nodes and is charging the final objective!");
        }
    }

    private IEnumerator ExecuteGeneralRetreatPath(SquadLeader general)
    {
        if (general == null) yield break;

        

        foreach (Transform node in retreatPathNodes)
        {
            if (general == null) yield break;

            general.AssignSquadMission(MusouUnit.AIMission.CaptureBase, node);

            float checkTimer = 0f;
            while (general != null && node != null)
            {
                // 🔥 THE FIX: Constantly push the general along the escape route every frame
                // Generals fleeing completely clear their target to ignore local grunt skirmishes
                general.currentTarget = null;
                general.MoveTowards(node.position, false);

                checkTimer += Time.deltaTime;
                if (checkTimer >= 0.2f)
                {
                    checkTimer = 0f;
                    if (Vector2.Distance(general.transform.position, node.position) <= 1.8f)
                    {
                        break;
                    }
                }

                yield return null;
            }
        }

        if (general != null && finalEscapePoint != null)
        {
            general.AssignSquadMission(MusouUnit.AIMission.CaptureBase, finalEscapePoint);

            float checkTimer = 0f;
            while (general != null)
            {
                // Continue pushing towards the final map boundary despawn threshold
                general.currentTarget = null;
                general.MoveTowards(finalEscapePoint.position, false);

                checkTimer += Time.deltaTime;
                if (checkTimer >= 0.2f)
                {
                    checkTimer = 0f;
                    if (Vector2.Distance(general.transform.position, finalEscapePoint.position) <= 1.5f)
                    {
                        break;
                    }
                }

                yield return null;
            }

            if (general != null)
            {
                // Clean up all attached living squad grunts explicitly
                foreach (MusouUnit member in general.squadMembers)
                {
                    if (member != null) Destroy(member.gameObject); // Removes the grunt, NOT the folder!
                }

                Debug.LogWarning($"[RETREAT] {general.gameObject.name} successfully escaped!");
                Destroy(general.gameObject); // Removes the general, NOT the folder!
            }

        }
    }
}