using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ExperianceController : MonoBehaviour
{

    public static ExperianceController instance;

    public int currentExp;

    public ExpPickup pickup;

    public List<int> expLevels;

    public int currentLevel = 1, levelCount = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;

        while (expLevels.Count < levelCount) 
        {

            expLevels.Add(Mathf.CeilToInt(expLevels[expLevels.Count - 1] * 1.1f));

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetExp(int amountToGet)
    {
        currentExp += amountToGet;

        if (currentExp >= expLevels[currentLevel]) 
        { 

            LevelUp();

        }

        UIController.instance.UpdateExp(currentExp, expLevels[currentLevel], currentLevel);
    }

    public void SpawnExp(Vector3 position, int expValue)
    {
        Instantiate(pickup, position, Quaternion.identity).expValue = expValue;
    }

    public void LevelUp()
    {
        currentExp -= expLevels[currentLevel];


        currentLevel++;

        if(currentLevel >= expLevels.Count) 
            {

            currentLevel = expLevels.Count - 1;

            }
    }
}
