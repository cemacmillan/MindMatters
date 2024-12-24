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

        private List<Pawn> pawnKeys;
        private List<int> moodValues;
        private List<Pawn> allPawns;
        
        private MindMattersVictimManager victimManager = MindMattersVictimManager.Instance;

        TraitDef recluseTrait;

        private bool debugNeedsRegistry = true;

        public enum Mood
        {
            Happy,
            Unhappy,
            Neutral
        }

        public Dictionary<Pawn, float> lastSeverity = new Dictionary<Pawn, float>();
        public Dictionary<Pawn, Mood> PawnMoods = new Dictionary<Pawn, Mood>();
        public Dictionary<int, int> BipolarPawnLastCheckedTicks = new Dictionary<int, int>();
        public Dictionary<int, int> PawnLastAloneTicks = new Dictionary<int, int>();
        public Dictionary<int, int> UnstablePawnLastMentalBreakTicks = new Dictionary<int, int>();
        public Dictionary<int, int> UnstablePawnLastMoodSwitchTicks = new Dictionary<int, int>();
        public Dictionary<int, int> UnstablePawnLastMoodState = new Dictionary<int, int>();

        public MindMattersGameComponent(Game game)
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

            int currentTick = Find.TickManager.TicksGame;

            // Every game hour (testing for now)
            if (currentTick % 900000 == 0)
            {
                victimManager.DesignateNewVictim();
            }

            // Every 5 seconds or so
            if (currentTick % 300 == 0)
            {
                ProcessTraitsForAllPawns();
            }

            // Every quarter game day (6 hours)
            if (currentTick % 15000 == 0)
            {
                outcomeManager.ProcessOutcomes();
            }

            // Every day
            if (currentTick % 60000 == 0)
            {
                CheckBipolarTraitsForAllPawns();
            }

            if(debugNeedsRegistry) {
                MMToolkit.DebugLog("GameComponentTick: NeedDefs");
                DynamicNeedFactory.DebugLogRegisteredDynamicNeeds(); // Ensures one-time call
                debugNeedsRegistry = false;
            }
            
            if (currentTick % 300 == 0) // Every 5 seconds
            {
                needsMgr.ProcessNeeds(DynamicNeedCategory.Primal);
            }

            if (currentTick % 600 == 0) // Every 10 seconds
            {
                needsMgr.ProcessNeeds(DynamicNeedCategory.Secondary);
            }

            if (currentTick % 1200 == 0) // Every 20 seconds
            {
                needsMgr.ProcessAllNeeds();
            }
            

        }

        private void ProcessTraitsForAllPawns()
        {
            allPawns = PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;

            if (allPawns == null)
            {
                return;
            }

            foreach (Pawn pawn in allPawns)
            {
                if (pawn == null || pawn.story == null || pawn.story.traits == null)
                {
                    continue;
                }

                var traits = pawn.story.traits;

                if ((traits.HasTrait(MindMattersTraitDef.Outgoing) ||
                     traits.HasTrait(MindMattersTraitDef.Reserved) ||
                     (recluseTrait != null && traits.HasTrait(recluseTrait))) &&
                    MindMattersUtilities.IsPawnAlone(pawn,allPawns))
                {
                    PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                }

                if (traits.HasTrait(MindMattersTraitDef.Unstable) && pawn.MentalState != null)
                {
                    ProcessUnstableTrait(pawn);
                }
            }
        }

        private void ProcessUnstableTrait(Pawn pawn)
        {
            int currentTick = Find.TickManager.TicksGame;

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
            foreach (Pawn pawn in allPawns)
            {
                if (pawn == null || pawn.story == null || pawn.story.traits == null)
                {
                    continue;
                }

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
            Scribe_Collections.Look(ref BipolarPawnLastCheckedTicks, "BipolarPawnLastCheckedTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref PawnLastAloneTicks, "PawnLastAloneTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref UnstablePawnLastMentalBreakTicks, "UnstablePawnLastMentalBreakTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref UnstablePawnLastMoodSwitchTicks, "UnstablePawnLastMoodSwitchTicks", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                PawnMoods = pawnKeys.Zip(moodValues, (key, value) => new { Key = key, Value = (Mood)value })
                    .ToDictionary(x => x.Key, x => x.Value);

                UnstablePawnLastMentalBreakTicks = UnstablePawnLastMentalBreakTicks ?? new Dictionary<int, int>();
                UnstablePawnLastMoodSwitchTicks = UnstablePawnLastMoodSwitchTicks ?? new Dictionary<int, int>();
            }
        }
    }
    
    
}