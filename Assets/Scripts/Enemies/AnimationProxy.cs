using UnityEngine;

public class AnimationProxy : MonoBehaviour
{
    private MusouUnit parentUnit;

    void Awake()
    {
        // Automatically finds the script on the parent
        parentUnit = GetComponentInParent<MusouUnit>();
    }

    // This is the function the Animation Event is looking for
    public void ApplyDamageToTarget()
    {
        if (parentUnit != null)
        {
            Debug.Log("Swing triggered!");
            parentUnit.ApplyDamageToTarget();
        }
    }
}