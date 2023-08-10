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
        public event Action<Pawn> OnPawnMoodChanged;

        private List<Pawn> pawnKeys;
        private List<int> moodValues;

        private MindMattersVictimManager victimManager = new MindMattersVictimManager();


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
            Instance = this;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            //if (Find.TickManager.TicksGame % 3600000 == 0)
            if (Find.TickManager.TicksGame % 3600 == 0)
            {
                victimManager.DesignateNewVictim();
            }

            // Every 60 ticks
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                var allPawns = PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;
                if (allPawns == null)
                {
                    return;
                }

                foreach (Pawn pawn in allPawns)
                {
                    if (pawn == null)
                    {
                        continue;
                    }

                    if (pawn.story != null && pawn.story.traits != null)
                    {
                        var traits = pawn.story.traits;

                        if (traits.HasTrait(MindMattersTraits.Outgoing) ||
                            traits.HasTrait(MindMattersTraits.Reserved) ||
                            traits.HasTrait(MindMattersTraits.Recluse))
                        {
                            if (MindMattersUtilities.IsPawnAlone(pawn))
                            {
                                PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                            }
                        }

                        if (traits.HasTrait(MindMattersTraits.Unstable) && pawn.MentalState != null)
                        {
                            if (!UnstablePawnLastMoodSwitchTicks.ContainsKey(pawn.thingIDNumber))
                            {
                                UnstablePawnLastMoodSwitchTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                            }

                            if (!UnstablePawnLastMentalBreakTicks.ContainsKey(pawn.thingIDNumber) || Find.TickManager.TicksGame - UnstablePawnLastMentalBreakTicks[pawn.thingIDNumber] > 60000)
                            {
                                UnstablePawnLastMentalBreakTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                                MindMattersUtilities.TryGiveRandomInspiration(pawn);
                            }
                            MindMattersUtilities.UpdatePawnMoods(pawn, PawnMoods, OnPawnMoodChanged);
                        }
                    }

                    
                }
            }

            // Every day
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                var allPawns = PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;
                if (allPawns == null)
                {
                    return;
                }

                foreach (Pawn pawn in allPawns)
                {
                    if (pawn == null)
                    {
                        continue;
                    }

                    if (pawn.story != null && pawn.story.traits != null)
                    {
                        var traits = pawn.story.traits;

                        if (traits.HasTrait(MindMattersTraits.Bipolar))
                        {
                            MindMattersUtilities.UpdateBipolarPawnTicks(pawn, BipolarPawnLastCheckedTicks);
                        }
                    }
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