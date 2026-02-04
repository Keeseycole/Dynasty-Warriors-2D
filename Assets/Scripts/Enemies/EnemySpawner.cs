using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
public class EnemySpawner : MonoBehaviour
{

    public GameObject enemyToSpawn;

    public float timeToSpawn;

    private float spawnCounter;

    public Transform minSpawn, maxSpawn;

    private float despawnDistance;

    public List<GameObject> spawnedEnemies = new List<GameObject>();

    public int PerFrameCheck;

    private int enemyCheck;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnCounter = timeToSpawn;

        despawnDistance = Vector3.Distance(transform.position, maxSpawn.position) + 4f;
    }

    // Update is called once per frame
    void Update()
    {
        spawnCounter -= Time.deltaTime;

        if (spawnCounter <= 0)
        {
            spawnCounter = timeToSpawn;

            //Instantiate(enemyToSpawn, transform.position, transform.rotation);

            GameObject newenemy = Instantiate(enemyToSpawn, SelectSpawnPoint(), transform.rotation);

            spawnedEnemies.Add(newenemy);

            int checkTar = enemyCheck + PerFrameCheck;

            while (enemyCheck < checkTar)
            {

                if (enemyCheck < spawnedEnemies.Count)
                {

                    if (spawnedEnemies[enemyCheck] != null)
                    {

                        if(Vector3.Distance(transform.position, spawnedEnemies[enemyCheck].transform.position) > despawnDistance)
                        {

                            Destroy(spawnedEnemies[enemyCheck]);

                            spawnedEnemies.RemoveAt(enemyCheck);
                            checkTar--;

                        } else
                        {
                            enemyCheck++;
                        }

                    } else
                    {
                        spawnedEnemies.RemoveAt(enemyCheck);
                        checkTar--;
                    }

                } else
                {
                    enemyCheck = 0;
                    checkTar = 0;
                }


            }
        }
    }

    public Vector3 SelectSpawnPoint()
    {
        Vector3 spawnPoint = Vector3.zero;

        bool spawnVertEdge = Random.Range(0f, 1f) > 0.5f;

         if(spawnVertEdge)
        {
            spawnPoint.y = Random.Range(minSpawn.position.y, maxSpawn.position.y);

            if(Random.Range(0f, 1f) > .5f)
            {
                spawnPoint.x = maxSpawn.position.x;
            } else
            {
                spawnPoint.x = minSpawn.position.x;
            }

        } else
        {
            spawnPoint.y = Random.Range(minSpawn.position.x, maxSpawn.position.x);

            if (Random.Range(0f, 1f) > .5f)
            {
                spawnPoint.y = maxSpawn.position.y;
            }
            else
            {
                spawnPoint.y = minSpawn.position.y;
            }
        }


        return spawnPoint;
    }
}


