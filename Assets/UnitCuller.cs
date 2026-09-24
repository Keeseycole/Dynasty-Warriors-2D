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

    // ========================================================================
    // 🟩 STATIC PERFORMANCE MEMORY CACHE (LAG DESTRUCTION MATRIX):
    // Caching your sprite and animator components permanently right on frame one
    // completely stops high-cost tree exploration loops from choking your CPU!
    // ========================================================================
    private Animator cachedVisualAnim;
    private SpriteRenderer[] cachedRenderers;
    private bool referencesAreCached = false;

    [HideInInspector]
    public bool isDeflectingOffWall = false;
    [HideInInspector]
    public Vector2 wallDeflectionDirection = Vector2.zero;

    public LayerMask solidEnvironmentLayers;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        musouUnitComponent = GetComponent<MusouUnit>() ?? GetComponentInChildren<MusouUnit>();

        // Run your initial component caching loop immediately on boot!
        CacheLocalVisualReferences();
    }

    void Start()
    {
        FindPlayerWithTag();

        if (player == null) FindPlayerWithTag();

        if (player != null)
        {
            float sqrDist = (transform.position - player.position).sqrMagnitude;
            bool shouldBeActiveOnStart = sqrDist <= (cullDistance * cullDistance);

            bool isStrategicUnitOnStart = false;
            if (musouUnitComponent != null)
            {
                if (musouUnitComponent.isOfficer || musouUnitComponent.isStageCommander || musouUnitComponent.gameObject.name.Contains("Leader"))
                {
                    isStrategicUnitOnStart = true;
                }
            }

            health.isSimulating = shouldBeActiveOnStart;
            SetSimulationMode(shouldBeActiveOnStart, isStrategicUnitOnStart);
        }
    }

    /// <summary>
    /// Sweeps the child structures exactly once on frame one to securely store rendering references.
    /// </summary>
    void CacheLocalVisualReferences()
    {
        if (referencesAreCached) return;

        if (visualsChild != null)
        {
            cachedVisualAnim = visualsChild.GetComponent<Animator>() ?? visualsChild.GetComponentInChildren<Animator>();
            cachedRenderers = visualsChild.GetComponentsInChildren<SpriteRenderer>(true);
            referencesAreCached = true;
        }
    }

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
        // 1. SAFETY LIFE CHECK: If this unit is dead, stop processing
        if (health != null && health.currentHealth <= 0)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            return;
        }

        if (player == null)
        {
            FindPlayerWithTag();
            if (player == null) return;
        }

        float sqrDist = (transform.position - player.position).sqrMagnitude;
        float currentThreshold = health.isSimulating ? (cullDistance + wakeBuffer) : cullDistance;
        bool shouldBeActive = sqrDist <= (currentThreshold * currentThreshold);

        bool isStrategicUnit = false;
        if (musouUnitComponent != null)
        {
            if (musouUnitComponent.isOfficer || musouUnitComponent.isStageCommander || musouUnitComponent.gameObject.name.Contains("Leader"))
            {
                isStrategicUnit = true;
            }
        }

        if (shouldBeActive != health.isSimulating)
        {
            SetSimulationMode(shouldBeActive, isStrategicUnit);
        }

        // CONTINUOUS OFF-SCREEN SQUAD DRAG
        if (!health.isSimulating && !isStrategicUnit)
        {
            if (musouUnitComponent != null && musouUnitComponent.myLeader != null)
            {
                Transform leaderTransform = musouUnitComponent.myLeader.transform;
                Rigidbody2D leaderRb = musouUnitComponent.myLeader.GetComponent<Rigidbody2D>();

                float distToLeaderSqr = (leaderTransform.position - transform.position).sqrMagnitude;

                if (distToLeaderSqr > 4f * 4f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, leaderTransform.position, 4f * Time.deltaTime);
                }
                else if (leaderRb != null && leaderRb.linearVelocity.sqrMagnitude > 0.01f)
                {
                    transform.position += (Vector3)(leaderRb.linearVelocity * Time.deltaTime);
                }
            }
        }
    }

    void FixedUpdate()
    {
        // Safety Life Check: If dead, stop processing completely
        if (health != null && health.currentHealth <= 0) return;

        bool isStrategicUnit = false;
        if (musouUnitComponent != null)
        {
            if (musouUnitComponent.isOfficer || musouUnitComponent.isStageCommander || musouUnitComponent.gameObject.name.Contains("Leader"))
            {
                isStrategicUnit = true;
            }
        }

        // Run sensor arrays strictly for off-screen regular grunt forces
        if (health != null && !health.isSimulating && !isStrategicUnit)
        {
            if (musouUnitComponent != null)
            {
                Vector3 targetPos = transform.position;

                // 1. Pull target focus vectors based on active group priorities
                if (musouUnitComponent.currentTarget != null)
                {
                    targetPos = musouUnitComponent.currentTarget.position;
                }
                else if (musouUnitComponent.myLeader != null)
                {
                    Transform leaderTransform = musouUnitComponent.myLeader.transform;
                    float distToLeaderSqr = (leaderTransform.position - transform.position).sqrMagnitude;

                    if (distToLeaderSqr > 6f * 6f)
                    {
                        targetPos = leaderTransform.position;
                    }
                    else if (musouUnitComponent.myLeader.rb != null && musouUnitComponent.myLeader.rb.simulated && musouUnitComponent.myLeader.rb.linearVelocity.sqrMagnitude > 0.01f)
                    {
                        // Symmetrical translation matching the active commander's momentum
                        transform.position += (Vector3)(musouUnitComponent.myLeader.rb.linearVelocity * Time.fixedDeltaTime);
                        isDeflectingOffWall = false;
                        wallDeflectionDirection = Vector2.zero;
                        return; // Position synced! Exit tracking pass cleanly
                    }
                }

                // 2. Compute spatial raycast vectors
                Vector2 currentPos2D = transform.position;
                Vector2 targetPos2D = targetPos;
                Vector2 desiredHeadingDirection = (targetPos2D - currentPos2D).normalized;

                if (desiredHeadingDirection.sqrMagnitude > 0.01f)
                {
                    float checkDistance = 0.85f; // Look-ahead radar buffer bubble (Almost 1 full tile)
                    Vector2 raycastLookAheadEndPoint = currentPos2D + (desiredHeadingDirection * checkDistance);

                    // Fire the lightning-fast Linecast against your assigned Tilemap layer filters
                    RaycastHit2D wallObstacleHit = Physics2D.Linecast(currentPos2D, raycastLookAheadEndPoint, solidEnvironmentLayers);

                    // Debug visual sightlines inside your Scene view grid maps live
                    Debug.DrawLine(currentPos2D, raycastLookAheadEndPoint, wallObstacleHit.collider != null ? Color.red : Color.green);

                    if (wallObstacleHit.collider != null)
                    {
                        Vector2 wallSurfaceNormal = wallObstacleHit.normal;
                        Vector2 slidingDeflectionVector = new Vector2(-wallSurfaceNormal.y, wallSurfaceNormal.x);

                        // Ensure our sliding deflection matches our original direction forward vector
                        if (Vector2.Dot(slidingDeflectionVector, desiredHeadingDirection) < 0f)
                        {
                            slidingDeflectionVector = -slidingDeflectionVector;
                        }

                        // 🟩 CONSOLIDATED PASSTHROUGH: Save data variables safely into your memory tickers!
                        isDeflectingOffWall = true;
                        wallDeflectionDirection = slidingDeflectionVector.normalized;
                    }
                    else
                    {
                        isDeflectingOffWall = false;
                        wallDeflectionDirection = Vector2.zero;
                    }
                }
                else
                {
                    isDeflectingOffWall = false;
                    wallDeflectionDirection = Vector2.zero;
                }
            }
        }
    }
    void SetSimulationMode(bool isActive, bool bypassAIAndPhysics)
    {
        health.isSimulating = isActive;

        // Force a safety backup collection if an initialization race condition happened
        if (!referencesAreCached) CacheLocalVisualReferences();

        // ========================================================================
        // 🟩 CLEAN CACHED GRAPHICS FILTER PASS (NO GETCOMPONENT CHEWS!):
        // Completely stripped away all runtime child searching loops! 
        // We read straight from your pre-saved 'cachedRenderers' array on the heap,
        // instantly dropping your on-screen crowding lag to absolute zero!
        // ========================================================================
        if (cachedVisualAnim != null) cachedVisualAnim.enabled = isActive;

        if (cachedRenderers != null)
        {
            bool isStrategicUnit = bypassAIAndPhysics || gameObject.name.Contains("Leader");

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer currentSR = cachedRenderers[i];
                if (currentSR == null) continue;

                string objectName = currentSR.gameObject.name;

                if (objectName.Contains("Minimap") || objectName.Contains("Icon") || objectName.Contains("Radar"))
                {
                    currentSR.gameObject.SetActive(true);
                    currentSR.enabled = true; // Radar tracking dots remain perfectly untouched and live
                }
                else
                {
                    if (isActive) currentSR.enabled = true;
                    else currentSR.enabled = isStrategicUnit; // Hide generic grunt body meshes far away
                }
            }
        }

        // PROCESSOR LOCOMOTION ROUTER
        if (isActive || bypassAIAndPhysics)
        {
            if (musouUnitComponent != null) musouUnitComponent.enabled = true;

            if (rb != null)
            {
                rb.simulated = true;
                if (!isActive) rb.angularVelocity = 0f;
            }
        }
        else
        {
            if (musouUnitComponent != null) musouUnitComponent.enabled = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false; // Put physics solver fully to sleep out of sight
            }
        }
    }
}