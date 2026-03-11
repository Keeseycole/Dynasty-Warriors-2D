using UnityEngine;

public class MinimapFollow : MonoBehaviour
{

    public Transform player; // Drag your Player object here in the Inspector
    public float mapHeight = 50f; // Height above the player

    void LateUpdate()
    {
        if (player == null) return;

        // Position the camera directly above the player
        Vector3 newPosition = player.position;
        newPosition.y += mapHeight;
        transform.position = newPosition;

        // Rotate the camera to match the player's Y-axis (Dynasty Warriors style)
        // This keeps the direction the player is facing at the "top" of the map
        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
    }
}

