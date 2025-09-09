using System;

namespace MindMatters;

[Flags]
public enum PawnNeedStatus
{
    None = 0,          // No association with the need
    Passive = 1 << 0,  // Known but inactive
    Active = 1 << 1,   // Active but not dominating
    Fulminant = 1 << 2, // Dominating behavior
    Deprecated = 1 << 3, // Was active but no longer relevant
    Dormant = 1 << 4,  // Temporarily inactive
    Transient = 1 << 5, // Temporary condition, may decay naturally
    Permanent = 1 << 6  // Persistent until explicitly removed
}