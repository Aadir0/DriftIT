using System;
using UnityEngine;

/// <summary>
/// Central persistent tracker for player stats displayed on completing the game's Easters.
/// </summary>
public static class EasterStatsTracker
{
    private static int _sheepHitCount = 0;
    private static int _driftCount = 0;

    public static int SheepHitCount => _sheepHitCount;
    public static int DriftCount => _driftCount;

    public static event Action<int> OnSheepHitChanged;
    public static event Action<int> OnDriftChanged;

    public static void RegisterSheepHit()
    {
        _sheepHitCount++;
        Debug.Log($"[EasterStatsTracker] Sheep Hit! Total: {_sheepHitCount}");
        OnSheepHitChanged?.Invoke(_sheepHitCount);
    }

    public static void RegisterDrift()
    {
        _driftCount++;
        Debug.Log($"[EasterStatsTracker] Drift recorded! Total: {_driftCount}");
        OnDriftChanged?.Invoke(_driftCount);
    }

    public static void Reset()
    {
        _sheepHitCount = 0;
        _driftCount = 0;
        Debug.Log("[EasterStatsTracker] Stats reset for new run.");
    }
}
