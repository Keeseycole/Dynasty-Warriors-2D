using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Health : MonoBehaviour
{
    public float currentHealth, maxHealth;
    public FloatingHealthBar healthBar;

    private Animator anim;

    // DW3 References
    private MeleeEnemy enemyAI;
    private Rigidbody2D rb;

    private void Awake()
    {
        currentHealth = maxHealth;
        enemyAI = GetComponent<MeleeEnemy>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {

        if (enemyAI != null)
        {
            enemyAI.TriggerHit();
        }

        if (currentHealth <= 0) return;

        healthBar.gameObject.SetActive(true);

        currentHealth -= damage;

        StartCoroutine(HitlagRoutine(0.05f));

        // 1. Update the UI Bar
        if (healthBar != null)

            healthBar.UpdateBar(currentHealth, maxHealth);

        // 2. DW3 STAGGER: Tell the AI to "Flinch" so they stop attacking
        if (enemyAI != null)
        {
            enemyAI.ChangeState(EnemyState.Stagger);
        }

        // 4. Automatic Death Check
        if (currentHealth <= 0) Die();
    }

    public void Die()
    {
        // Notify the squad leader if this unit dies
        //SquadLeader leader = GetComponent<SquadLeader>();
        //if (leader != null) leader.OnLeaderDeath();

        Destroy(gameObject);
    }

    private IEnumerator HitlagRoutine(float duration)
    {
        if (anim == null || rb == null) yield break;

        // 1. FREEZE: Stop the animation and physics movement
        float originalSpeed = anim.speed;
        Vector2 originalVelocity = rb.linearVelocity;

        anim.speed = 0;
        rb.linearVelocity = Vector2.zero;

        // 2. WAIT: This creates the "crunchy" impact feel
        yield return new WaitForSeconds(duration);

        // 3. UNFREEZE: Restore everything
        anim.speed = originalSpeed;
        rb.linearVelocity = originalVelocity;
    }

}