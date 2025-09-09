using System;

namespace MindMatters;

public enum ExperienceValency
{
    Positive,       // Explicitly good events (compliments, achievements, gifts)
    Negative,       // Explicitly bad events (insults, failures, trauma)
    Neutral,        // Neutral events (routine work, non-controversial ceremonies)
    Eldritch,       // Mysterious, unsettling, or reality-altering events
    Affirming,      // Experiences reinforcing norms, roles, or ideoligions
    Humiliating,    // Events that degrade dignity, self-worth, or social standing
    Exhilarating,   // Thrilling, adrenaline-pumping experiences (victories, near misses)
    Transformative  // Events that fundamentally change the pawn's self-concept
}
