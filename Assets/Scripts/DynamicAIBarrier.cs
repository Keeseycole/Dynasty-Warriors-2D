using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DynamicAIBarrier : MonoBehaviour
{
    private Collider2D barrierCollider;

    [Header("Sensor Bounds")]
    [Tooltip("How close an enemy needs to be before they physically collide with this wall (to prevent clipping during combat).")]
    public float combatDistance = 6f;

    [Header("Dynamic Masking")]
    [Tooltip("Make sure this matches the physics layer assigned to your enemy grunts.")]
    public LayerMask enemyLayer;

    // A reusable array buffer to track close enemies without causing memory lag allocation spikes
    private Collider2D[] closeEnemyBuffer = new Collider2D[5];

    void Awake()
    {
        barrierCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // ========================================================================
        // 🟩 MODERNIZED OVERLAP FILTER ENGINE (FIXED):
        // Swaps the deprecated non-alloc function with the clean, production-ready
        // Physics2D.OverlapCircle syntax structure, utilizing a ContactFilter2D buffer array!
        // ========================================================================
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;

        // Uses the modern method to populate our close enemy array buffer without lag allocation spikes
        int enemyCount = Physics2D.OverlapCircle(transform.position, combatDistance, filter, closeEnemyBuffer);

        // 2. THE DYNAMIC SWITCH:
        if (enemyCount > 0)
        {
            // Enemies are close and fighting! Make the wall completely SOLID for them 
            // so they bounce off it and never clip inside the geometry.
            barrierCollider.excludeLayers = 0;
        }
        else
        {
            // No enemies are near! Tell this specific collider to explicitly EXCLUDE the Enemy layer.
            // The distant AI grunts will now move right through it like thin air!
            barrierCollider.excludeLayers = enemyLayer;
        }
    }

    // Draw a helpful visual ring in the editor scene window to help you tune your combat distance
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, combatDistance);
    }
}