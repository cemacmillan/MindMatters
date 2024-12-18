using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public static class DynamicNeedRegistry
{
    private static readonly Dictionary<Type, DynamicNeedProperties> Registry = new();
    private static readonly Dictionary<Type, DynamicNeedsBitmap> BitmapMap = new();
    
    static DynamicNeedRegistry()
    {
        // Register Needs
        RegisterNeed<FreshFruitNeed>(
            label: "Fresh Fruit",
            description: "This pawn craves fresh fruit to stay happy.",
            bitmap: DynamicNeedsBitmap.FreshFruit,
            pawnFilter: pawn =>
                pawn.story?.traits?.HasTrait(MindMattersTraits.SelfCentered) == true &&
                pawn.story.traits.DegreeOfTrait(MindMattersTraits.NaturalMood) >= 1
        );

        RegisterNeed<FormalityNeed>(
            label: "Formality",
            description: "This pawn gains satisfaction from wearing formal or restrictive attire.",
            bitmap: DynamicNeedsBitmap.Formality,
            pawnFilter: pawn =>
                pawn.story?.traits?.HasTrait(MindMattersTraits.Submissive) == true ||
                pawn.story?.traits.HasTrait(MindMattersTraits.Reserved) == true ||
                pawn.story?.traits.HasTrait(MindMattersTraits.Perfectionist) == true
               
        );

        RegisterNeed<ConstraintNeed>(
            label: "Constraint",
            description: "This pawn feels more comfortable in restrictive environments.",
            bitmap: DynamicNeedsBitmap.Constraint,
            pawnFilter: pawn =>
                (true == pawn.story?.traits?.HasTrait(MindMattersTraits.Submissive) &&
                 true == pawn.story?.traits?.HasTrait(MindMattersTraits.Masochist)) ||
                true == pawn.story?.traits?.HasTrait(MindMattersTraits.Prude)
        );
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void RegisterNeed<TNeed>(
        string label, 
        string description, 
        DynamicNeedsBitmap bitmap, 
        Func<Pawn, bool> pawnFilter, 
        int listPriority = 100) // Default listPriority
        where TNeed : DynamicNeed
    {
        var needType = typeof(TNeed);
        if (!Registry.ContainsKey(needType))
        {
            // Create and register NeedDef
            var needDef = new NeedDef
            {
                defName = needType.Name, // Ensure unique defName
                label = label,
                description = description,
                needClass = needType, // Link the need class
                colonistAndPrisonersOnly = true,
                listPriority = listPriority
            };

            // Register the properties
            Registry[needType] = new DynamicNeedProperties
            {
                NeedClass = needType,
                Label = label,
                Description = description,
                PawnFilter = pawnFilter
            };
            BitmapMap[needType] = bitmap;

            // Also register the NeedDef in the DefDatabase (to satisfy RimWorld's GUI requirements)
            DefDatabase<NeedDef>.Add(needDef);

            Log.Message($"[MindMatters] Registered dynamic need: {label} (Priority: {listPriority})");
        }
    }
    
    public static void TryAddNeed<TNeed>(Pawn pawn) where TNeed : DynamicNeed
    {
        // Ensure TNeed is associated with a valid NeedDef
        NeedDef needDef = DynamicNeedPropertiesRegistry.GetPropertiesFor(typeof(TNeed));
        if (needDef == null)
        {
            Log.Error($"[MindMatters] No NeedDef found for '{typeof(TNeed).Name}'.");
            return;
        }

        // Check for duplicates using the shared NeedDef
        if (pawn.needs.TryGetNeed(needDef) != null)
        {
            MindMattersUtilities.DebugLog($"[MindMatters] {pawn.LabelShort} already has the need '{needDef.label}'.");
            return;
        }

        // Validate pawn eligibility for the need
        DynamicNeedProperties properties = GetProperties(typeof(TNeed));
        if (properties == null || !properties.PawnFilter(pawn))
        {
            MindMattersUtilities.DebugLog($"[MindMatters] Pawn '{pawn.LabelShort}' does not qualify for '{needDef.label}'.");
            return;
        }

        // Explicitly create and cast the new need with both arguments (pawn, needDef)
        DynamicNeed newNeed = (DynamicNeed)Activator.CreateInstance(typeof(TNeed), pawn, needDef);
        if (newNeed == null)
        {
            Log.Error($"[MindMatters] Failed to create instance of '{typeof(TNeed).Name}' for {pawn.LabelShort}.");
            return;
        }

        // Add the need to the pawn
        pawn.needs.AllNeeds.Add(newNeed);
        MindMattersUtilities.DebugLog($"[MindMatters] Added need '{needDef.label}' to {pawn.LabelShort}.");
    }
    
    public static IEnumerable<DynamicNeedsBitmap> GetActiveBitmaps(DynamicNeedsBitmap bitmap)
    {
        return BitmapMap.Values.Where(b => DynamicNeedHelper.IsNeedActive(bitmap, b));
    }

    public static DynamicNeedProperties GetProperties(Type needType) =>
        Registry.TryGetValue(needType, out var props) ? props : null;
}

public class DynamicNeedProperties
{
    public Type NeedClass { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public Func<Pawn, bool> PawnFilter { get; set; }
}