using UnityEngine;

public class KillEnemySpawner : MonoBehaviour
{

    public GameObject triggerZone;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("TaskUnit"))
        {
            triggerZone.gameObject.SetActive(true);
        }
    }
}
