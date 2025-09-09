using System;

namespace MindMatters;

[Flags]
public enum DynamicNeedState
{
    None = 0,
    Disabled = 1 << 0,  // Globally disabled
    Available = 1 << 1, // Globally available but not currently influencing pawns
    Observed = 1 << 2,  // Observed in some pawns but not universally affecting
    Fulfilled = 1 << 3, // Globally satisfied
    Triggered = 1 << 4, // Globally triggered (e.g., spreading or causing events)
}
