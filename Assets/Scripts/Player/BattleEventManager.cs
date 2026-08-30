using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Ensure this namespace is present

public class BattleEventManager : MonoBehaviour
{
    public static BattleEventManager Instance;

    // --- GLOBAL OBJECTIVE EVENT BROADCASTER ---
    // Sends out the exact index position of the item that was destroyed so external triggers can listen!
    public static event Action<int> OnObjectiveDestroyed;

    [Header("Mission Parameters")]
    private int totalObjectsNeeded = 0;
    private int objectsDestroyedCount = 0;
    private bool eventTriggered = false;

    [Header("Dynamic Objective Tracking")]
    [Tooltip("Drag ANY number of GameObjects that must be destroyed to complete this mission (Rams, towers, gates, etc.)")]
    public List<GameObject> targetObjectsToDestroy;

    [Header("Cinematic Strategic Dialogues")]
    [Tooltip("The sequential conversation to send to the MusouDialogManager when the fire attack trap springs.")]
    public DialogConversation fireAttackConversation;

    [Header("Retreat Strategy Parameters")]
    [Tooltip("Drag the Enemy General / SquadLeader who should flee after the fire attack strategy executes.")]
    public SquadLeader enemyGeneralToRetreat;

    [Tooltip("Drag sequential nodes the general must follow to safely escape the map terrain grids.")]
    public List<Transform> retreatPathNodes;

    [Tooltip("The final gate or exit point transform where the fleeing general completely despawns.")]
    public Transform finalEscapePoint;

    private HashSet<int> activatedIndexes = new HashSet<int>();

    private MusouUnit musouunit;

    private void Awake()
    {
        musouunit = GetComponent<MusouUnit>();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (targetObjectsToDestroy != null)
        {
            totalObjectsNeeded = targetObjectsToDestroy.Count;
        }
    }

    private void Update()
    {
        if (targetObjectsToDestroy == null || eventTriggered) return;

        for (int i = 0; i < targetObjectsToDestroy.Count; i++)
        {
            if (activatedIndexes.Contains(i)) continue;

            if (targetObjectsToDestroy[i] == null || !targetObjectsToDestroy[i].activeInHierarchy)
            {
                activatedIndexes.Add(i);
                objectsDestroyedCount++;

                // 🔥 BROADCAST: Alert all listening standalone UnitReinforcementTriggers across the map layout!
                OnObjectiveDestroyed?.Invoke(i);

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

        // 2. CINEMATIC CUTSCENE BANNER DIALOGUE HANDOFF
        // ========================================================================
        // 🔥 DIRECT TEXT PLACEMENT: Directs the conversation text straight onto the player UI view layer!
        // ========================================================================
        if (fireAttackConversation != null && MusouDialogManager.Instance != null)
        {
            MusouDialogManager.Instance.PlayConversation(fireAttackConversation);
        }
        else
        {
            Debug.LogWarning("★ CRITICAL STRATAGEM ACTIVATE: FIRE ATTACK SUCCESS ★");
            Debug.LogWarning("Commander: 'The trap is sprung! Burn them to ashes!'");
        }

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(1.5f, 0.4f);

        // Wait in absolute RealTime while the screen is frozen so the player can witness the vibration
        yield return new WaitForSecondsRealtime(2.0f);

        // 3. UNFREEZE PHYSICS: Wake the game clock back up to calculate forces
        Time.timeScale = 1f;

        // 4. SPAWN EXPLOSION ARTWORK: Trigger your custom 2D sprite hit animation prefab
        if (HitParticleManager.Instance != null)
        {
            HitParticleManager.Instance.SpawnHitSpark(bombCenter, true, Vector2.up);
        }

        // 5. THE TRIGGER BOARD WIPE: Wipe out exactly who was caught in your custom Trigger zone boundaries
        Debug.Log($"[STRATAGEM] Detonating! Eliminating enemies caught inside the trigger volume zone.");

        foreach (Health victim in victimsToScorched)
        {
            if (victim != null && victim.currentHealth > 0)
            {

                MusouUnit unitData = victim.GetComponent<MusouUnit>() ?? victim.GetComponentInChildren<MusouUnit>();
                if (unitData != null)
                {
                    if (unitData.isOfficer || unitData.isStageCommander)
                    {
                        Debug.Log($"[FIRE TRAP SHIELD]: Safeguarding officer '{victim.name}' from instant cinematic death!");

                        // OPTIONAL: If you want officers to take a tiny bit of non-lethal chip damage, uncomment below:
                        // Vector2 minorDirection = ((Vector2)victim.transform.position - (Vector2)bombCenter).normalized;
                        // victim.TakeDamage(15f, bombCenter, minorDirection * 5f, (Animator)null, (Rigidbody2D)null);

                        continue; 
                    }
                }

                // Deliver terminal damage values to vaporize standard fodder grunts immediately
                Vector2 blastDirection = ((Vector2)victim.transform.position - (Vector2)bombCenter).normalized;
                float lethalBlastForce = 25f;
                victim.TakeDamage(9999f, bombCenter, blastDirection * lethalBlastForce, (Animator)null, (Rigidbody2D)null);
            }
        }

        if (enemyGeneralToRetreat != null && retreatPathNodes != null && retreatPathNodes.Count > 0)
        {
            StartCoroutine(ExecuteGeneralRetreatPath(enemyGeneralToRetreat));
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
                // Constantly push the general along the escape route every frame to ignore local grunt skirmishes
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
                // ========================================================================
                // 🔥 FIXED MUTATION CRASH: Loops backward to safely purge squad members!
                // ========================================================================
                if (general.squadMembers != null)
                {
                    for (int m = general.squadMembers.Count - 1; m >= 0; m--)
                    {
                        if (general.squadMembers[m] != null)
                        {
                            GameObject gruntObject = general.squadMembers[m].gameObject;
                            general.squadMembers.RemoveAt(m);
                            if (gruntObject != null)
                            {
                                Destroy(gruntObject);
                            }
                        }
                    }
                }
                Destroy(general.gameObject);
            }
        }
    }
}