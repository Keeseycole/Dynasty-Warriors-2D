using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerStatController : MonoBehaviour
{

    public static PlayerStatController instance;

    public List<PlayerStatValue> moveSpeed, attack;
    public int  AttackLevelCount, moveSpeedLevelCount;

    public int attackLevel,defenseLevel, SpeedLevel;

    private void Awake()
    {
        instance = this;
    }

  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = moveSpeed.Count - 1; i < moveSpeedLevelCount; i++ )
        {
            moveSpeed.Add(new PlayerStatValue(moveSpeed[i].cost + moveSpeed[1].cost, moveSpeed[i].value + (moveSpeed[1].value - moveSpeed[0].value)));
        }

        for (int i = attack.Count - 1; i < AttackLevelCount; i++)
        {
            attack.Add(new PlayerStatValue(attack[i].cost + attack[1].cost, attack[i].value + (attack[1].value - attack[0].value)));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[System.Serializable]

public class PlayerStatValue
{
    public int cost;

    public float value;

    public PlayerStatValue(int newCost, float newValue)
    {
        cost = newCost;
        value = newValue;
    }
}



