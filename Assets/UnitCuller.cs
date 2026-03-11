using UnityEngine;

public class UnitCuller : MonoBehaviour
{
    private Health health;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer[] renderers;

    public GameObject visualsChild;
    public float cullDistance = 20f;
    public float wakeBuffer = 2f; // Extra distance to prevent flickering

    private Transform player;

    void Awake()
    {
        health = GetComponent<Health>();
        anim = GetComponentInChildren<Animator>(); // Look in children if needed
        rb = GetComponent<Rigidbody2D>();
        renderers = visualsChild.GetComponentsInChildren<SpriteRenderer>();
    }

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }



    void Update()
    {
        if (player == null) { FindPlayer(); return; }

        float sqrDist = (transform.position - player.position).sqrMagnitude;

        // Use a larger distance to "wake up" and a smaller one to "cull"
        float currentThreshold = health.isSimulating ? (cullDistance + wakeBuffer) : cullDistance;
        bool shouldBeActive = sqrDist <= (currentThreshold * currentThreshold);

        if (shouldBeActive != health.isSimulating)
        {
            SetSimulationMode(shouldBeActive);
        }
    }

    void SetSimulationMode(bool isActive)
    {
        health.isSimulating = isActive;

        // This one line handles the Animator and all SpriteRenderers 
        // sitting inside that child object.
        if (visualsChild != null)
            visualsChild.SetActive(isActive);

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

        // Note: If you SetActive(false) the visualsChild, 
        // you don't need 'if (anim != null) anim.enabled = false' 
        // because the object is gone!
    }
}