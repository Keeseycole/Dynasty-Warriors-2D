using UnityEngine;

public class HealthUpgrade : MonoBehaviour
{

    public int increaseAmount;

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("Something hit the item: " + other.name); // If this doesn't show up, check your Colliders/Rigidbodys

            if (other.CompareTag("Player"))
            {
                {
                    Debug.Log("Player tag detected!"); // If this doesn't show up, check your Tags

                    PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        // 2. Now you can use it here
                        playerHealth.IncreaseMaxHealth(increaseAmount);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }

