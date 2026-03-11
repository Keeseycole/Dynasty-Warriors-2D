using UnityEngine;

public class PlayerMinimapIcon : MonoBehaviour
{

    public PlayerController player;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate the angle based on the player's lastLookDir
        float angle = Mathf.Atan2(player.GetLastLookDir().y, player.GetLastLookDir().x) * Mathf.Rad2Deg;
        // Adjust by -90 if your arrow sprite points Up by default
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}
