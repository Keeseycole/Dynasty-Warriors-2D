using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class KnockBack : MonoBehaviour
{

    [SerializeField] private float thrust;

    [SerializeField] private float knockbackTime;

    [SerializeField] private string TagtoHit;


    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check tag and ensure it's the trigger we want
        if (other.gameObject.CompareTag(TagtoHit))
        {
            // 2. Get the Rigidbody from the PARENT (where the main script usually lives)
            Rigidbody2D hitRb = other.GetComponentInParent<Rigidbody2D>();

            if (hitRb != null)
            {
                Vector3 difference = (hitRb.transform.position - transform.position).normalized * thrust;

                // Apply the DOTween movement
                hitRb.DOMove(hitRb.transform.position + difference, knockbackTime);

                // 3. Handle Enemy Logic SAFELY
                Enemy enemyScript = hitRb.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.currentState = EnemyState.Stagger;
                    enemyScript.Knock(hitRb, knockbackTime);
                    return; // Exit so we don't check Player logic
                }

                // 4. Handle Player Logic SAFELY
                PlayerController playerScript = hitRb.GetComponent<PlayerController>();
                if (playerScript != null)
                {
                    playerScript.currentState = PlayerState.stagger;
                    playerScript.Knock(hitRb, knockbackTime);
                }
            }
        }
    }

}
