using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    public float cellSize = 10f; // Size of each "battle zone"
    
    // A dictionary where the key is the Grid Coordinates, and value is a list of units there
    private Dictionary<Vector2Int, List<Health>> unitGrid = new Dictionary<Vector2Int, List<Health>>();

    public List<Health> activeUnits = new List<Health>();

    private void Awake()
    {
        // If there is already an instance, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // Don't let the manager be destroyed if you change levels
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        // Run simulation every X frames or seconds
        SimulateOffScreenBattles();
    }

    void SimulateOffScreenBattles()
    {
        unitGrid.Clear();

        // 1. Sort all units into grid cells
        Health[] allUnits = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(unit.transform.position.x / cellSize),
                Mathf.FloorToInt(unit.transform.position.y / cellSize)
            );

            if (!unitGrid.ContainsKey(gridPos)) unitGrid[gridPos] = new List<Health>();
            unitGrid[gridPos].Add(unit);
        }

        // 2. Units in the same cell fight each other
        foreach (var cell in unitGrid.Values)
        {
            if (cell.Count < 2) continue; // Need at least 2 units to fight

            foreach (var unitA in cell)
            {
                if (!unitA.isSimulating) continue; // Only simulate off-screen units

                foreach (var unitB in cell)
                {
                    // If they are on different teams, apply simulated damage
                    // if (unitA.faction != unitB.faction) 
                    // {
                    //    unitA.SimulatedDamage(1f * Time.deltaTime);
                    // }
                }
            }
        }
    }
}
