using UnityEngine;
using System.Collections.Generic;

public class AttackDirector : MonoBehaviour
{
    public static AttackDirector instance;

    [Header("Settings")]
    [Tooltip("How many enemies can attack the player at once")]
    public int maxAttackerSlots = 3;

    // Tracks: Who is being targeted -> How many people are currently swinging at them
    private Dictionary<Transform, int> targetRegistry = new Dictionary<Transform, int>();

    void Awake()
    {
        // Singleton pattern: Let every enemy find this easily
        if (instance == null) instance = this;
    }

    // Enemies call this before they start a combo
    public bool RequestAttackToken(Transform target)
    {
        if (target == null) return false;

        // Initialize target in list if they aren't there
        if (!targetRegistry.ContainsKey(target))
            targetRegistry[target] = 0;

        // If there is an open slot, take it!
        if (targetRegistry[target] < maxAttackerSlots)
        {
            targetRegistry[target]++;
            return true;
        }

        return false; // No slots left, you must wait/strafe
    }

    // Enemies call this when their animation/combo is finished
    public void ReturnAttackToken(Transform target)
    {
        if (target != null && targetRegistry.ContainsKey(target))
        {
            targetRegistry[target] = Mathf.Max(0, targetRegistry[target] - 1);
        }
    }
}