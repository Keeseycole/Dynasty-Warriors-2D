using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public enum EnemyState
{
    Idle,
    Walk,
    Attack,
    Stagger
}

public class Enemy : MonoBehaviour
{
    public EnemyState currentState;

    public int health;

    public int baseAttack;

    public string enemyName;

    public float moveSpeed;
   
    
    public void Knock(Rigidbody2D rb, float knockbackTime)
    {
        StartCoroutine(KnockbackCo(rb, knockbackTime));
    }
    private IEnumerator KnockbackCo(Rigidbody2D rb, float knockbackTime)
    {
        if (rb != null)
        {
            yield return new WaitForSeconds(knockbackTime);

            rb.linearVelocity = Vector2.zero;

            currentState = EnemyState.Idle;

            rb.linearVelocity = Vector2.zero;
        }
    }
}


