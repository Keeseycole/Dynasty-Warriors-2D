using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using Random = UnityEngine.Random;

public class MeleeEnemy : sleepEnemy
{

    private List<IEnumerator> ComboChains;

    [SerializeField] bool comboStarted = false;

    public float strafeMagnitude = 1f; // How far to strafe sideway

    public float strafeDuration = 1.5f;

    public float strafeSpeed;

    public float strafeTimer = 2f;

    private int strafeDirection = 1; // 1 for right, -1 for left

    public int randomValue;

    public bool chooseCombo;

    public bool chooseStrafe;

    private float chooseTimer = 5f;


    private void Update()
    {
        ComboChains = new List<IEnumerator>
        {
            Combo1(),
            Combo2(),
            Combo3(),
           
        };

        strafeTimer -= Time.deltaTime;

        chooseTimer -= Time.deltaTime;

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
                //animator.SetBool("isMoving", true);

            }
        }
        else if (Vector3.Distance(target.position, transform.position) <= chaseRadius &&
            Vector3.Distance(target.position, transform.position) <= attackRadius)
        {
          
            if (currentState == EnemyState.Idle || currentState == EnemyState.Walk &&
                currentState != EnemyState.Stagger)
            {
                //StartCoroutine(RandomBoolRoutine());
                StartCoroutine(ChooseCombo());
                StartCoroutine(ChooseStrafe());
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
        yield return new WaitForSeconds(1f);
        comboStarted = false;
    }

    public IEnumerator Combo2()
    {
        comboStarted = true;
        yield return new WaitForSeconds(.1f);
        StartCoroutine(Attack1());
        yield return new WaitForSeconds(1f);
        StartCoroutine(Attack2());
        yield return new WaitForSeconds(1f);
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
        yield return new WaitForSeconds(1f);
        comboStarted = false;
    }

    public IEnumerator ChooseCombo()
    {
        if (!comboStarted)
        {       
            yield return StartCoroutine(ComboChains[Random.Range(0, ComboChains.Count)]);
        }
    }

    public IEnumerator ChooseStrafe()
    {    
            yield return StartCoroutine(Strafe());        
    }

    public IEnumerator StartRandomCoroutine()
    {
        yield return new WaitForSeconds(chooseTimer);

        yield return StartCoroutine(ChooseStrafe());

        yield return new WaitForSeconds(chooseTimer);

        yield return  StartCoroutine(ChooseCombo());
                    
    }

    IEnumerator RandomBoolRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(chooseTimer);

            // Generate two random bools (50/50 chance)
            chooseStrafe = Random.value > 0.5f;
            chooseCombo = UnityEngine.Random.value > 0.5f;

            //Debug.Log($"Bool1: {bool1}, Bool2: {bool2}");

            // Wait for the specified time
           
        }
    }

    public IEnumerator Strafe()
    {
        //chooseStrafe = true;
        yield return new  WaitForSeconds(.1f);

        currentState = EnemyState.Strafe;
        animator.SetBool("isStrafing", true);

        if (strafeTimer <= 0)
        {
            strafeTimer = 0;
            strafeDirection *= -1; // Reverse direction
            strafeTimer = strafeDuration;
        }

        Vector3 directionToTarget = target.position - transform.position;

        Vector3 perpendicularDirection = new Vector3(directionToTarget.y, -directionToTarget.x, 0f);

        // Calculate strafe movement (using transform.right for local right, or a calculated perpendicular vector)
        Vector3 strafeMovement = perpendicularDirection * strafeDirection * strafeMagnitude * Time.deltaTime;

        transform.position -= strafeMovement; // Add movement
   
        directionToTarget.Normalize(); // Normalize to get just the direction
    
        // This vector points along the circle's circumference.
        Vector3 movementVector = perpendicularDirection * strafeSpeed * Time.deltaTime;

      

    }

}
