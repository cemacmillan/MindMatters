using RimWorld;
using Verse;

namespace MindMatters;

public interface IDynamicNeed
{
    Pawn Pawn { get; } // The pawn this need is tied to
    float CurLevel { get; set; } // Current level of the need
    bool ShowOnNeedList { get; } // Determines if it's visible in the Need tab
    void NeedInterval(); // Called periodically for updates
    string GetTipString(); // Tooltip for the need in the UI
    void Initialize(Pawn pawn, NeedDef def);
    void ExposeData(); // Save/load functionality
    NeedDef NeedDef { get; } // Expose the NeedDef as a property
}