using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class MindMattersNeedsMgr : IExposable
    {
        private Dictionary<Pawn, DynamicNeedsBitmap> pawnNeedsMap = new();

        // Process all registered needs
        public void ProcessAllNeeds()
        {
            foreach (DynamicNeedCategory category in Enum.GetValues(typeof(DynamicNeedCategory)))
            {
                ProcessNeeds(category);
            }
        }

        // Process needs by category
        public void ProcessNeeds(DynamicNeedCategory? category = null)
        {
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned)
            {
                if (pawn == null || !pawn.RaceProps.Humanlike)
                {
                    continue;
                }

                var potentialNeeds = DynamicNeedFactory.GetNeedsForCategory(category);

                foreach (var needProps in potentialNeeds)
                {
                    if (HasNeed(pawn, DynamicNeedFactory.GetBitmapForNeed(needProps.NeedClass)))
                    {
                        continue;
                    }

                    if (DynamicNeedFactory.ShouldPawnHaveNeed(pawn, needProps.NeedClass))
                    {
                        AddNeed(pawn, DynamicNeedFactory.GetBitmapForNeed(needProps.NeedClass));
                    }
                }
            }
        }

        // Add a need to a pawn
        public void AddNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
        {
            if (!pawnNeedsMap.ContainsKey(pawn))
            {
                pawnNeedsMap[pawn] = DynamicNeedsBitmap.None;
            }

            pawnNeedsMap[pawn] = DynamicNeedHelper.AddNeed(pawnNeedsMap[pawn], needBitmap);

            var needProps = DynamicNeedFactory.GetPropertiesForBitmap(needBitmap);
            if (needProps == null)
            {
                MindMattersUtilities.GripeOnce($"[MindMatters] No properties found for bitmap: {needBitmap}");
                return;
            }

            var newNeed = DynamicNeedFactory.CreateNeedInstance(needProps.NeedClass, pawn);
            if (newNeed != null)
            {
                pawn.needs.AllNeeds.Add(newNeed);
                MindMattersUtilities.DebugLog($"[MindMatters] Successfully added need '{newNeed.def.defName}' to {pawn.LabelShort}.");
            }
        }

        // Remove a need from a pawn
        public void RemoveNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
        {
            if (pawnNeedsMap.TryGetValue(pawn, out var currentNeeds))
            {
                pawnNeedsMap[pawn] = DynamicNeedHelper.RemoveNeed(currentNeeds, needBitmap);

                // Remove the need instance
                var needProps = DynamicNeedFactory.GetPropertiesForBitmap(needBitmap);
                if (needProps != null)
                {
                    var needDef = DynamicNeedFactory.GetNeedDef(needProps.NeedClass);
                    var need = pawn.needs.TryGetNeed(needDef);
                    if (need != null)
                    {
                        pawn.needs.AllNeeds.Remove(need);
                        MindMattersUtilities.DebugLog($"[MindMatters] Successfully removed need '{needDef.defName}' from {pawn.LabelShort}.");
                    }
                }
            }
        }

        // Check if a pawn has a specific need
        public bool HasNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
        {
            return pawnNeedsMap.TryGetValue(pawn, out var currentNeeds) &&
                   DynamicNeedHelper.IsNeedActive(currentNeeds, needBitmap);
        }

        // Expose data for saving/loading
        public void ExposeData()
        {
            Scribe_Collections.Look(ref pawnNeedsMap, "pawnNeedsMap", LookMode.Reference, LookMode.Value);
        }
    }
}