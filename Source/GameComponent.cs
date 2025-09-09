using UnityEngine;
using Verse;
using System.Collections.Generic;
using RimWorld;
using System.Linq;
using System;

namespace MindMatters
{
    public class MindMattersGameComponent : GameComponent
    {
        public static MindMattersGameComponent Instance;

        private MindMattersNeedsMgr needsMgr;
        private OutcomeManager outcomeManager;
        public event Action<Pawn> OnPawnMoodChanged;

        // Misc fields
        private bool readyToParley = false;
        private bool isInitialized = false;
        private bool shouldRetryApiSetup = true;

        private int delayBeforeProcessing = 500;

        private int tickCounter = 0;
        private int longTickCounter = 0;

        // Possibly to track or store pawn data
        private List<Pawn> pawnKeys;
        private List<int> moodValues;
        private List<Pawn> allPawns;

        // pendingPawnQueue is now touched by the NeedsMgr quite early in the lifecycle. Beware!
        protected internal List<Pawn> pendingPawnQueue = new List<Pawn>();

        private MindMattersVictimManager victimManager = MindMattersVictimManager.Instance;

        private bool debugNeedsRegistry = true;

        TraitDef recluseTrait;

        public enum Mood
        {
            Happy,
            Unhappy,
            Neutral
        }

        public Dictionary<Pawn, float> lastSeverity = new Dictionary<Pawn, float>();
        private Dictionary<Pawn, Mood> PawnMoods = new Dictionary<Pawn, Mood>();
        public Dictionary<int, int> BipolarPawnLastCheckedTicks = new Dictionary<int, int>();
        public Dictionary<int, int> PawnLastAloneTicks = new Dictionary<int, int>();
        private Dictionary<int, int> UnstablePawnLastMentalBreakTicks = new Dictionary<int, int>();
        public Dictionary<int, int> UnstablePawnLastMoodSwitchTicks = new Dictionary<int, int>();
        public Dictionary<int, int> UnstablePawnLastMoodState = new Dictionary<int, int>();

#pragma warning disable CS8618, CS9264
        public MindMattersGameComponent(Game game)
#pragma warning restore CS8618, CS9264
        {
            outcomeManager = new OutcomeManager();
            recluseTrait = MindMattersTraitDef.Recluse ?? null;
            needsMgr = new MindMattersNeedsMgr();
            Instance = this;
        }

        public List<Pawn> GetAllPawns()
        {
            return allPawns;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            // If we haven't waited enough ticks after game load, do nothing
            if (delayBeforeProcessing > 0)
            {
                delayBeforeProcessing--;
                return;
            }

            // If environment is not ready, skip
            if (!IsEnvironmentReady())
            {
                return;
            }

            // If we haven't done the one-time init
            if (!isInitialized)
            {
                isInitialized = true;
                MindMattersMod.IsSystemReady = true;

                // Populate registry after environment is ready
                DynamicNeedsRegistry.Initialize();
                DynamicNeedsRegistry.PopulateRegistryFromDefDatabase();
                MMToolkit.DebugLog("<color=#00CCAA>[Mind Matters]</color> DynamicNeed registry populated.");

                // If user’s settings allow the API
                if (MindMattersMod.settings.enableAPI)
                {
                    readyToParley = true;
                    MindMattersMod.ReadyToParley = true;
                    MMToolkit.DebugLog("<color=#00CCAA>[Mind Matters]</color> Ready for action and ready to parley.");
                }
                else
                {
                    readyToParley = false;
                    MindMattersMod.ReadyToParley = false;
                    MMToolkit.DebugLog(
                        "<color=#00CCAA>[Mind Matters]</color> Ready for action. API disabled in Mod Settings.");
                }

                InitializePendingPawns();
            }

            // Log and debug dynamic needs once
            debugNeedsRegistry = true;
            if (debugNeedsRegistry)
            {
                DynamicNeedsRegistry.DebugLogRegisteredDynamicNeeds();
                debugNeedsRegistry = false;
            }

            // Minor ticks (every 600 ticks)
            if (SimulateRareTick(600))
            {
                ProcessPendingPawns(); // Add dynamic needs for newly spawned or pending pawns
            }

            // Medium ticks (every 1200 ticks)
            if (SimulateLongTick(1200))
            {
                // If we had delayed the API setup, check again
                if (shouldRetryApiSetup && readyToParley)
                {
                    readyToParley = true;
                    shouldRetryApiSetup = false;
                }

                ProcessTraitsForAllPawns();
                outcomeManager.ProcessOutcomes(); // e.g., experiences
            }

            // Longer tick (every 3600)
            if (SimulateLongTick(3600))
            {
                // Ensure all dynamic needs are up to date
                needsMgr.ProcessNeeds(null, null); // no category => process all categories
                CheckBipolarTraitsForAllPawns();
            }

            // Very long tick (90k) => once a day or so
            if (SimulateLongTick(90000))
            {
                victimManager.DesignateNewVictim();
            }
        }

        private bool SimulateRareTick(int interval)
        {
            tickCounter++;
            if (tickCounter >= interval)
            {
                tickCounter = 0;
                return true;
            }

            return false;
        }

