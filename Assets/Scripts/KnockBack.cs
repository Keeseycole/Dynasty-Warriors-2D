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
        if (other.gameObject.CompareTag(TagtoHit) && other.isTrigger)
        {

            Rigidbody2D hit = other.GetComponentInParent<Rigidbody2D>();

            if (hit != null)
            {
                Vector3 difference = hit.transform.position - transform.position;

                difference = difference.normalized * thrust;

                hit.DOMove(hit.transform.position + difference, knockbackTime);


               // hit.AddForce(difference, ForceMode2D.Impulse);

                if (other.gameObject.CompareTag("Enemy"))
                {
                    hit.GetComponent<Enemy>().currentState = EnemyState.Stagger;

                    other.GetComponent<Enemy>().Knock(hit, knockbackTime);
                }

                if (other.gameObject.CompareTag("Player"))
                {
                    other.GetComponentInParent<PlayerController>().Knock(hit, knockbackTime);

                    hit.GetComponent<PlayerController>().currentState = PlayerState.stagger;
                   
                }
              
            }

        }
    }

}
