using UnityEngine;


public class MinimapFollow : MonoBehaviour
{
    [Header("Fixed Map Settings")]
    [Tooltip("The static center point coordinates of your battlefield grid map")]
    public Vector2 mapCenterPosition = Vector2.zero;

    [Tooltip("How high above the 2D grid the minimap camera sits")]
    public float mapHeight = 50f;

    // Changed from LateUpdate to Start since a fixed map only needs to be positioned once!
    void Start()
    {
        // Set the camera to the absolute center of your stage layout
        Vector3 fixedPosition = new Vector3(mapCenterPosition.x, mapCenterPosition.y, -mapHeight);
        transform.position = fixedPosition;

        // Force a flat, straight top-down view (Zero rotation)
        // In 2D top-down projects, looking down the Z-axis is typically Quaternion.identity
        transform.rotation = Quaternion.identity;
    }
}

