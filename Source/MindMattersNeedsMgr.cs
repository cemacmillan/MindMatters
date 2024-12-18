using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public class MindMattersNeedsMgr : IExposable
{
    public event Action<Pawn> OnPawnMoodChanged;

    private Dictionary<Pawn, int> BipolarPawnLastCheckedTicks = new();
    private Dictionary<Pawn, int> PawnLastAloneTicks = new();
    private Dictionary<Pawn, int> UnstablePawnLastMentalBreakTicks = new();
    private Dictionary<Pawn, int> UnstablePawnLastMoodSwitchTicks = new();

    private Dictionary<Pawn, DynamicNeedsBitmap> pawnNeedsMap = new();


    public void Tick(int currentTick)
    {
        // Add tick-based logic for needs here if necessary
    }
    
    public static void AddDynamicNeed<TNeed>(Pawn pawn) where TNeed : DynamicNeed
    {
        // Get the NeedDef corresponding to the dynamic need
        var needDef = DefDatabase<NeedDef>.AllDefs.FirstOrDefault(def => def.needClass == typeof(TNeed));
        if (needDef == null)
        {
            Log.Error($"[MindMatters] No NeedDef found for {typeof(TNeed)}.");
            return;
        }

        // Avoid duplicates
        if (pawn.needs.TryGetNeed(needDef) != null)
        {
            // MindMattersUtilities.DebugLog($"[MindMatters] {pawn.LabelShort} already has {needDef.defName}.");
            return;
        }

        // Create and add the need instance
        var newNeed = (TNeed)Activator.CreateInstance(typeof(TNeed), pawn);
        pawn.needs.AllNeeds.Add(newNeed);

        MindMattersUtilities.DebugLog($"[MindMatters] Added {needDef.defName} to {pawn.LabelShort}.");
    }

    public void MarkPawnAlone(Pawn pawn)
    {
        PawnLastAloneTicks[pawn] = Find.TickManager.TicksGame;
    }

    public bool HasMentalBreakRecently(Pawn pawn)
    {
        return UnstablePawnLastMentalBreakTicks.TryGetValue(pawn, out var lastBreakTick) &&
               Find.TickManager.TicksGame - lastBreakTick < 60000;
    }

    public void MarkMentalBreak(Pawn pawn)
    {
        UnstablePawnLastMentalBreakTicks[pawn] = Find.TickManager.TicksGame;
    }

    public void UpdateBipolarPawnTicks(Pawn pawn)
    {
        BipolarPawnLastCheckedTicks[pawn] = Find.TickManager.TicksGame;
    }

    public void AddNeed(Pawn pawn, DynamicNeedsBitmap need)
    {
        if (!pawnNeedsMap.ContainsKey(pawn))
        {
            pawnNeedsMap[pawn] = DynamicNeedsBitmap.None;
        }
        pawnNeedsMap[pawn] = DynamicNeedHelper.AddNeed(pawnNeedsMap[pawn], need);
    }

    public void RemoveNeed(Pawn pawn, DynamicNeedsBitmap need)
    {
        if (pawnNeedsMap.TryGetValue(pawn, out var needs))
        {
            pawnNeedsMap[pawn] = DynamicNeedHelper.RemoveNeed(needs, need);
        }
    }

    public bool HasNeed(Pawn pawn, DynamicNeedsBitmap need)
    {
        return pawnNeedsMap.TryGetValue(pawn, out var needs) &&
               DynamicNeedHelper.IsNeedActive(needs, need);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref BipolarPawnLastCheckedTicks, "BipolarPawnLastCheckedTicks", LookMode.Reference, LookMode.Value);
        Scribe_Collections.Look(ref PawnLastAloneTicks, "PawnLastAloneTicks", LookMode.Reference, LookMode.Value);
        Scribe_Collections.Look(ref UnstablePawnLastMentalBreakTicks, "UnstablePawnLastMentalBreakTicks", LookMode.Reference, LookMode.Value);
        Scribe_Collections.Look(ref UnstablePawnLastMoodSwitchTicks, "UnstablePawnLastMoodSwitchTicks", LookMode.Reference, LookMode.Value);
        Scribe_Collections.Look(ref pawnNeedsMap, "pawnNeedsMap", LookMode.Reference, LookMode.Value);
    }
}