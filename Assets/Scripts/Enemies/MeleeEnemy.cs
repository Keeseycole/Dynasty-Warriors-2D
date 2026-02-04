using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : sleepEnemy
{

    private List<IEnumerator> ComboChains;

    [SerializeField] bool comboStarted = false;

    private void Update()
    {
        ComboChains = new List<IEnumerator>
        {
            Combo1(),
            Combo2(),
            Combo3()
        };
    }

    public override void CheckDistance()
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
              
            }
        }
        else if (Vector3.Distance(target.position, transform.position) <= chaseRadius &&
            Vector3.Distance(target.position, transform.position) <= attackRadius)
        {
            if (currentState == EnemyState.Idle || currentState == EnemyState.Walk &&
                currentState != EnemyState.Stagger)
            {
                if (!comboStarted)
                {
                    StartCoroutine(ComboChains[Random.Range(0, ComboChains.Count)]);
                }
                
            }
        }
    }

    public IEnumerator Attack1()
    {
        currentState = EnemyState.Attack;
        animator.SetBool("attack1", true);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Idle;
        animator.SetBool("attack1", false);
    }

    public IEnumerator Attack2()
    {
        currentState = EnemyState.Attack;
        animator.SetBool("attack2", true);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Idle;
        animator.SetBool("attack2", false);
    }

    public IEnumerator Attack3()
    {
        currentState = EnemyState.Attack;
        animator.SetBool("attack3", true);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Idle;
        animator.SetBool("attack3", false);
    }

    public IEnumerator Combo1()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
       StartCoroutine(Attack1());
        yield return new WaitForSeconds(1.5f);
        comboStarted = false;
    }

    public IEnumerator Combo2()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
        StartCoroutine(Attack1());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack2());
        yield return new WaitForSeconds(1.5f);
        comboStarted = false;
    }

    public IEnumerator Combo3()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
        StartCoroutine(Attack1());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack2());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack3());
        yield return new WaitForSeconds(1.5f);
        comboStarted = false;
    }

}
