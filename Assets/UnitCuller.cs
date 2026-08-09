using UnityEngine;

public class UnitCuller : MonoBehaviour
{
    private Health health;
    private Rigidbody2D rb;
    private MusouUnit musouUnitComponent;

    [Header("Hierarchy Targets")]
    [Tooltip("The sub-child container holding the SpriteRenderers and Animator components.")]
    public GameObject visualsChild;

    [Header("Culling Bounds Matrix")]
    public float cullDistance = 20f;
    [Tooltip("Extra padding distance when waking units up to prevent rapid on/off asset flickering.")]
    public float wakeBuffer = 2f;

    private Transform player;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        musouUnitComponent = GetComponent<MusouUnit>() ?? GetComponentInChildren<MusouUnit>();
    }

    void Start()
    {
        FindPlayerWithTag();
    }

    /// <summary>
    /// Uses standard Unity tags to find the player character transform.
    /// </summary>
    void FindPlayerWithTag()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        // 1. SAFETY LIFE CHECK: If this unit is dead, it must NEVER process culling cycles!
        if (health != null && health.currentHealth <= 0)
        {
            if (visualsChild != null && !visualsChild.activeSelf)
            {
                visualsChild.SetActive(true);
            }
            return;
        }

        // 2. RETRY LOOKUP: If player is lost, find them again using the tag
        if (player == null)
        {
            FindPlayerWithTag();
            if (player == null) return;
        }

        float sqrDist = (transform.position - player.position).sqrMagnitude;

        float currentThreshold = health.isSimulating ? (cullDistance + wakeBuffer) : cullDistance;
        bool shouldBeActive = sqrDist <= (currentThreshold * currentThreshold);

        // ========================================================================
        // 🔥 THE STRATEGIC OVERRIDE LOCK:
        // Officers, Stage Commanders, and Squad Leaders must ALWAYS process their
        // AI positioning logic so they can march toward objective bases completely
        // independently without freezing up when the player walks away!
        // ========================================================================
        bool isStrategicUnit = false;
        if (musouUnitComponent != null)
        {
            if (musouUnitComponent.isOfficer || musouUnitComponent.isStageCommander || musouUnitComponent is SquadLeader)
            {
                isStrategicUnit = true;
            }
        }

        if (shouldBeActive != health.isSimulating)
        {
            SetSimulationMode(shouldBeActive, isStrategicUnit);
        }
    }

    void SetSimulationMode(bool isActive, bool bypassAIAndPhysics)
    {
        health.isSimulating = isActive;

        // 🟩 VISUAL CULLING (Always happens for everyone based on distance):
        // Turns off expensive animators and sprite renderers to protect framerate!
        if (visualsChild != null)
            visualsChild.SetActive(isActive);

        // 🟩 PROCESSOR CULLING (Bypassed entirely for important leaders):
        if (bypassAIAndPhysics)
        {
            // For Squad Leaders, force their AI thoughts and physics engines to stay 
            // 100% active so they can continue to calculate marching vectors!
            if (musouUnitComponent != null) musouUnitComponent.enabled = true;
            if (rb != null) rb.simulated = true;
        }
        else
        {
            // For standard grunt soldiers, safely put their brains and physics to sleep
            if (musouUnitComponent != null) musouUnitComponent.enabled = isActive;

            if (rb != null)
            {
                if (isActive)
                {
                    rb.simulated = true;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                }
            }
        }
    }
}