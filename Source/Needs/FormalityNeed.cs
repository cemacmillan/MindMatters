using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public class FormalityNeed : DynamicNeed
{
    
    private float curLevel;
    private float baselineFormalityFromApparel = 0f;
    private const int checkApparelInterval = 20;
    private int checkApparelIntervalCounter;
    
    
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
        //this.checkApparelIntervalCounter = checkApparelIntervalCounter;
    }

    // Provide a fallback def in case the reflection path uses (Pawn) only:
    protected override NeedDef DefaultNeedDef()
    {
        return DefDatabase<NeedDef>.GetNamedSilentFail("FormalityNeed");
    }
    
   
    public override void Initialize(Pawn instPawn, NeedDef instDef)
    {
        base.Initialize(instPawn, instDef);
    }
    
    protected override void UpdateValue()
    {
        // Periodically update the baseline
        if (checkApparelIntervalCounter % checkApparelInterval == 0)
        {
            UpdateBaselineFormality();
        }
        checkApparelIntervalCounter = (checkApparelIntervalCounter + 1) % checkApparelInterval;

        // If no apparel is worn, reset the need
        if (baselineFormalityFromApparel == 0f)
        {
            CurLevel = 0f;
            return;
        }

        // Calculate the new level by combining the baseline and any external contributions
        float externalContributions = CurLevel - baselineFormalityFromApparel;
        CurLevel = baselineFormalityFromApparel + externalContributions;

        // Clamp the value between 0 and 1
        CurLevel = UnityEngine.Mathf.Clamp01(CurLevel);
    }
    
    private void UpdateBaselineFormality()
    {
        if (pawn.apparel?.WornApparel == null || !pawn.apparel.WornApparel.Any())
        {
            baselineFormalityFromApparel = 0f;
            return;
        }

        float totalBeauty = 0f;

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            if (apparel?.Stuff == null) continue; 
            // Get the beauty of the apparel, factoring in the Stuff it is made from
            float apparelBeauty = apparel.def.GetStatValueAbstract(StatDefOf.Beauty, apparel.Stuff);

            // Accumulate the total beauty
            totalBeauty += apparelBeauty;
        }

        // Divide by 10 as in the original logic to normalize
        baselineFormalityFromApparel = totalBeauty / 10f;
    }
  
    public override bool ShouldPawnHaveThisNeed(Pawn instPawn)
    {
        if (instPawn == null || instPawn.Dead || instPawn.Destroyed || instPawn.story?.traits == null)
        {
            MMToolkit.GripeOnce($"FormalityNeed: Invalid or incomplete instPawn detected in ShouldPawnHaveThisNeed. Label: {instPawn?.LabelShort ?? "Unknown"}");
            return false;
        }
        
        return instPawn.story.traits.HasTrait(MindMattersTraitDef.Reserved) ||
               instPawn.story.traits.HasTrait(MindMattersTraitDef.Prude);
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