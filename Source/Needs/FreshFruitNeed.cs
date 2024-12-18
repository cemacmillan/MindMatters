using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MindMatters;

public class FreshFruitNeed : DynamicNeed
{
    public FreshFruitNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef) { }

    protected override void UpdateValue()
    {
        // Update the need level based on fresh fruit presence
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
        return thing.def.IsIngestible && thing.def.label.ToLowerInvariant().Contains("strawberry");
    }

    public override string GetTipString()
    {
        // Start with the base tip string
        string tip = base.GetTipString();

        // Add fresh fruit-specific details
        
        tip += "\n\n";
               
        tip += "MMNeedFreshFruit".Translate(CurInstantLevel.ToStringPercent());
    

        return tip;
    }
}