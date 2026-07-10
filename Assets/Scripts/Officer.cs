using UnityEngine;

// 🔥 HIERARCHICAL COUPLING: Inherits the child object scanning 
// and waypoint pathing queue loops completely automatically!
public class Officer : SquadLeader
{
    /// <summary>
    /// 🔥 THE MASTER COMMANDER OVERRIDE:
    /// Because a supreme strategist manages macro-level waypoints,
    /// bypass the localized grunt assembly loops so pathing stays fluid!
    /// </summary>
    protected override bool IsEntireSquadAssembledAndIdle()
    {
        // Instantly returns true, letting the commander advance nodes independently
        return true;
    }
}