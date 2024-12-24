using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public class FormalityNeed : DynamicNeed
{
    // Parameterless constructor for serialization
    public FormalityNeed() : base() 
    {
    }
    
    [JetBrains.Annotations.UsedImplicitlyAttribute]
    public override void Initialize(Pawn pawn, NeedDef def)
    {
        base.Initialize(pawn, def);
    }
    
    public FormalityNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef) 
    {
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

    // Serialize/deserialize custom fields (if needed)
    public override void ExposeData()
    {
        base.ExposeData();
        // Add any custom fields here, if necessary
    }
}