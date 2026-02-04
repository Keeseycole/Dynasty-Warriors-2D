using System.Collections.Generic;
using UnityEngine;

public class DamageNumberController : MonoBehaviour
{

    public static DamageNumberController instance;

    public DamageNumber numberToSpawn;

    public Transform numberCanvas;

    private List<DamageNumber> numberpool = new List<DamageNumber>();

    private void Awake()
    {
        instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnDamage (float Damage, Vector3 location)
    {
        int rounded = Mathf.RoundToInt(Damage);

        //DamageNumber newDamage =  Instantiate(numberToSpawn, location, Quaternion.identity, numberCanvas);

        DamageNumber newDamage = GetFromPool();

        newDamage.setUp(rounded);
        newDamage.gameObject.SetActive(true);

        newDamage.transform.position = location;
    }

    public DamageNumber GetFromPool()
    {
        DamageNumber outputNum = null;

        if (numberpool.Count == 0) 
        { 

            outputNum = Instantiate(numberToSpawn, numberCanvas);

        }
        else
        {
            {
                outputNum = numberpool[0];
                numberpool.RemoveAt(0);
            }
        }

            return outputNum;
    }

    public void placeinPool(DamageNumber numToPlace) 
    {

        numToPlace.gameObject.SetActive(false);

        numberpool.Add(numToPlace);

    }
}
