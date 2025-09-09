using Verse;

namespace MindMatters;

/// <summary>
/// Represents a rolling accumulator that tracks a transient value (Current) and a persistent accumulated value.
/// Designed for periodic consolidation.
/// </summary>
public class DynamicAccumulator
{
    private float _accumulated; // Backing field for Accumulated
    private float _current;     // Backing field for Current

    /// <summary>
    /// Persistent contribution.
    /// </summary>
    public float Accumulated
    {
        get => _accumulated;
        private set => _accumulated = value;
    }

    /// <summary>
    /// Temporary contribution (rolling).
    /// </summary>
    public float Current
    {
        get => _current;
        private set => _current = value;
    }

    /// <summary>
    /// Total combined value (Accumulated + Current).
    /// </summary>
    public float Total => Accumulated + Current;

    /// <summary>
    /// Adds to the current (rolling) value.
    /// </summary>
    public void Add(float amount)
    {
        Current += amount;
    }

    /// <summary>
    /// Adjusts the accumulator to match a target value by recalculating Current.
    /// </summary>
    public void Adjust(float target)
    {
        Current += target - Total;
    }

    /// <summary>
    /// Consolidates Current into Accumulated, resetting Current to zero.
    /// </summary>
    public void Consolidate()
    {
        Accumulated += Current;
        //MMToolkit.DebugLog($"[DynamicAccumulator] Instance ID: {GetHashCode()}, Total: {Total}, Accumulated: {Accumulated}, Current: {Current}");
        Current = 0f;
    }

    /// <summary>
    /// Resets both Current and Accumulated.
    /// </summary>
    public void Reset()
    {
        Accumulated = 0f;
        Current = 0f;
    }

    /// <summary>
    /// Handles serialization for saving/loading.
    /// </summary>
    public void ExposeData(string keyPrefix)
    {
        Scribe_Values.Look(ref _accumulated, $"{keyPrefix}_Accumulated", 0f);
        Scribe_Values.Look(ref _current, $"{keyPrefix}_Current", 0f);
    }
}