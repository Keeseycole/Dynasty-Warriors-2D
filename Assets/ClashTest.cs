using UnityEngine;

public class ClashTest: MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("<color=cyan>[HARNESS] Key 'C' detected!</color> Scanning scene...");

            if (WeaponClashManager.Instance == null)
            {
                Debug.LogError("[HARNESS ERROR] WeaponClashManager is missing from your active scene!");
                return;
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[HARNESS ERROR] Could not find any GameObject tagged 'Player'!");
                return;
            }

            PlayerController playerUnit = FindFirstObjectByType<PlayerController>();

            if (playerUnit == null)
            {
                // Deep fallback search if your version of Unity expects classic naming conventions
                playerUnit = FindObjectOfType<PlayerController>();
            }

            if (playerUnit == null)
            {
                Debug.LogError("[HARNESS ERROR] Could not find any 'PlayerController' component script running in this scene!");
                return;
            }

            // Find closest enemy officer
            SquadLeader closestLeader = null;
            float closestDistSqr = Mathf.Infinity;

            SquadLeader[] allLeaders = FindObjectsByType<SquadLeader>(FindObjectsSortMode.None);
            Vector2 playerPos = (Vector2)playerObj.transform.position;

            foreach (var leader in allLeaders)
            {
                if (leader == null) continue;

                Vector2 leaderPos = leader.rb != null ? leader.rb.position : (Vector2)leader.transform.position;
                float dSqr = (leaderPos - playerPos).sqrMagnitude;

                if (dSqr < closestDistSqr)
                {
                    closestDistSqr = dSqr;
                    closestLeader = leader;
                }
            }

            if (closestLeader == null)
            {
                Debug.LogError("[HARNESS ERROR] There are no Enemy 'SquadLeader' objects active on the map.");
                return;
            }

            Debug.Log($"<color=green>[HARNESS SUCCESS]</color> Forcing clash between Player and {closestLeader.gameObject.name}!");
            WeaponClashManager.Instance.InitiateClash(playerUnit, closestLeader);
        }
    }
}
