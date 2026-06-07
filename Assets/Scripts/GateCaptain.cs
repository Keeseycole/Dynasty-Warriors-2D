using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GateCaptain : SquadLeader
{
    [Header("Classic DW Spawning Settings")]
    [Tooltip("The grunt prefab to spawn when the gate needs reinforcements")]
    public GameObject soldierPrefab;

    [Tooltip("An empty GameObject positioned directly at the gate checkpoint line")]
    public Transform spawnPoint;

    [Tooltip("The maximum amount of guards this gate keeps active at once")]
    public int maxSquadSize = 5;

    [Tooltip("How many seconds between reinforcement waves")]
    public float respawnCooldown = 10f;
    private float nextRespawnTime;

    public override void Start()
    {
        // Run standard MusouUnit/SquadLeader initialization
        base.Start();

        // Spawn the first wave immediately on startup
        if (squadMembers.Count == 0)
        {
            SpawnGuards();
        }
    }

    private void Update()
    {
        // 1. PURGE DEAD GUARDS: Keep the squad count accurate by removing missing instances
        squadMembers.RemoveAll(item => item == null);

        // 2. RESPOND CLOCK: Trigger fresh spawns if the group is shrinking
        if (squadMembers.Count < maxSquadSize && Time.time >= nextRespawnTime)
        {
            SpawnGuards();
        }
    }

    // --- LOCAL METHODS ---
    public void SpawnGuards() { ExecuteSpawningLogic(); }
    public void SpawnGaurds() { ExecuteSpawningLogic(); }

    private void ExecuteSpawningLogic()
    {
        nextRespawnTime = Time.time + respawnCooldown;
        int amountToSpawn = maxSquadSize - squadMembers.Count;

        Debug.Log($"[GATE CHECKPOINT] {gameObject.name} is spawning {amountToSpawn} new soldiers.");

        GameObject newSoldier = Instantiate(soldierPrefab, spawnPoint.position, Quaternion.identity);

        MusouUnit unitScript = newSoldier.GetComponent<MusouUnit>();

        // FIXED AND RESTORED: All spawning lines are securely wrapped back inside the loop
        for (int i = 0; i < amountToSpawn; i++)

            if (unitScript != null)
            {
                // Typo fallback definition so both capitalizations point to the exact same reference
                MusouUnit UnitScript = unitScript;

                // Hook them directly into this captain's squad management loop
                unitScript.myLeader = this;
                unitScript.unitTeam = this.unitTeam;
                unitScript.squadIndex = squadMembers.Count;

                squadMembers.Add(unitScript);

                // Send them to their respective visual formation slot coordinates
                unitScript.transform.position = GetSlotPosition(unitScript.squadIndex);
            }
    }
}
