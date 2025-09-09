using System;

// MemberCanBePrivate disabled here because this class is passed around like a... never mind.
// ReSharper disable MemberCanBePrivate.Global

namespace MindMatters;


public class NeedStatus
{
    public PawnNeedStatus PawnStatus { get; set; }
    public DynamicNeedState GlobalState { get; set; }
    public float? CurrentLevel { get; set; }
    public string DiagnosticMessage { get; set; }

    public bool IsGloballyObserved => GlobalState.HasFlag(DynamicNeedState.Observed);
    public bool IsPawnNeedDormant => PawnStatus.HasFlag(PawnNeedStatus.Dormant);
    public bool IsPawnNeedActive => PawnStatus.HasFlag(PawnNeedStatus.Active) || PawnStatus.HasFlag(PawnNeedStatus.Fulminant);
    public bool IsPawnNeedHistoricallyPresent => IsPawnNeedDormant || IsPawnNeedActive || PawnStatus.HasFlag(PawnNeedStatus.Deprecated);

    public NeedStatus(PawnNeedStatus pawnStatus, DynamicNeedState globalState, float? currentLevel = null, string message = null)
    {
        PawnStatus = pawnStatus;
        GlobalState = globalState;
        CurrentLevel = currentLevel;
        DiagnosticMessage = message;
    }

    public override string ToString()
    {
        return $"PawnStatus: {PawnStatus}, GlobalState: {GlobalState}, CurrentLevel: {CurrentLevel?.ToString("F2")}, Message: {DiagnosticMessage}";
    }
}