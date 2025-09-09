using System;
using RimWorld;
using Verse;


namespace MindMatters;

public class ConstraintNeed : DynamicNeed
{
    //protected const float FallFactor = 4f;
   //private const float RiseFactor = 1f;
    
    protected override bool IsSatisfying => Find.TickManager.TicksGame < lastGainTick + 10;
        
    // IsSatisfying should allow for fall in Slippers when apparel is removed
    protected override bool IsSatisfied()
    {
        return IsSuppressed || !IsSatisfying || curLevelInt >= 0.50f;
    }
        
    public override void NeedInterval()
    {
        int currentTick = Find.TickManager.TicksGame;
        if (currentTick <= lastUpdateTick)
            return;
       
        int entryTick = lastUpdateTick;
        // MMToolkit.DebugLogVerbose($"=========={pawn.LabelShort}====={def.defName}===@==={lastUpdateTick}==========");
        // MMToolkit.DebugLogVerbose($"NeedInterval Instance ID: {GetHashCode()}, CurLevel: {CurLevel}, Baseline: {baselineSatisfaction}, Rolling: {rollingSatisfaction.Total}");
            
        
        // Core rise/fall logic
        if (!IsSatisfied())
        {
            rollingSatisfaction.Add(def.seekerRisePerHour * RiseFactor * UpdateDeltaFactor);
        }
        else
        {
            rollingSatisfaction.Add(-def.seekerFallPerHour * FallPerIntervalFactor * UpdateDeltaFactor);
            // MMToolkit.DebugLogVerbose(
            //     $"[NeedInterval]<<<<<<< Falling: {(-def.seekerFallPerHour * FallPerIntervalFactor * UpdateDeltaFactor)}, Rolling Total: {rollingSatisfaction.Total}");
        }

        // Consolidate rolling contribution and recalculate need level
        rollingSatisfaction.Consolidate();
      
        RecalculateNeedLevel();

        lastUpdateTick = Find.TickManager.TicksGame;
        // MMToolkit.DebugLogVerbose($"======= CurLevel at End: {CurLevel} ==== {entryTick} / {lastUpdateTick} ============");
    }

    // ReSharper disable once MemberCanBePrivate.Global - can be exported to API
    public DynamicNeedLevel CurCategory
    {
        get
        {
            if (CurLevel < 0.00001f)
            {
                return DynamicNeedLevel.Empty;
            }
            if (CurLevel < 0.15f)
            {
                return DynamicNeedLevel.VeryLow;
            }
            if (CurLevel < 0.3f)
            {
                return DynamicNeedLevel.Low;
            }
            if (CurLevel < 0.7f)
            {
                return DynamicNeedLevel.Satisfied;
            }
            if (CurLevel < 0.85f)
            {
                return DynamicNeedLevel.High;
            }
            return DynamicNeedLevel.Extreme;
        }
    }
    
    private float FallPerIntervalFactor => CurCategory switch
    {
        DynamicNeedLevel.Empty => 0f, 
        DynamicNeedLevel.VeryLow => 1.5f, 
        DynamicNeedLevel.Low => 1.75f, 
        DynamicNeedLevel.Satisfied => 2.5f, 
        DynamicNeedLevel.High => 3.5f, 
        DynamicNeedLevel.Extreme => 4.15f, 
        _ => throw new InvalidOperationException(), 
    };
    /*
    protected override void RecalculateNeedLevel()
    {
        // Use the base method, which now integrates rolling satisfaction
        base.RecalculateNeedLevel();

        if (MindMattersMod.settings.enableLogging)
        {
            MMToolkit.DebugLog($"[ConstraintNeed] Recalculated: CurLevel={CurLevel}, Baseline={baselineSatisfaction}, Rolling={rollingSatisfaction.Total}");
        }
    }*/

    protected override NeedDef DefaultNeedDef()
    {
        return DefDatabase<NeedDef>.GetNamedSilentFail("ConstraintNeed");
    }

    public override bool ShouldPawnHaveThisNeed(Pawn instPawn)
    {
        if (instPawn == null)
        {
            MMToolkit.GripeOnce("ConstraintNeed: Null instPawn detected in ShouldPawnHaveThisNeed.");
            return false;
        }

        return instPawn.story.traits.HasTrait(MindMattersTraitDef.Submissive) &&
               instPawn.story.traits.HasTrait(MindMattersTraitDef.Masochist) ||
               instPawn.story.traits.HasTrait(MindMattersTraitDef.Prude);
    }

    public override string GetTipString()
    {
        return $"Constraint: {(CurInstantLevelPercentage * 100f):F0}%\n"
               + "This pawn is most comfortable while wearing movement-limiting or restricting clothing.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        
        Scribe_Values.Look(ref baselineSatisfaction, "baselineSatisfaction", 0f);
    }
        
    // ReSharper disable once RedundantBaseConstructorCall - required for reflection
    public ConstraintNeed() : base()
    {
    }

    public ConstraintNeed(Pawn pawn) : base(pawn)
    {
    }

    public ConstraintNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef)
    {
    }

    protected override void UpdateValue()
    {
    }
}