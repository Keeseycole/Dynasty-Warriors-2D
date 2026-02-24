using UnityEngine;

public class DamageGiver : MonoBehaviour
{

    Health health;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = GetComponent<Health>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
