using RimWorld;
using Verse;
//using System.Collections.Generic;
using UnityEngine;

namespace MindMatters;


public abstract class DynamicNeed : Need, IDynamicNeed
{
    public virtual DynamicNeedCategory Category => DynamicNeedCategory.Secondary;

    // private float value;
    // private const int DefaultUpdateFrequency = 150; // Match Pawn_NeedsTracker
    //protected virtual int UpdateFrequency => DefaultUpdateFrequency;
    // Adjust UpdateDeltaFactor to frequency
    //protected virtual float UpdateDeltaFactor => UpdateFrequency / 2500f;

    private const float TicksPerHour = 2500f;
    protected const float FallFactor = 4f;
    protected const float RiseFactor = 1f;
    protected const float UpdateDeltaFactor = FallFactor / TicksPerHour;
    
    protected readonly DynamicAccumulator rollingSatisfaction = new();
    protected float baselineSatisfaction;
    // protected float curLevelInternal;
    protected int lastGainTick = -999;
    protected int lastUpdateTick = -999;

    protected virtual bool IsSatisfying => Find.TickManager.TicksGame < lastGainTick + 10;


    public override float CurInstantLevel => Mathf.Clamp(baselineSatisfaction + rollingSatisfaction.Total, 0f, MaxLevel);
    


    public virtual void SatisfyDynamicNeed(Pawn instPawn, string needDefName, float amount, bool satisfyToMax = false)
    {
        if (satisfyToMax)
        {
            ApplyTickSatisfaction(MaxSatisfaction - CurLevel);
        }
        else
        {
            ApplyTickSatisfaction(amount);
        }

        lastGainTick = Find.TickManager.TicksGame;
    }

    // applies quasi-static satisfaction to Need for Notify_Equip, etc. as implemented in derived classes
    public virtual void UpdateNeedBaselineContribution(Pawn instPawn, string needDefName, float contribution, bool isAdding)
    {
        MMToolkit.GripeOnce(
            $"UpdateNeedBaselineContribution({needDefName}, {contribution}, {isAdding}) has been called... by someone.");
    }

    /* quickly evolving methods */
    protected virtual bool IsSatisfied()
    {
        // Consider the need satisfied if it is suppressed or the level exceeds a threshold.
        // Override this method to introduce 
        return !IsSatisfying || IsSuppressed || curLevelInt >= 0.66f;
    }

    public virtual void AddSatisfactionContribution(float contribution)
    {
        baselineSatisfaction += contribution; 
        // RecalculateNeedLevel();
    }

    public virtual void RemoveSatisfactionContribution(float contribution)
    {
        baselineSatisfaction -= contribution;
        if (baselineSatisfaction < 0f) baselineSatisfaction = 0f;
        // RecalculateNeedLevel();
    }

    public virtual void ApplyTickSatisfaction(float amount)
    {
        // math it like the game does
        amount = Mathf.Min(amount, 1f - CurLevel);
        // joy sets here.... why not
        // curLevelInt += amount;
        rollingSatisfaction.Add(amount);
        if (CurLevel > MaxSatisfaction)
            rollingSatisfaction.Adjust(MaxSatisfaction);
        rollingSatisfaction.Consolidate();
        // RecalculateNeedLevel();
    }

    public override float CurLevel
    {
        get => curLevelInt;
        set
        {
            float delta = value - curLevelInt;
            rollingSatisfaction.Adjust(delta);
            curLevelInt = Mathf.Clamp(value,0f, MaxLevel);
        }
    }
// RCNL
    protected virtual void RecalculateNeedLevel()
    {
        //if (lastUpdateTick == Find.TickManager.TicksGame) return;

        curLevelInt = Mathf.Clamp(baselineSatisfaction + rollingSatisfaction.Total, 0f, MaxLevel);

        // MMToolkit.DebugLog($"[DynamicNeed] RecalculateNeedLevel called. CurLevel: {CurLevel}, Baseline: {baselineSatisfaction}, Rolling: {rollingSatisfaction.Total}");
      
    }
    
