using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MindMatters;

public class FreshFruitNeed : DynamicNeed
{
    public override DynamicNeedCategory Category => DynamicNeedCategory.Luxury; // Override the category if needed

    public FreshFruitNeed() : base()
    {
        
    }

    public FreshFruitNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef)
    {
        
    }

    // Use DefaultNeedDef for fallback NeedDef assignment during initialization
    protected override NeedDef DefaultNeedDef()
    {
        return DefDatabase<NeedDef>.GetNamed("FreshFruitNeed", errorOnFail: true); // Replace with the correct NeedDef name
    }

    protected override void UpdateValue()
    {
        // Update need level based on fresh fruit presence
        if (pawn.inventory != null && pawn.inventory.innerContainer.Any(IsFreshFruit))
        {
            CurLevel += 0.1f;
        }
        else if (pawn.needs.mood?.thoughts?.memories?.Memories
                     .Any(m => m.def.defName == "AteFreshFruit") == true)
        {
            CurLevel += 0.05f;
        }
        else
        {
            CurLevel -= 0.01f;
        }

        // Clamp CurLevel between 0 and 1
        CurLevel = Mathf.Clamp01(CurLevel);
    }

    private bool IsFreshFruit(Thing thing)
    {
        // Example logic: Replace "strawberry" with actual fruit labels or categories
        return thing.def.IsIngestible && thing.def.label.ToLowerInvariant().Contains("strawberry");
    }

    public override string GetTipString()
    {
        // Start with the base tip string
        string tip = base.GetTipString();

        // Add fresh fruit-specific details
        tip += "\n\n";
        tip += "MMNeedFreshFruit".Translate(CurInstantLevel.ToStringPercent()); // Ensure translation key exists

        return tip;
    }
}