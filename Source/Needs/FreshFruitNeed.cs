using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MindMatters;

public class FreshFruitNeed : DynamicNeed
{
    private float curLevel;
    public FreshFruitNeed() : base()
    {
    }

    // The reflection-friendly constructor:
    public FreshFruitNeed(Pawn pawn) : base(pawn)
    {
    }

    // The existing two-arg constructor:
    public FreshFruitNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef)
    {
    }

    // Provide a fallback def in case the reflection path uses (Pawn) only:
    protected override NeedDef DefaultNeedDef()
    {
        return DefDatabase<NeedDef>.GetNamedSilentFail("FreshFruitNeed");
    }

    public override void Initialize(Pawn pawn, NeedDef def)
    {
        base.Initialize(pawn, def);
    }

    // The new override to control who "should" have this need:
    public override bool ShouldPawnHaveThisNeed(Pawn pawn)
    {
        if (pawn?.story?.traits == null)
        {
           // MMToolkit.GripeOnce($"[MindMatters] Pawn {pawn?.LabelShort} has no traits. FreshFruitNeed => false");
            return false;
        }

        bool hasSelfCentered = pawn.story.traits.HasTrait(MindMattersTraitDef.SelfCentered);
        int moodDegree = pawn.story.traits.DegreeOfTrait(MindMattersTraitDef.NaturalMood);

        if (hasSelfCentered && moodDegree >= 1)
        {
            //MindMattersUtilities.DebugLog($"[MindMatters] Pawn {pawn.LabelShort} meets FreshFruitNeed trait criteria => true");
            return true;
        }

        // MindMattersUtilities.DebugLog($"[MindMatters] Pawn {pawn.LabelShort} does NOT meet FreshFruitNeed trait criteria => false");
        return false;
    }

    // Core loop for adjusting the need each interval:
    protected override void UpdateValue()
    {
        // Adjust need level based on fresh fruit presence
        if (pawn.inventory?.innerContainer.Any(IsFreshFruit) == true)
        {
            CurLevel += 0.1f;
        }
        else if (pawn.needs?.mood?.thoughts?.memories?.Memories
                     .Any(m => m.def.defName == "AteFreshFruit") == true)
        {
            CurLevel += 0.05f;
        }
        else
        {
            CurLevel -= 0.01f;
        }

        // Clamp between 0 and 1
        CurLevel = Mathf.Clamp01(CurLevel);
        curLevel = CurLevel;
    }

    private bool IsFreshFruit(Thing thing)
    {
        return thing.def.IsIngestible && thing.def.label.ToLowerInvariant().Contains("strawberry");
    }

    // Additional text in the hover tooltip
    public override string GetTipString()
    {
        string tip = base.GetTipString();
        tip += "\n\n" + "MMNeedFreshFruit".Translate(CurInstantLevel.ToStringPercent());
        return tip;
    }
    
    public override void ExposeData()
    {
        base.ExposeData(); 
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref curLevel, "curLevel", 0.5f); 
    }
}