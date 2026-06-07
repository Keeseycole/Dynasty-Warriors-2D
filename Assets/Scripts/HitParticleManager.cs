using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitParticleManager : MonoBehaviour
{
    public static HitParticleManager Instance { get; private set; }

    [Header("Sprite Effect Prefabs")]
    [Tooltip("Your custom 2D sprite animation prefab for regular hits")]
    public GameObject basicHitSparkPrefab;

    [Tooltip("Your custom 2D sprite animation prefab for Combo 5 finisher")]
    public GameObject finisherHitSparkPrefab;

    [Header("Basara Native Particle Settings")]
    [Tooltip("Drag a GameObject here that has a ParticleSystem component configured for shard sprays")]
    public ParticleSystem shardParticleSystem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Spawns custom sprite animations and triggers native physics shard bursts along the attack vector.
    /// </summary>
    public void SpawnHitSpark(Vector2 position, bool isFinisher, Vector2 attackDir)
    {
        GameObject prefabToSpawn = isFinisher ? finisherHitSparkPrefab : basicHitSparkPrefab;
        if (prefabToSpawn == null) return;

        // Orient the slash sprite to match the mathematical direction of your sword swing
        float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
        Quaternion slashOrientation = Quaternion.Euler(0, 0, angle + Random.Range(-15f, 15f));

        // Spawn the core slash animation into the world
        GameObject spark = Instantiate(prefabToSpawn, position, slashOrientation);

        // Randomly stretch and flip the effect to create visual variety
        Vector3 dynamicScale = spark.transform.localScale;
        dynamicScale.x *= Random.Range(0.8f, 1.4f);
        dynamicScale.y *= Random.Range(0.9f, 1.2f);
        if (Random.value > 0.5f) dynamicScale.y *= -1;
        spark.transform.localScale = dynamicScale;

        // =========================================================================
        // 🔥 TRIGGER NATIVE BASARA SHARD BURST
        // =========================================================================
        if (shardParticleSystem != null)
        {
            // 1. Teleport the particle emitter system to the exact point of enemy contact
            shardParticleSystem.transform.position = position;

            // 2. Rotate the emitter shape so the cone spray faces where the blade is cutting
            shardParticleSystem.transform.rotation = Quaternion.Euler(0, 0, angle);

            // 3. Emit an explosive cluster of particles instantly
            int burstCount = isFinisher ? Random.Range(15, 25) : Random.Range(5, 10);
            shardParticleSystem.Emit(burstCount);
        }
    }
}