using UnityEngine;

public class sleepEnemy : Enemy
{

    public Rigidbody2D rb;

    public Transform target;

    public float chaseRadius;

    public float attackRadius;

    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = EnemyState.Idle;
        target = GameObject.FindWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckDistance();
    }

    public virtual void CheckDistance()
    {
        if (Vector3.Distance(target.position, transform.position) <= chaseRadius &&
            Vector3.Distance(target.position, transform.position) > attackRadius)
        {
            if (currentState == EnemyState.Idle || currentState == EnemyState.Walk &&
                currentState != EnemyState.Stagger)
            {

                Vector3 temp = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

                ChangeAnim(temp - transform.position);
                rb.MovePosition(temp);
                ChangeState(EnemyState.Walk);
                animator.SetBool("wakeUp", true);

            }
        } else if (Vector3.Distance(target.position, transform.position) > chaseRadius)
        {
            animator.SetBool("wakeUp", false);
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
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {

            if(dir.x > 0)
            {
                SetAnimFloat(Vector2.right);
            } else if (dir.x < 0)
            {
                SetAnimFloat(Vector2.left);
            }

        } else if (Mathf.Abs(dir.x) < Mathf.Abs(dir.y))
            
        {
            if (dir.y > 0)
            {
                SetAnimFloat(Vector2.up);
            }
            else if (dir.y < 0)
            {
                SetAnimFloat(Vector2.down);
            }
        }
    }

    private void SetAnimFloat(Vector2 setVec)
    {
        animator.SetFloat("moveX", setVec.x);
        animator.SetFloat("moveY", setVec.y);
    }

}
