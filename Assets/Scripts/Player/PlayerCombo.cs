using UnityEngine;
using UnityEngine.InputSystem;
public enum ComboState
{
    None,
    Attack1,
    Attack2, 
    Attack3, 
    Attack4,
    Attack5

}
public class PlayerCombo : MonoBehaviour
{
    PlayerState playerState;

    PlayerController playerController;


    private CharecterAnimations attackAnim;

    private bool ActivateResetTimer;

    private float defultComboTimer = .6f;

    private float currentComboTimer;

    private ComboState currentComboState;

   public bool isAttacking;

    [Header("Attack Movement")]
    public float basicStepForce = 3f;
    public float finisherStepForce = 7f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        attackAnim= GetComponent<CharecterAnimations>();

        playerController = GetComponent<PlayerController>();
    }


    private void Start()
    {
        currentComboTimer = defultComboTimer;

        currentComboState = ComboState.None;
    }

    // Update is called once per frame
    void Update()
    {
        ComboAttacks();
        ResetComboState();
    }

    public void ComboAttacks()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
           
            // Don't skip states! Only go to the next one if we aren't at the end
            if (isAttacking||currentComboState == ComboState.Attack5) return;

            currentComboState++;
            ActivateResetTimer = true;
            currentComboTimer = defultComboTimer;

            // --- ADD THIS BIT ---
            float force = (currentComboState == ComboState.Attack5) ? finisherStepForce : basicStepForce;
            Vector2 stepDir = playerController.lastLookDir;

            // Apply a quick burst of movement
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            rb.AddForce(stepDir * force, ForceMode2D.Impulse);

            // Trigger the specific animation
            // Using a Switch statement is cleaner than 5 'if' blocks!
            switch (currentComboState)
            {
                case ComboState.Attack1: attackAnim.Attack1(); break;
                case ComboState.Attack2: attackAnim.Attack2(); break;
                case ComboState.Attack3: attackAnim.Attack3(); break;
                case ComboState.Attack4: attackAnim.Attack4(); break;
                case ComboState.Attack5: attackAnim.Attack5(); break;
            }

        }
    }
    public void ResetComboState()
    {
        if (ActivateResetTimer)
        {
            currentComboTimer -= Time.deltaTime;

            if (currentComboTimer <= 0)
            {
                currentComboState = ComboState.None;

                ActivateResetTimer = false;

                currentComboTimer = defultComboTimer;
            }
        }
    }
    public void FinishAttack()
    {
        isAttacking = false;
    }
    void CheckForHit()
    {
        float range = 1.5f;
        float damage = 10f;
        float knockbackForce = 5f; // Standard push for normal hits

        // --- ATTACK 5 MODIFIERS ---
        if (currentComboState == ComboState.Attack5)
        {
           // range = 3.0f;          // Double the reach for a big finisher
           // damage = 30f;         // Triple the damage
            knockbackForce = 15f; // SEND THEM FLYING!
        }

        LayerMask enemyLayer = LayerMask.GetMask("Enemy");
        Vector2 attackDir = playerController.lastLookDir;
        Vector2 attackPos = (Vector2)transform.position + attackDir * 1.0f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, range, enemyLayer);

        foreach (Collider2D enemy in hits)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                // Calculate a direction AWAY from the player
                Vector2 dir = (enemy.transform.position - transform.position).normalized;

                // Pass the knockback force into our TakeDamage function
                enemyHealth.TakeDamage(damage, transform.position, dir * knockbackForce);

                StartCoroutine(Hitstop(0.05f));
            }
        }
    }

    private System.Collections.IEnumerator Hitstop(float duration)
    {
        Time.timeScale = 0.5f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

}
