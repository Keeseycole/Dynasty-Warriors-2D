using UnityEngine;

public class BasicTimerSpawner : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("Drag the prefab you want to spawn here")]
    public GameObject prefabToSpawn;

    [Tooltip("How many seconds to wait before spawning the object")]
    public float delayTime = 3.0f;

    private float timer = 0f;
 

    private void Update()
    {
    
        // Count up the time elapsed
        timer += Time.deltaTime;

        // The exact frame the timer passes your delay threshold, trigger the spawn!
        if (timer >= delayTime)
        {
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
       prefabToSpawn.gameObject.SetActive(true);
    }
}