using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class sleepEnemy : Enemy
{
    [Header("Base Core Components")]
    public Rigidbody2D rb;
    public Transform currentTarget;
    public Animator animator;

    [Header("Base Settings")]
    public float chaseRadius;
    public float attackRadius;

    private Vector2 lastFacingDir;
    public Vector2 GetFacingDirection() => lastFacingDir;

    // Cache animator performance hashes
    private static readonly int MoveXHash = Animator.StringToHash("moveX");
    private static readonly int MoveYHash = Animator.StringToHash("moveY");
    private static readonly int WakeUpHash = Animator.StringToHash("wakeUp");

    public virtual void Start()
    {
        currentState = EnemyState.Idle;

        // Try to find the Rigidbody on this object, if null check the children instantly!
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody2D>();
        }

        // Try to find the Animator on this object, if null check the children instantly!
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Safety verification logs
        if (animator == null)
        {
            Debug.LogError($"[PREFAB ERROR] {gameObject.name} is completely missing an Animator component on its root or children!");
        }
        if (rb == null)
        {
            Debug.LogError($"[PREFAB ERROR] {gameObject.name} is completely missing a Rigidbody2D component on its root or children!");
        }

        // Default look-up fallback if not handled by a higher manager
        currentTarget = null;
       
    }

    // High frequency physics loop
    protected virtual void FixedUpdate()
    {
        CheckDistance();
    }
    

    /// <summary>
    /// Base idle/wake tracking. Overridden completely by MusouUnit for advanced combat/marching loops.
    /// </summary>
    public virtual void CheckDistance()
    {
        // Safety gate to avoid game-breaking NullReference crashes
        if (currentTarget == null)
        {
            if (animator != null) animator.SetBool(WakeUpHash, false);
            return;
        }

        // Use the physical Rigidbody position for accurate 2D grid spacing checks
        Vector2 physicalPos = (rb != null) ? rb.position : (Vector2)transform.position;
        float distToTargetSqr = ((Vector2)currentTarget.position - physicalPos).sqrMagnitude;

        float chaseRadiusSqr = chaseRadius * chaseRadius;
        float attackRadiusSqr = attackRadius * attackRadius;

        if (distToTargetSqr <= chaseRadiusSqr && distToTargetSqr > attackRadiusSqr)
        {
            if ((currentState == EnemyState.Idle || currentState == EnemyState.Walk) && currentState != EnemyState.Stagger)
            {
                // Pure linear translation layout fallback for very basic sleep enemies
                Vector2 targetStep = Vector2.MoveTowards(physicalPos, currentTarget.position, moveSpeed * Time.fixedDeltaTime);

                ChangeAnim(targetStep - physicalPos);

                if (rb != null) rb.MovePosition(targetStep);

                ChangeState(EnemyState.Walk);
                if (animator != null) animator.SetBool(WakeUpHash, true);
            }
        }
        else if (distToTargetSqr > chaseRadiusSqr)
        {
            if (animator != null) animator.SetBool(WakeUpHash, false);
        }
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
        }
    }

    public void ChangeAnim(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        lastFacingDir = dir.normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            SetAnimFloat(dir.x > 0 ? Vector2.right : Vector2.left);
        }
        else
        {
            SetAnimFloat(dir.y > 0 ? Vector2.up : Vector2.down);
        }
    }

    private void SetAnimFloat(Vector2 setVec)
    {
        if (animator == null) return;
        animator.SetFloat(MoveXHash, setVec.x);
        animator.SetFloat(MoveYHash, setVec.y);
    }
}