using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public class ConstraintNeed : DynamicNeed
{
    private float curLevel;
    public ConstraintNeed() : base()
    {
    }

    // The reflection-friendly constructor:
    public ConstraintNeed(Pawn pawn) : base(pawn)
    {
    }

    // The existing two-arg constructor:
    public ConstraintNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef)
    {
    }

    // Provide a fallback def in case the reflection path uses (Pawn) only:
    protected override NeedDef DefaultNeedDef()
    {
        return DefDatabase<NeedDef>.GetNamedSilentFail("ConstraintNeed");
    }
    
    public override void Initialize(Pawn pawn, NeedDef def)
    {
        base.Initialize(pawn, def);
    }



    public override bool ShouldPawnHaveThisNeed(Pawn pawn)
    {
        if (pawn == null)
        {
            MMToolkit.GripeOnce("ConstraintNeed: Null pawn detected in ShouldPawnHaveThisNeed.");
            return false;
        }
            
        return pawn.story.traits.HasTrait(MindMattersTraitDef.Submissive) == true &&
               pawn.story.traits.HasTrait(MindMattersTraitDef.Masochist) == true ||
               pawn.story.traits.HasTrait(MindMattersTraitDef.Prude) == true;
    }

    
    protected override void UpdateValue()
    {
        // Example: Penalize need if wearing "restrictive" gear
        var apparel = pawn.apparel?.WornApparel;
        if (apparel != null)
        {
            CurLevel = 1f - apparel.Sum(a => a.def.GetStatValueAbstract(StatDefOf.Comfort, null)) / 10f;
        }
    }
    public override string GetTipString()
    {
        return $"Constraint: {(CurLevel * 100f):F0}%\n"
               + "This pawn becomes unhappy when wearing restrictive clothing.";
    }
    
    public override void ExposeData()
    {
        base.ExposeData(); 
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref curLevel, "curLevel", 0.5f); 
    }
}