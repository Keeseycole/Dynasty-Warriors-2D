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

        // 1. Use your existing list (Performance Fix)
        foreach (var unit in activeUnits)
        {
            if (unit == null) continue;

            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(unit.transform.position.x / cellSize),
                Mathf.FloorToInt(unit.transform.position.y / cellSize)
            );

            if (!unitGrid.ContainsKey(gridPos)) unitGrid[gridPos] = new List<Health>();
            unitGrid[gridPos].Add(unit);
        }

        // 2. Simulated Battle Loop
        foreach (var cell in unitGrid.Values)
        {
            if (cell.Count < 2) continue;

            foreach (var unitA in cell)
            {
                // NOTE: Change this to !isSimulating because you want to 
                // simulate the ones the player CANNOT see.
                if (!unitA.isSimulating) continue;

                MusouUnit aiA = unitA.GetComponent<MusouUnit>();

                foreach (var unitB in cell)
                {
                    if (unitA == unitB) continue;
                    MusouUnit aiB = unitB.GetComponent<MusouUnit>();

                    // 3. Team Check & Pulse Trigger
                    if (aiA != null && aiB != null && aiA.unitTeam != aiB.unitTeam)
                    {
                  
                        // DW3 FEEL: Assign target so the Minimap Pulse starts!
                        aiA.currentTarget = unitB.transform;

                        // Apply a tiny amount of damage over time
                        // 🟢 FIXED: Explicitly cast the null parameters to clarify the exact function signature!
                        unitA.TakeDamage(0.5f * Time.deltaTime, unitB.transform.position, Vector2.zero, (Animator)null, (Rigidbody2D)null);

                        // Break so we don't calculate multiple combat loops per grid tick
                        break;
                    }
                }
            }
        }
    }
}
