using UnityEngine;
using System.Collections;

public enum PlayerState
{
    walk,
    attack,
    idle,
    stagger
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public PlayerState currentState = PlayerState.idle;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private bool snapToDirAnim = true;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput = Vector2.zero;
    public Vector2 lastLookDir = Vector2.down; // Default facing direction

    public float pickupRange = 1.5f;

    [Header("Musou Strike Matrix")]
    public float playerAttackRange = 1.8f;
    public float playerAttackDamage = 10f;
    public float playerKnockbackForce = 6f;
    [SerializeField] private LayerMask enemyFactionLayers;

    // 🔥 THE MULTI-TARGET ANIMATION EVENT RECEIVER:
    // This safely overrides the AI single-target restriction for the playable hero!
    // 🔥 THE MULTI-TARGET ANIMATION EVENT RECEIVER:
    public void ApplyDamageToTarget()
    {
        
        Vector2 physicalOrigin = (rb != null) ? rb.position : (Vector2)transform.position;

        // Push the damage circle forward directly in front of whichever way the player is looking
        Vector2 attackCenterPoint = physicalOrigin + (lastLookDir * (playerAttackRange * 0.5f));

        // Perform a fast 2D physics overlap scan to capture ALL targets inside your sword sweep
        Collider2D[] caughtEnemies = Physics2D.OverlapCircleAll(attackCenterPoint, playerAttackRange * 0.75f, enemyFactionLayers);   

        System.Collections.Generic.List<MonoBehaviour> hitVictimsList = new System.Collections.Generic.List<MonoBehaviour>();

        // Loop through every grunt caught inside your blade's reach
        for (int i = 0; i < caughtEnemies.Length; i++)
        {
            Collider2D targetCol = caughtEnemies[i];
            if (targetCol == null || targetCol.gameObject == this.gameObject) continue;

            // Extract the health component from the target body
            Health enemyHealth = targetCol.GetComponent<Health>();
            if (enemyHealth == null) enemyHealth = targetCol.GetComponentInParent<Health>();

            if (enemyHealth != null && enemyHealth.currentHealth > 0)
            {
                // Calculate individual knockback trajectories away from your player center point
                Vector2 targetBodyPos = targetCol.attachedRigidbody != null ? targetCol.attachedRigidbody.position : (Vector2)targetCol.transform.position;
                Vector2 strikeKnockbackVector = (targetBodyPos - physicalOrigin).normalized;
                if (strikeKnockbackVector == Vector2.zero) strikeKnockbackVector = lastLookDir;
               
                enemyHealth.TakeDamage(playerAttackDamage, physicalOrigin, strikeKnockbackVector * playerKnockbackForce, anim, rb);

                // Track this victim for the Basara freeze frames
                hitVictimsList.Add(enemyHealth);
            }
        }

        // BASARA HIT-STOP: Freeze time momentarily if your blade slices down target units!
        if (hitVictimsList.Count > 0 && HitLagManager.Instance != null)
        {
            Animator playerAnim = GetComponentInChildren<Animator>();
            if (playerAnim == null) playerAnim = anim;

            float playerHitLagDuration = 0.05f;
            HitLagManager.Instance.TriggerBasaraHitLag(
                playerAnim,
                rb,
                hitVictimsList,
                playerHitLagDuration
            );
        }
    }