    public override void NeedInterval()
    {
        int currentTick = Find.TickManager.TicksGame;
        if (currentTick <= lastUpdateTick)
            return;
       
        int entryTick = lastUpdateTick;
        MMToolkit.DebugLog($"=========={pawn.LabelShort}====={def.defName}===@==={lastUpdateTick}==========");
        MMToolkit.DebugLog($"NeedInterval Instance ID: {GetHashCode()}, CurLevel: {CurLevel}, Baseline: {baselineSatisfaction}, Rolling: {rollingSatisfaction.Total}");
            
        
            // Core rise/fall logic
            if (!IsSatisfied())
            {
                rollingSatisfaction.Add(def.seekerRisePerHour * UpdateDeltaFactor);
            }
            else
            {
                rollingSatisfaction.Add(-def.seekerFallPerHour * UpdateDeltaFactor);
                MMToolkit.DebugLog(
                    $"[NeedInterval] Fall Added: {(-def.seekerFallPerHour * UpdateDeltaFactor)}, Rolling Total: {rollingSatisfaction.Total}");
            }

        // Consolidate rolling contribution and recalculate need level
        rollingSatisfaction.Consolidate();
      
        RecalculateNeedLevel();
        //curLevelInt = CurInstantLevel;

        // Debug logging
        /*if (MindMattersMod.settings.enableLogging)
        {
            MMToolkit.DebugLog(
                $"[DynamicNeed::NeedInterval] CurLevel: {CurLevel}, Baseline: {baselineSatisfaction}, Rolling: {rollingSatisfaction.Total}");
        }*/
        // CurLevel = Mathf.Clamp(baselineSatisfaction + rollingSatisfaction.Total, 0f, MaxLevel);
        lastUpdateTick = Find.TickManager.TicksGame;
        MMToolkit.DebugLog($"======= CurLevel at End: {CurLevel} ==== {entryTick} / {lastUpdateTick} ============");
    }
    
        
    public override bool ShowOnNeedList => true;
    
    public virtual float BaselineSatisfaction
    {
        get => baselineSatisfaction;
        set => baselineSatisfaction = Mathf.Clamp01(value);
    }


    protected abstract void UpdateValue();

    public NeedDef NeedDef
    {
        get => def;
        private set
        {
            if (value == null)
            {
                MMToolkit.GripeOnce("[DynamicNeed::NeedDef] Attempted to assign a null NeedDef. Skipping assignment.");
                return; // Soft handling; do not crash
            }

            
        }
    }

    public Pawn Pawn { get; } = null!;
    public override string GetTipString()
    {
        return $"{def?.label ?? "Undefined Need"}: {CurInstantLevelPercentage.ToString("F0")}%";
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref curLevelInt, "curLevelInt", 0.5f);
        Scribe_Values.Look(ref baselineSatisfaction, "baselineSatisfaction", 0f);

        // Serialize rolling satisfaction
        rollingSatisfaction.ExposeData("rollingSatisfaction");

        // Recalculate after loading to ensure consistency
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            RecalculateNeedLevel();
        }
    }
    
    // ReSharper disable once RedundantBaseConstructorCall - not redundant as required by Reflection
    protected DynamicNeed() : base(null)
    {
    }

    protected DynamicNeed(Pawn pawn) : base(pawn)
    {
        // def = NeedDef;
        InitializeDefaults();
    }

    protected DynamicNeed(Pawn pawn, NeedDef needDef) : base(pawn)
    {
        
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        this.threshPercents = new() { 0.25f, 0.5f, 0.75f };
        
    }

    // Subclasses typically override this:
    protected abstract NeedDef DefaultNeedDef();

    // Overridable init method
    public virtual void Initialize(Pawn instPawn, NeedDef instDef)
    {
        
        // def is set by RimWorld reflection system
    }

    public virtual bool IsSuppressed { get; set; }

    public virtual float MaxSatisfaction { get; set; } = 1f;

    public object NeedDefName
    {
        get
        {
            if (def == null)
            {
                MMToolkit.GripeOnce(
                    $"[DynamicNeed] Attempted to access NeedDefName, but the NeedDef is null for {GetType().Name}. Defaulting to 'UndefinedNeed'.");
                return "UndefinedNeed"; // Safe fallback
            }

            return def.defName;
        }
        set
        {
            if (value is string defName)
            {
                
                if (def == null)
                {
                    MMToolkit.GripeOnce(
                        $"[DynamicNeed] Unable to assign NeedDefName: '{defName}' not found in DefDatabase for {GetType().Name}.");
                }
            }
            else
            {
                MMToolkit.GripeOnce(
                    $"[DynamicNeed] Invalid type assigned to NeedDefName: expected string, got {value?.GetType().Name ?? "null"}.");
            }
        }
    }

    // callback for reactivity within Mind Matters
    public virtual void HandleEvent(string eventName)
    {
    }

    /* Determines if a Pawn qualifies for this Need - rename before release!!!
     See ConstraintNeed.cs for an example of a safe derived class implementation.
    */
    public virtual bool ShouldPawnHaveThisNeed(Pawn instPawn)
    {
        return true;
    }

}
