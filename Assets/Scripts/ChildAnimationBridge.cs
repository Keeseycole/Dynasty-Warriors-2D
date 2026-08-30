using UnityEngine;

public class ChildAnimationBridge : MonoBehaviour
{
    private MusouUnit parentMusouUnit;

    void Awake()
    {
        // 🔥 THE UNIVERSAL PARENT RADAR:
        // Scans all parent layers recursively until it locks onto your root MusouUnit controller!
        // This works out of the box for both the Player character and AI units identically.
        parentMusouUnit = GetComponentInParent<MusouUnit>();

        if (parentMusouUnit == null)
        {
            Debug.LogWarning($"[ANIMATION BRIDGE]: '{gameObject.name}' could not find a MusouUnit component on its parent objects tree layer!");
        }
    }

    /// <summary>
    /// Catch the timeline frame cue and instantly pipe it straight up to the parent physics engine!
    /// </summary>
    public void TriggerLungeFromAnimationEvent()
    {
        if (parentMusouUnit != null)
        {
            parentMusouUnit.TriggerLungeFromAnimationEvent();
        }
    }
}