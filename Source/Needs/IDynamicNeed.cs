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
    void Initialize(Pawn instPawn, NeedDef instDef);
    void ExposeData(); // Save/load functionality
    NeedDef NeedDef { get; } // Expose the NeedDef as a property
    // Indicates whether the need is temporarily suppressed
    bool IsSuppressed { get; set; }
    float MaxSatisfaction { get; set; }
    object NeedDefName { get; set; }


    // Handles events that may influence the need
    void HandleEvent(string eventName);
    
    void SatisfyDynamicNeed(Pawn instPawn, string needDefName, float amount, bool satisfyToMax = false);
   
    // Adds a baseline contribution to the need (e.g., from apparel or traits)
    void AddSatisfactionContribution(float contribution);
   
    // Removes a baseline contribution from the need
    void RemoveSatisfactionContribution(float contribution);
   
    // Updates the baseline contribution for the need based on external factors
    void UpdateNeedBaselineContribution(Pawn instPawn, string needDefName, float contribution, bool isAdding);
    void ApplyTickSatisfaction(float amount);
}