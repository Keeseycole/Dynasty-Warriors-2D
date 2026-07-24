using UnityEngine;

public class AnimationProxy : MonoBehaviour
{
    private MusouUnit parentUnit;
    private PlayerController parentPlayer; // 🔥 ADDED: Support link tracking for the player entity

    void Awake()
    {
        // 1. Attempt to cache an AI soldier component link
        parentUnit = GetComponentInParent<MusouUnit>();

        // 2. If it is null, look for the player controller link instead!
        if (parentUnit == null)
        {
            parentPlayer = GetComponentInParent<PlayerController>();
        }
    }

    // This is the clean, single function the Animation Event timeline looks for
    public void ApplyDamageToTarget()
    {
        // Path A: If an AI unit is swinging, pass the signal over to its combat brain
        if (parentUnit != null)
        {
            parentUnit.ApplyDamageToTarget();
        }
        // Path B: 🔥 FIXED: If the Player is swinging, safely pass the signal over to their controller!
        else if (parentPlayer != null)
        {
            parentPlayer.ApplyDamageToTarget();
        }
        else
        {
            Debug.LogWarning($"AnimationProxy on {gameObject.name} has no valid Player or AI parent script component attached!");
        }
    }
}