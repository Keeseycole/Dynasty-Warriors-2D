using UnityEngine;


public class SiegeWeapon : MonoBehaviour
{
    private Health health; // Reference to your existing Health script
    private bool isDestroyed = false;

    void Start()
    {
        health = GetComponent<Health>();
    }

    void Update()
    {
        // Check if the health script drops to zero or below
        if (health != null && health.currentHealth <= 0 && !isDestroyed)
        {
            HandleDestruction();
        }
    }

    private void HandleDestruction()
    {
        isDestroyed = true;


        // Play an explosion effect here if you want!
        Destroy(gameObject);
    }
}
