using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class MindMattersNeedsMgr : IExposable
    {
        // ----------------------------------------------------------------------
        // DATA FIELDS
        // ----------------------------------------------------------------------

        private Dictionary<Pawn, DynamicNeedsBitmap> pawnNeedsMap = new();

        private bool isProcessing = false;
        private int ticksUntilNextProcess = 0;

        // ----------------------------------------------------------------------
        // AGGREGATOR FOR THE HARMONY PATCH
        // ----------------------------------------------------------------------
        public static bool CanPawnHaveDynamicNeed(Pawn pawn, NeedDef needDef)
        {
            // Basic null and destroyed checks
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                //MMToolkit.GripeOnce($"[MindMatters] Invalid pawn detected: {pawn?.LabelShort ?? "Unknown"}");
                return false;
            }

            // Check if the pawn is incomplete or a PawnKind template
            if (pawn.RaceProps == null || pawn.health == null || pawn.story == null)
            {
                // MMToolkit.GripeOnce(
                   // $"[MindMatters] Skipping incomplete pawn or PawnKind: {pawn.def?.defName ?? "Unknown"}");
                return false;
            }

            // Minimum intelligence required for the need
            if ((int)pawn.RaceProps.intelligence < (int)needDef.minIntelligence)
            {
                return false;
            }

            // Developmental stage filtering
            if (!needDef.developmentalStageFilter.Has(pawn.DevelopmentalStage))
            {
                return false;
            }

            // Faction-based exclusions
            if (needDef.colonistsOnly && (pawn.Faction == null || !pawn.Faction.IsPlayer))
            {
                return false;
            }

            // Slaves and prisoners
            if (needDef.neverOnPrisoner && pawn.IsPrisoner)
            {
                return false;
            }

            if (needDef.neverOnSlave && pawn.IsSlave)
            {
                return false;
            }

            // Hediff-based exclusions
            if (pawn.health.hediffSet.hediffs.Any(h => h.def?.disablesNeeds?.Contains(needDef) == true))
            {
                return false;
            }

            // Trait-based exclusions
            if (pawn.story.traits.allTraits.Any(t =>
                    !t.Suppressed && t.CurrentData?.disablesNeeds?.Contains(needDef) == true))
            {
                return false;
            }

            // Gene-based exclusions (Biotech)
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.GenesListForReading.Any(g =>
                        g.Active && g.def?.disablesNeeds?.Contains(needDef) == true))
                {
                    return false;
                }
            }

            // Dynamic-specific check
            if (Activator.CreateInstance(needDef.needClass) is DynamicNeed dynNeed)
            {
                try
                {
                    return dynNeed.ShouldPawnHaveThisNeed(pawn);
                }
                catch (Exception ex)
                {
                    MMToolkit.GripeOnce(
                        $"Error while determining if {pawn.LabelShort} should have Need {needDef.defName}: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        // ----------------------------------------------------------------------
        // SITUATIONAL AWARENESS CHECK
        // ----------------------------------------------------------------------

        private static bool SituationalAwarenessCheck(Pawn pawn, NeedDef needDef)
        {
            // Placeholder logic: Expand as needed
            // E.g., Prevent needs from being added during high-priority situations
            // or when the player explicitly opts out.
            return true; // For now, always allow
        }

        // ----------------------------------------------------------------------
        // TICKING / UPDATING ALL DYNAMIC NEEDS
        // ----------------------------------------------------------------------

        public void TickAllDynamicNeeds(int specificDelay = 0)
        {
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned)
            {
                if (pawn == null || !pawn.RaceProps.Humanlike) continue;

                foreach (var need in pawn.needs.AllNeeds.OfType<IDynamicNeed>())
                {
                    try
                    {
                        need.NeedInterval();
                    }
                    catch (Exception ex)
                    {
                        MindMattersUtilities.GripeOnce(
                            $"[MindMatters] Error ticking dynamic need '{need}' for '{pawn.LabelShort}': {ex.Message}"
                        );
                    }
                }
            }

            if (specificDelay > 0)
            {
                ticksUntilNextProcess = specificDelay;
            }
        }

        // ----------------------------------------------------------------------
        // MASTER PROCESS METHOD
        // ----------------------------------------------------------------------

        public void ProcessNeeds(
            Pawn pawn = null,
            DynamicNeedCategory? category = null,
            bool exclusive = false,
            string failures = "warning",
            int specificDelay = 0
        )
        {
            if (exclusive && isProcessing)
            {
                if (failures == "exception")
                {
                    throw new InvalidOperationException("[MindMatters] Exclusive processing in progress.");
                }

                MindMattersUtilities.GripeOnce("[MindMatters] Exclusive processing in progress. Skipping.");
                return;
            }

            if (specificDelay > 0 && ticksUntilNextProcess > 0)
            {
                ticksUntilNextProcess--;
                if (ticksUntilNextProcess > 0) return;
            }

            isProcessing = true;

            try
            {
                IEnumerable<Pawn> pawns = pawn != null
                    ? new List<Pawn> { pawn }
                    : PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;

                foreach (var p in pawns)
                {
                    if (p == null || !p.RaceProps.Humanlike) continue;

                    foreach (var needProps in DynamicNeedsRegistry.GetNeedsForCategory(category))
                    {
                        var bitmap = DynamicNeedsRegistry.GetBitmapForNeed(needProps.NeedClass);
                        bool alreadyHasIt = HasNeed(p, bitmap);

                        var needDef = DynamicNeedsRegistry.GetNeedDef(needProps.NeedClass);
                        if (needDef == null) continue;

                        bool shouldHave = CanPawnHaveDynamicNeed(p, needDef);

                        if (shouldHave && !alreadyHasIt)
                        {
                            try
                            {
                                TryAddNeed(p, needDef);
                            }
                            catch (Exception ex)
                            {
                                HandleProcessingFailure(failures, ex, needProps.NeedClass, p);
                            }
                        }
                        else if (!shouldHave && alreadyHasIt)
                        {
                            RemoveNeed(p, bitmap);
                        }
                    }
                }
            }
            finally
            {
                isProcessing = false;
                if (specificDelay > 0) ticksUntilNextProcess = specificDelay;
            }
        }

        private void TryAddNeed(Pawn pawn, NeedDef needDef)
        {
            // Check if the need already exists in AllNeeds
            if (pawn.needs.TryGetNeed(needDef) != null)
            {
                // MindMattersUtilities.DebugLog(
                   //  $"[MindMatters] Need '{needDef.defName}' already exists for {pawn.LabelShort}.");
                return;
            }

            // Create and add the new need safely
            var newNeed = (Need)Activator.CreateInstance(needDef.needClass, pawn);
            if (newNeed == null)
            {
                MindMattersUtilities.GripeOnce(
                    $"[MindMatters] Failed to create instance for NeedDef '{needDef.defName}'.");
                return;
            }

            newNeed.def = needDef;
            newNeed.SetInitialLevel();

            pawn.needs.AllNeeds.Add(newNeed);
            pawn.needs.BindDirectNeedFields(); // Ensures proper bindings

            // MindMattersUtilities.DebugLog(
               //  $"[MindMatters] Successfully added need '{needDef.defName}' to {pawn.LabelShort}.");
        }

        public void InitializePawnNeeds(Pawn pawn)
        {
            ProcessNeeds(pawn: pawn);
        }

        // ----------------------------------------------------------------------
        // ADD / REMOVE / HAS
        // ----------------------------------------------------------------------

        public void AddNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
        {
            if (pawn != null) pawnNeedsMap.TryAdd(pawn, DynamicNeedsBitmap.None);

            pawnNeedsMap[pawn] = DynamicNeedHelper.AddNeed(pawnNeedsMap[pawn], needBitmap);

            var needProps = DynamicNeedsRegistry.GetPropertiesForBitmap(needBitmap);
            if (needProps == null)
            {
                MindMattersUtilities.GripeOnce($"[MindMatters] No properties found for bitmap: {needBitmap}");
                return;
            }

            var newNeed = DynamicNeedFactory.CreateNeedInstance(needProps.NeedClass, pawn);
            if (newNeed != null)
            {
                if (!MindMattersMod.settings.DoNotAddNeeds)
                {
                    pawn.needs.AllNeeds.Add(newNeed);
                }

                //MindMattersUtilities.DebugLog(
                  //  $"[MindMatters] Successfully added need '{newNeed.def.defName}' to {pawn.LabelShort}."
                //);
            }
        }

        public void RemoveNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
        {
            if (pawnNeedsMap.TryGetValue(pawn, out var currentNeeds))
            {
                pawnNeedsMap[pawn] = DynamicNeedHelper.RemoveNeed(currentNeeds, needBitmap);

                var needProps = DynamicNeedsRegistry.GetPropertiesForBitmap(needBitmap);
                if (needProps != null)
                {
                    var needDef = DynamicNeedsRegistry.GetNeedDef(needProps.NeedClass);
                    var need = pawn.needs.TryGetNeed(needDef);
                    if (need != null)
                    {
                        if (!MindMattersMod.settings.DoNotAddNeeds)
                        {
                            pawn.needs.AllNeeds.Remove(need);
                        }

                        // MindMattersUtilities.DebugLog(
                         //   $"[MindMatters] Successfully removed need '{needDef.defName}' from {pawn.LabelShort}."
                        //);
                    }
                }
            }
        }

        public bool HasNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
        {
            return pawnNeedsMap.TryGetValue(pawn, out var currentNeeds) &&
                   DynamicNeedHelper.IsNeedActive(currentNeeds, needBitmap);
        }

        private void HandleProcessingFailure(string failures, Exception ex, Type needClass, Pawn pawn)
        {
            string message =
                $"[MindMatters] Failed to process need '{needClass.Name}' for {pawn.LabelShort}: {ex.Message}";
            if (failures == "exception")
            {
                throw new InvalidOperationException(message, ex);
            }
            else if (failures == "warning")
            {
                MindMattersUtilities.GripeOnce(message);
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref pawnNeedsMap, "pawnNeedsMap", LookMode.Reference, LookMode.Value);
        }
    }
}