        private bool SimulateLongTick(int interval)
        {
            longTickCounter++;
            if (longTickCounter >= interval)
            {
                longTickCounter = 0;
                return true;
            }

            return false;
        }

        // Decide how to define readiness
        private bool IsEnvironmentReady()
        {
            // Must have at least one map loaded
            if (Find.CurrentMap == null || Find.Maps == null || Find.Maps.Count == 0)
                return false;

            // Must have at least one playable pawn - double-check CurrentMap is not null
            if (Find.CurrentMap?.mapPawns?.SpawnedPawnsInFaction(Faction.OfPlayer)?.Any() != true)
                return false;

            // Ensure managers are not null
            if (needsMgr == null || outcomeManager == null)
                return false;

            return true;
        }

        private void InitializePendingPawns()
        {
            pendingPawnQueue.AddRange(PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned);
            ProcessPendingPawns(); // immediate pass
        }

        // pendingPawnQueue is correct name
        private void ProcessPendingPawns()
        {
            if (pendingPawnQueue.Count == 0)
                return;

            var processedPawns = new List<Pawn>();

            foreach (var pawn in pendingPawnQueue)
            {
                if (pawn.Spawned && pawn.needs != null)
                {
                    // Initialize dynamic needs for this pawn
                    needsMgr.InitializePawnNeeds(pawn);
                    processedPawns.Add(pawn);
                }
            }

            // Remove processed pawns from the queue
            pendingPawnQueue = pendingPawnQueue.Except(processedPawns).ToList();
        }

        private void ProcessTraitsForAllPawns()
        {
            allPawns = PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;
            if (allPawns == null) return;

            foreach (Pawn pawn in allPawns)
            {
                if (pawn?.story?.traits == null) continue;
                var traits = pawn.story.traits;

                // Example: If certain traits + "pawn alone" => do something
                if ((traits.HasTrait(MindMattersTraitDef.Outgoing) ||
                     traits.HasTrait(MindMattersTraitDef.Reserved) ||
                     (recluseTrait != null && traits.HasTrait(recluseTrait))) &&
                    MindMattersUtilities.IsPawnAlone(pawn, allPawns))
                {
                    PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager?.TicksGame ?? 0;
                }

                // Example: if Unstable => handle logic
                if (traits.HasTrait(MindMattersTraitDef.Unstable) && pawn.MentalState != null)
                {
                    ProcessUnstableTrait(pawn);
                }
            }
        }

        private void ProcessUnstableTrait(Pawn pawn)
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (!UnstablePawnLastMoodSwitchTicks.ContainsKey(pawn.thingIDNumber))
            {
                UnstablePawnLastMoodSwitchTicks[pawn.thingIDNumber] = currentTick;
            }

            if (!UnstablePawnLastMentalBreakTicks.ContainsKey(pawn.thingIDNumber) ||
                currentTick - UnstablePawnLastMentalBreakTicks[pawn.thingIDNumber] > 60000)
            {
                UnstablePawnLastMentalBreakTicks[pawn.thingIDNumber] = currentTick;
                MindMattersUtilities.TryGiveRandomInspiration(pawn);
            }

            MindMattersUtilities.UpdatePawnMoods(pawn, PawnMoods, OnPawnMoodChanged);
        }

        private void CheckBipolarTraitsForAllPawns()
        {
            if (allPawns == null) return;

            foreach (Pawn pawn in allPawns)
            {
                if (pawn?.story?.traits == null) continue;
                if (pawn.story.traits.HasTrait(MindMattersTraitDef.Bipolar))
                {
                    MindMattersUtilities.UpdateBipolarPawnTicks(pawn, BipolarPawnLastCheckedTicks);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                pawnKeys = PawnMoods.Keys.ToList();
                moodValues = PawnMoods.Values.Select(mood => (int)mood).ToList();
            }

            Scribe_Collections.Look(ref pawnKeys, "pawnKeys", LookMode.Reference);
            Scribe_Collections.Look(ref moodValues, "moodValues", LookMode.Value);
            Scribe_Collections.Look(ref BipolarPawnLastCheckedTicks, "BipolarPawnLastCheckedTicks", LookMode.Value,
                LookMode.Value);
            Scribe_Collections.Look(ref PawnLastAloneTicks, "PawnLastAloneTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref UnstablePawnLastMentalBreakTicks, "UnstablePawnLastMentalBreakTicks",
                LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref UnstablePawnLastMoodSwitchTicks, "UnstablePawnLastMoodSwitchTicks",
                LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Rebuild PawnMoods
                PawnMoods = new Dictionary<Pawn, Mood>();
                if (pawnKeys != null && moodValues != null)
                {
                    PawnMoods = pawnKeys
                        .Zip(moodValues, (key, value) => new { Key = key, Value = (Mood)value })
                        .ToDictionary(x => x.Key, x => x.Value);
                }

                UnstablePawnLastMentalBreakTicks ??= new Dictionary<int, int>();
                UnstablePawnLastMoodSwitchTicks ??= new Dictionary<int, int>();
            }
        }
    }
}