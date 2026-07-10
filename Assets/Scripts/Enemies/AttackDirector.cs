using UnityEngine;
using System.Collections.Generic;

public class AttackDirector : MonoBehaviour
{
    public static AttackDirector instance;

    [Header("Settings")]
    [Tooltip("How many enemies can attack a single target at once")]
    public int maxAttackerSlots = 3;

    // 🔥 FIXED UNIVERSAL REGISTRY: Tracks the target Transform -> against a HashSet of the root MusouUnits
    private Dictionary<Transform, HashSet<MusouUnit>> targetRegistry = new Dictionary<Transform, HashSet<MusouUnit>>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 🔥 CHANGED PARAMETER TYPE to 'Component'. This allows 'this' from MeleeEnemy, 
    /// SleepEnemy, SquadLeader, or MusouUnit to pass through completely error-free!
    /// </summary>
    public bool RequestAttackToken(Component attackerComponent, Transform target)
    {
        if (attackerComponent == null || target == null) return false;

        // Automatically resolve and cast down to the valid MusouUnit base component internally
        MusouUnit attacker = attackerComponent.GetComponent<MusouUnit>();
        if (attacker == null) return false; // Safety fallback if script type doesn't contain a MusouUnit component

        PruneDeadRegistryEntries();

        if (!targetRegistry.ContainsKey(target))
        {
            targetRegistry[target] = new HashSet<MusouUnit>();
        }

        if (targetRegistry[target].Contains(attacker)) return true;

        if (targetRegistry[target].Count < maxAttackerSlots)
        {
            targetRegistry[target].Add(attacker);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 🔥 CHANGED PARAMETER TYPE to 'Component' for seamless universal unlinking
    /// </summary>
    public void ReturnAttackToken(Component attackerComponent, Transform target)
    {
        if (attackerComponent == null || target == null) return;

        MusouUnit attacker = attackerComponent.GetComponent<MusouUnit>();
        if (attacker == null) return;

        if (targetRegistry.ContainsKey(target))
        {
            targetRegistry[target].Remove(attacker);

            if (targetRegistry[target].Count == 0)
            {
                targetRegistry.Remove(target);
            }
        }
    }

    /// <summary>
    /// 🔥 CHANGED PARAMETER TYPE to 'Component' for absolute OnDisable crash prevention
    /// </summary>
    public void ForceReleaseAllTokensForAttacker(Component attackerComponent)
    {
        if (attackerComponent == null) return;

        MusouUnit attacker = attackerComponent.GetComponent<MusouUnit>();
        if (attacker == null) return;

        List<Transform> keysToClean = new List<Transform>();

        foreach (var pair in targetRegistry)
        {
            if (pair.Value.Contains(attacker))
            {
                pair.Value.Remove(attacker);
                if (pair.Value.Count == 0) keysToClean.Add(pair.Key);
            }
        }

        foreach (Transform key in keysToClean)
        {
            targetRegistry.Remove(key);
        }
    }

    private void PruneDeadRegistryEntries()
    {
        List<Transform> deadKeys = new List<Transform>();

        foreach (var key in targetRegistry.Keys)
        {
            if (key == null)
            {
                deadKeys.Add(key);
            }
        }

        foreach (Transform deadKey in deadKeys)
        {
            targetRegistry.Remove(deadKey);
        }
    }
}