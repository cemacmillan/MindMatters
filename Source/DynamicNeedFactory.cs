using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MindMatters
{
    public static class DynamicNeedFactory
    {
        /// <summary>
        /// Creates a single instance of a dynamic need for a pawn.
        /// Delegates metadata lookups to the registry.
        /// </summary>
        public static DynamicNeed CreateNeedInstance(Type needType, Pawn pawn)
        {
            if (!DynamicNeedsRegistry.HasNeed(needType))
                return null;

            try
            {
                var needDef = DynamicNeedsRegistry.GetNeedDef(needType);
                if (needDef == null)
                    return null;

                if (Activator.CreateInstance(needType) is DynamicNeed need)
                {
                    need.Initialize(pawn, needDef);
                    return need;
                }
            }
            catch (Exception ex)
            {
                MindMattersUtilities.GripeOnce(
                    $"[MindMatters] Failed to create dynamic need instance for type '{needType.Name}': {ex.Message}"
                );
            }

            return null;
        }

        /// <summary>
        /// Creates a "bouquet" of needs for a specific pawn.
        /// </summary>
        public static List<DynamicNeed> CreateBouquet(Pawn pawn, DynamicNeedCategory? category = null)
        {
            var needs = new List<DynamicNeed>();
            foreach (var needProps in DynamicNeedsRegistry.GetNeedsForCategory(category))
            {
                var newNeed = CreateNeedInstance(needProps.NeedClass, pawn);
                if (newNeed != null)
                {
                    needs.Add(newNeed);
                }
            }

            return needs;
        }

        /// <summary>
        /// Example of creating a bundle of related needs triggered by an event.
        /// </summary>
        public static void ApplyEventTriggeredNeeds(Pawn pawn, string eventType)
        {
            switch (eventType)
            {
                case "SocialAdjustment":
                    CreateAndAddNeed(pawn, typeof(FreshFruitNeed));
                   // CreateAndAddNeed(pawn, typeof(ValidationNeed));
                    CreateAndAddNeed(pawn, typeof(ConstraintNeed));
                    break;

                default:
                    MindMattersUtilities.GripeOnce($"[MindMatters] Unknown event type '{eventType}' for needs creation.");
                    break;
            }
        }

        /// <summary>
        /// Helper to create and immediately add a need to the game.
        /// </summary>
        /// XXX Fix this method to use TryAddNeed
        private static void CreateAndAddNeed(Pawn pawn, Type needType)
        {
            var newNeed = CreateNeedInstance(needType, pawn);
            if (newNeed != null)
            {
                // pawn.needs.AddOrRemoveNeed(newNeed.def, true);
                MindMattersUtilities.DebugLog($"[MindMatters] Added need '{newNeed.def.defName}' to {pawn.LabelShort}.");
            }
        }
    }
}