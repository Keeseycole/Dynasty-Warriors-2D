using System.Collections.Generic;
using UnityEngine;

public class FireAttack : MonoBehaviour
{

    private PolygonCollider2D polygonCollider;
    private bool zoneActivated = false;

    public GameObject objtoActivate;

    private void Awake()
    {
        polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider == null)
        {
            Debug.LogError($"[TACTICAL] {gameObject.name} must have a PolygonCollider2D attached!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (zoneActivated) return;

        // Check if the moving unit stepping into the shape boundary is our friendly vanguard soldier
        MusouUnit soldier = other.GetComponent<MusouUnit>();
        if (soldier != null && soldier.unitTeam == MusouUnit.Team.PlayerSide)
        {
            zoneActivated = true;
            Debug.Log($"[TACTICAL] Fire Captain {other.gameObject.name} infiltrated the polygon zone! Running mathematical coordinate sweep...");

            // LIVE GEOMETRY SWEEP: Pull absolute frame-perfect targets caught inside our polygon bounds right now
            List<Health> caughtEnemies = GatherEnemiesByMathematicalContains();

            // Pass our pristine target list over to the BattleEventManager
            if (BattleEventManager.Instance != null)
            {
                BattleEventManager.Instance.ExecuteFireAttackCutscene(transform.position, caughtEnemies);
            }
        }
    }

    /// <summary>
    /// Pure Mathematical Check: Looks at every active unit on the map and checks 
    /// if their world coordinates physically sit inside the polygon line boundary.
    /// Completely bypasses all Unity physics layer, matrix, and contact filter bugs!
    /// </summary>
    private List<Health> GatherEnemiesByMathematicalContains()
    {
        List<Health> victims = new List<Health>();
        if (polygonCollider == null) return victims;

        // 1. Fetch the raw local polygon points drawn in the inspector and convert them to true world-space positions
        Vector2[] localPoints = polygonCollider.points;
        Vector2[] worldPoints = new Vector2[localPoints.Length];

        for (int i = 0; i < localPoints.Length; i++)
        {
            worldPoints[i] = polygonCollider.transform.TransformPoint(localPoints[i]);
        }

        // 2. Query ALL registered combat units currently alive in your game via the BattleManager tracking registry
        if (BattleManager.Instance == null || BattleManager.Instance.activeUnits == null)
        {
            Debug.LogError("[TACTICAL] BattleManager.Instance.activeUnits is null or missing from your scene!");
            return victims;
        }

        // Copy the list to avoid collection modification errors mid-frame
        List<Health> allCurrentUnits = new List<Health>(BattleManager.Instance.activeUnits);

        foreach (Health unit in allCurrentUnits)
        {
            if (unit == null || unit.currentHealth <= 0) continue;

            // Ensure we are only targeting the enemy army faction
            if (unit.gameObject.CompareTag("Enemy"))
            {
                Vector2 unitPos = unit.transform.position;

                // 3. THE MATH CHECK: Use the Ray-Casting algorithm to test if the point is inside the shape
                if (IsPointInPolygon(unitPos, worldPoints))
                {
                    if (!victims.Contains(unit))
                    {
                        victims.Add(unit);
                    }
                }
            }
        }

        Debug.Log($"[MATHEMATICAL SWEEP] Found {victims.Count} active enemies verified inside the polygon perimeter lines.");
        return victims;
    }

    /// <summary>
    /// Standard Ray-Casting Algorithm for testing if a 2D point is inside an irregular polygon shape.
    /// </summary>
    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool isInside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y < point.y && polygon[j].y >= point.y || polygon[j].y < point.y && polygon[i].y >= point.y) &&
                (polygon[i].x + (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < point.x))
            {
                isInside = !isInside;
            }
            j = i;
        }

        return isInside;
    }
}





