using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public class FormalityNeed : DynamicNeed
{
    
    private float curLevel;
    
    public FormalityNeed() : base()
    {
    }

    // The reflection-friendly constructor:
    public FormalityNeed(Pawn pawn) : base(pawn)
    {
    }

    // The existing two-arg constructor:
    public FormalityNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef)
    {
    }

    // Provide a fallback def in case the reflection path uses (Pawn) only:
    protected override NeedDef DefaultNeedDef()
    {
        return DefDatabase<NeedDef>.GetNamedSilentFail("FormalityNeed");
    }
    
   
    public override void Initialize(Pawn pawn, NeedDef def)
    {
        base.Initialize(pawn, def);
    }
    
  
    public override bool ShouldPawnHaveThisNeed(Pawn pawn)
    {
        // Check if the pawn is valid and in a proper state
        if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.story?.traits == null)
        {
            MMToolkit.GripeOnce($"FormalityNeed: Invalid or incomplete pawn detected in ShouldPawnHaveThisNeed. Label: {pawn?.LabelShort ?? "Unknown"}");
            return false;
        }

        // Check if the pawn has the relevant traits
        return pawn.story.traits.HasTrait(MindMattersTraitDef.Reserved) ||
               pawn.story.traits.HasTrait(MindMattersTraitDef.Prude);
    }
    // Handles periodic updates to the need value
    protected override void UpdateValue()
    {
        if (pawn?.apparel?.WornApparel == null || !pawn.apparel.WornApparel.Any())
        {
            CurLevel = 0f; // No formal wear, need is unsatisfied
            return;
        }

        // Calculate formality satisfaction based on the beauty of worn apparel
        CurLevel = pawn.apparel.WornApparel
            .Sum(a => a.def.GetStatValueAbstract(StatDefOf.Beauty, null)) / 10f;

        // FIXTHIS generalized clamping 01 method so we don't bring in Mathf every fucking time
        CurLevel = UnityEngine.Mathf.Clamp01(CurLevel);
    }

    // Tooltip string for UI display
    public override string GetTipString()
    {
        return $"Formality: {(CurLevel * 100f):F0}%\n"
               + "This pawn gains satisfaction from wearing formal or restrictive attire.";
    }

    public override void ExposeData()
    {
        base.ExposeData(); 
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref curLevel, "curLevel", 0.5f); 
    }
}