    // Draws a red wire circle in front of your character inside the editor scene view to easily configure sizes
    private void OnDrawGizmosSelected()
    {
        Vector2 physicalOrigin = (rb != null) ? rb.position : (Vector2)transform.position;
        Vector2 attackCenterPoint = physicalOrigin + (lastLookDir * (playerAttackRange * 0.5f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCenterPoint, playerAttackRange * 0.75f);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // 🔥 THE AUTOMATED INSTANTIATION SAVIOR GATE:
        // If this character was spawned dynamically at runtime by the character select screen
        // and its inspector layer field is unassigned (0), forcefully bind it to your "Enemy" layer!
        if (enemyFactionLayers == 0)
        {
            // This reads your singular "Enemy" layer name directly from your project's layer matrix
            enemyFactionLayers = LayerMask.GetMask("Enemy");

            Debug.Log("<color=#00FFFF>[PLAYER RUNTIME INITIALIZATION]:</color> Faction layer masks successfully configured for runtime character select copy!");
        }
    }
    private void Update()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Gather manual running input keys
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        bool isPlayerTryingToMove = moveInput.sqrMagnitude > 0.0001f;

        // 🔥 THE AUTOMATED INPUT UNLOCKER:
        // If you are pushing your directional movement keys, double-check your combo script status.
        // If the combo script says it is done attacking, forcefully drop the attack lock!
        PlayerCombo combo = GetComponent<PlayerCombo>();
        if (combo == null) combo = GetComponentInChildren<PlayerCombo>();

        if (combo != null && !combo.isAttacking && isPlayerTryingToMove)
        {
            currentState = PlayerState.idle;
        }

        if (currentState == PlayerState.stagger) return;

        if (currentState == PlayerState.attack)
        {
            moveInput = Vector2.zero; // Freeze drift while swinging
        }
        else
        {
            if (isPlayerTryingToMove)
            {
                moveInput = moveInput.normalized;
                currentState = PlayerState.walk;
            }
            else
            {
                currentState = PlayerState.idle;
            }
        }

        AnimateChar();
    }
    private void FixedUpdate()
    {
        // 🟢 FIXED: Target your script's main class-level cached 'rb' variable directly!
        if (rb != null)
        {
            if (currentState == PlayerState.walk)
            {
                
                // Pull your non-zero movement speed variables directly
                rb.linearVelocity = moveInput * moveSpeed;
            }
            else if (currentState == PlayerState.idle)
            {
                rb.linearVelocity = Vector2.zero; // Stop instantly when keys are released
            }
        }
        else
        {
            // Emergency fallback if rb somehow cleared during the scene transition
            rb = GetComponent<Rigidbody2D>();
        }
    }
    private void AnimateChar()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.0001f;
        Vector2 animDir = moveInput;

        if (isMoving)
        {
            if (snapToDirAnim)
            {
                if (Mathf.Abs(animDir.x) >= Mathf.Abs(animDir.y))
                {
                    animDir = new Vector2(Mathf.Sign(animDir.x), 0f);
                }
                else
                {
                    animDir = new Vector2(0f, Mathf.Sign(animDir.y));
                }
            }
            lastLookDir = animDir;
        }
        else
        {
            animDir = lastLookDir;
        }

        if (anim != null)
        {
            anim.SetBool("isMoving", isMoving);
            anim.SetFloat("moveX", animDir.x);
            anim.SetFloat("moveY", animDir.y);
        }
    }

  
    private void OnDrawGizmos()
    {
        // THE ANIMATION GATE: Only render the attack circle if the player is actively swinging!
        if (currentState == PlayerState.attack)
        {
            Vector2 physicalOrigin = (rb != null) ? rb.position : (Vector2)transform.position;

            // Project the center point forward based on your last looked direction vector
            Vector2 attackCenterPoint = physicalOrigin + (lastLookDir * (playerAttackRange * 0.5f));

            // Render a semi-transparent cyan solid circle for a punchy, classic arcade feel
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);

            // 🟢 FIXED: Changed 'DrawSolidSphere' to the correct Unity method 'DrawSphere'
            Gizmos.DrawSphere(attackCenterPoint, playerAttackRange * 0.75f);

            // Add a crisp white outline so you can easily track the precise edge of your weapon sweep
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(attackCenterPoint, playerAttackRange * 0.75f);
        }
    }

    public void Knock(Rigidbody2D targetRb, float knockbackTime)
    {
        if (currentState == PlayerState.stagger) return;
        StartCoroutine(KnockbackCo(targetRb, knockbackTime));
    }

    private IEnumerator KnockbackCo(Rigidbody2D targetRb, float knockbackTime)
    {
        currentState = PlayerState.stagger;
        yield return new WaitForSeconds(knockbackTime);

        if (targetRb != null) targetRb.linearVelocity = Vector2.zero;
        currentState = PlayerState.idle;
    }

    public Vector2 GetLastLookDir() => lastLookDir;
}