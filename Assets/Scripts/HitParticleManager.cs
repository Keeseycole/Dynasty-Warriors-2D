using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitParticleManager : MonoBehaviour
{
    public static HitParticleManager Instance { get; private set; }

    public enum AttackType { Basic, Finisher, Block }

    [Header("Sprite Effect Prefabs")]
    public GameObject basicHitSparkPrefab;
    public GameObject finisherHitSparkPrefab;
    public GameObject blockHitSparkPrefab;

    [Header("Native Particle Systems")]
    [Tooltip("Flesh and slice slash shard sprays")]
    public ParticleSystem shardParticleSystem;

    [Header("Block Particle Settings")]
    [Tooltip("🔥 DRAG YOUR NEW BLOCK SPARK EMITTER PREFAB HERE!")]
    public ParticleSystem blockParticleSystem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // =========================================================================
        // 🔥 THE PREFAB INSTANTIATION SAFE-GUARD:
        // If you dragged a project asset prefab into the inspector fields, Unity 
        // cannot emit from them directly. We must spawn live scene instances right here!
        // =========================================================================

        // 1. Process the standard flesh slash shard sprays
        if (shardParticleSystem != null && shardParticleSystem.gameObject.scene.name == null)
        {
            // scene.name == null proves it is a raw asset prefab, not a scene object!
            ParticleSystem liveSystem = Instantiate(shardParticleSystem, transform.position, Quaternion.identity, transform);
            shardParticleSystem = liveSystem;
        }

        // 2. Process your new iron weapon blocking sparks
        if (blockParticleSystem != null && blockParticleSystem.gameObject.scene.name == null)
        {
            // Spawns a clone nested cleanly under the HitParticleManager container hierarchy
            ParticleSystem liveBlockSystem = Instantiate(blockParticleSystem, transform.position, Quaternion.identity, transform);
            blockParticleSystem = liveBlockSystem;
        }

    }

    // --- INSIDE HITPARTICLEMANAGER.CS ---

    public void SpawnHitSpark(Vector2 position, bool isFinisher, Vector2 attackDir)
    {
        // 🔥 THE EXCLUSION CURE: Cast an overlap circle right at the impact coordinate 
        // to check if the target character has blocked the weapon strike!
        Collider2D hitTarget = Physics2D.OverlapCircle(position, 0.5f);
        if (hitTarget != null)
        {
            Health targetHealth = hitTarget.GetComponentInParent<Health>();

            // If the unit successfully guarded, immediately drop out!
            // This stops your basic blood slash prefab from double-spawning on top of shields.
            if (targetHealth != null && targetHealth.blockedOnThisFrame)
            {
                return;
            }
        }

        // If no shield block was registered on this frame, proceed to spawn standard blood slashes cleanly
        AttackType type = isFinisher ? AttackType.Finisher : AttackType.Basic;
        SpawnHitSparkUniversal(position, type, attackDir);
    }

    public void SpawnHitSparkUniversal(Vector2 position, AttackType attackType, Vector2 attackDir)
    {
        // 1. Determine standard sprite prefabs
        GameObject prefabToSpawn = basicHitSparkPrefab;
        switch (attackType)
        {
            case AttackType.Finisher: prefabToSpawn = finisherHitSparkPrefab; break;
            case AttackType.Block: prefabToSpawn = blockHitSparkPrefab; break;
        }

        if (prefabToSpawn != null)
        {
            float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
            Quaternion slashOrientation = Quaternion.Euler(0, 0, angle + Random.Range(-15f, 15f));

            GameObject spark = Instantiate(prefabToSpawn, position, slashOrientation);

            Vector3 dynamicScale = spark.transform.localScale;
            dynamicScale.x *= Random.Range(0.8f, 1.4f);
            dynamicScale.y *= Random.Range(0.9f, 1.2f);
            if (Random.value > 0.5f) dynamicScale.y *= -1;
            spark.transform.localScale = dynamicScale;
        }

        // =========================================================================
        // 🔥 TRIGGER SPECIALIZED BLOCK PARTICLE SYSTEM
        // =========================================================================
        if (attackType == AttackType.Block && blockParticleSystem != null)
        {
            // Calculate the angle of attack vector
            float attackAngle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;

            // Teleport the metal spark emitter right to the point of shield contact
            blockParticleSystem.transform.position = position;

            // Rotate the cone shape 180 degrees back toward the player 
            // so the metal shards explosively ricochet off the iron shield!
            blockParticleSystem.transform.rotation = Quaternion.Euler(0, 0, attackAngle + 180f);

            // Trigger an instantaneous hardware-accelerated particle spray
            int blockBurst = Random.Range(12, 18);
            blockParticleSystem.Emit(blockBurst);
        }
        // Standard flesh shards loop if they were caught off-guard
        else if (attackType != AttackType.Block && shardParticleSystem != null)
        {
            float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
            shardParticleSystem.transform.position = position;
            shardParticleSystem.transform.rotation = Quaternion.Euler(0, 0, angle);

            int burstCount = (attackType == AttackType.Finisher) ? Random.Range(15, 25) : Random.Range(5, 10);
            shardParticleSystem.Emit(burstCount);
        }
    }
}