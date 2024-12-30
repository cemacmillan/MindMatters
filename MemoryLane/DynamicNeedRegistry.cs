using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters
{
    /*
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
                category: DynamicNeedCategory.Luxury,
                bitmap: DynamicNeedsBitmap.FreshFruit,
                pawnFilter: pawn =>
                    pawn.story?.traits?.HasTrait(MindMattersTraitDef.SelfCentered) == true &&
                    pawn.story.traits.DegreeOfTrait(MindMattersTraitDef.NaturalMood) >= 1
            );

            RegisterNeed<FormalityNeed>(
                label: "Formality",
                description: "This pawn gains satisfaction from wearing formal or restrictive attire.",
                category: DynamicNeedCategory.Primal,
                bitmap: DynamicNeedsBitmap.Formality,
                pawnFilter: pawn =>
                    pawn.story?.traits?.HasTrait(MindMattersTraitDef.Submissive) == true ||
                    pawn.story?.traits.HasTrait(MindMattersTraitDef.Reserved) == true ||
                    pawn.story?.traits.HasTrait(MindMattersTraitDef.Perfectionist) == true
            );

            RegisterNeed<ConstraintNeed>(
                label: "Constraint",
                description: "This pawn feels more comfortable in restrictive environments.",
                category: DynamicNeedCategory.Quirk,
                bitmap: DynamicNeedsBitmap.Constraint,
                pawnFilter: pawn =>
                    (true == pawn.story?.traits?.HasTrait(MindMattersTraitDef.Submissive) &&
                     true == pawn.story?.traits?.HasTrait(MindMattersTraitDef.Masochist)) ||
                    true == pawn.story?.traits?.HasTrait(MindMattersTraitDef.Prude)
            );
        }
        
        public static NeedDef GetNeedDef(Type needClass)
        {
            if (Registry.TryGetValue(needClass, out var props))
            {
                var needDef = DefDatabase<NeedDef>.GetNamedSilentFail(needClass.Name);
                if (needDef != null)
                {
                    return needDef;
                }

                MindMattersUtilities.DebugWarn($"[MindMatters] NeedDef not found in DefDatabase for class: {needClass.Name}");
            }
            else
            {
                MindMattersUtilities.DebugWarn($"[MindMatters] No registered properties found for need class: {needClass.Name}");
            }

            return null;
        }
        
        public static DynamicNeedProperties GetPropertiesForBitmap(DynamicNeedsBitmap bitmap)
        {
            var needType = BitmapMap.FirstOrDefault(pair => pair.Value == bitmap).Key;
            if (needType != null && Registry.TryGetValue(needType, out var props))
            {
                return props;
            }

            MindMattersUtilities.DebugWarn($"[MindMatters] No properties found for bitmap: {bitmap}");
            return null;
        }
        
        public static DynamicNeedsBitmap GetBitmapForNeed(Type needClass)
        {
            if (BitmapMap.TryGetValue(needClass, out var bitmap))
            {
                return bitmap;
            }
    
            Log.Warning($"[MindMatters] No bitmap found for need class '{needClass.Name}'");
            return DynamicNeedsBitmap.None; // Return None as a fallback
        }

        public static void RegisterNeed<TNeed>(
            string label,
            string description,
            DynamicNeedCategory category,
            DynamicNeedsBitmap bitmap,
            Func<Pawn, bool> pawnFilter,
            int listPriority = 100 // Default listPriority
        ) where TNeed : DynamicNeed
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
                    Category = category,
                    PawnFilter = pawnFilter
                };
                BitmapMap[needType] = bitmap;

                // Also register the NeedDef in the DefDatabase (to satisfy RimWorld's GUI requirements)
                DefDatabase<NeedDef>.Add(needDef);

                Log.Message($"[MindMatters] Registered dynamic need: {label} (Priority: {listPriority})");
            }
        }
        
        public static IEnumerable<DynamicNeedProperties> GetNeedsForCategory(DynamicNeedCategory? category = null)
        {
            return Registry.Values.Where(props => !category.HasValue || props.Category == category.Value);
        }

        public static DynamicNeedProperties GetProperties(Type needType)
        {
            return Registry.TryGetValue(needType, out var props) ? props : null;
        }

        public static bool HasNeed(Type needType)
        {
            return Registry.ContainsKey(needType);
        }
    }

    public class DynamicNeedProperties
    {
        public Type NeedClass { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public DynamicNeedCategory Category { get; set; }
        public Func<Pawn, bool> PawnFilter { get; set; }
    }
    */


}