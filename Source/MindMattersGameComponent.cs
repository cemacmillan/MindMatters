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
        //private List<Pawn> severityKeys;
        //private List<float> severityValues;

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
            if (Instance != null)
            {
                Log.Warning("A new instance of MindMattersGameComponent is being created, even though an instance already exists. This is expected if a new game is being loaded or started.");
            }
            else
            {
                Log.Message("MindMattersGameComponent created.");
            }
            Instance = this; // Assign this as the new Instance, whether it previously existed or not
        }

        private static readonly int[] StageWeights = { 1, 2, 5, 2, 1 };

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager.TicksGame % 2500 == 0) // Every day
            {
                var allPawns = PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;
                if (allPawns == null)
                {
                    Log.Error("AllMaps_FreeColonistsAndPrisonersSpawned list is null.");
                    return;
                }

                var allPawnsCopy = new List<Pawn>(allPawns);

                var pawnsToRemoveFromBipolarTicks = new List<int>();
                var pawnsToAddToBipolarTicks = new Dictionary<int, int>();

                foreach (Pawn pawn in allPawnsCopy)
                {
                    if (pawn == null)
                    {
                        Log.Error("Found null pawn in AllMaps_FreeColonistsAndPrisonersSpawned list.");
                        continue;
                    }

                    if (MindMattersUtilities.IsPawnAlone(pawn))
                    {
                        PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                    }
                    //Unstable and bipolar
                    if (pawn.story != null && pawn.story.traits != null &&
                        pawn.story.traits.HasTrait(TraitDef.Named("Unstable")) && pawn.MentalState != null)
                    {
                        if (!UnstablePawnLastMoodSwitchTicks.ContainsKey(pawn.thingIDNumber))
                        {
                            UnstablePawnLastMoodSwitchTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                        }
                    }

                    Hediff bipolar = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Bipolar"));

                    if (bipolar != null)
                    {
                        if (!BipolarPawnLastCheckedTicks.TryGetValue(pawn.thingIDNumber, out int lastCheckedTick))
                        {
                            lastCheckedTick = Find.TickManager.TicksGame;
                            BipolarPawnLastCheckedTicks[pawn.thingIDNumber] = lastCheckedTick;
                        }

                        if (Find.TickManager.TicksGame >= lastCheckedTick + GenDate.TicksPerDay)
                        {
                            // Pass bipolar.Severity to WeightedRandomStageWithInertia instead of bipolar.CurStageIndex
                            int newStage = WeightedRandomStageWithInertia(pawn, bipolar.Severity);
                            bipolar.Severity = newStage / (float)(bipolar.def.stages.Count - 1);

                            BipolarPawnLastCheckedTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                        }
                    }
                    else
                    {
                        if (BipolarPawnLastCheckedTicks.ContainsKey(pawn.thingIDNumber))
                        {
                            pawnsToRemoveFromBipolarTicks.Add(pawn.thingIDNumber);
                        }
                    }

                    if (PawnMoods.TryGetValue(pawn, out var currentMood))
                    {
                        Mood newMood = GetMoodForPawn(pawn);

                        if (currentMood != newMood)
                        {
                            PawnMoods[pawn] = newMood;
                            OnPawnMoodChanged?.Invoke(pawn);
                        }
                    }
                    else
                    {
                        PawnMoods[pawn] = GetMoodForPawn(pawn);
                    }

                    // New code for Unstable trait
                    if (pawn.story.traits.HasTrait(TraitDef.Named("Unstable")) && pawn.MentalState != null)
                    {
                        if (!UnstablePawnLastMentalBreakTicks.ContainsKey(pawn.thingIDNumber) || Find.TickManager.TicksGame - UnstablePawnLastMentalBreakTicks[pawn.thingIDNumber] > 60000)
                        {
                            UnstablePawnLastMentalBreakTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;

                            TryGiveRandomInspiration(pawn);
                        }
                    }
                }

                foreach (var pawnId in pawnsToRemoveFromBipolarTicks)
                {
                    BipolarPawnLastCheckedTicks.Remove(pawnId);
                }

                foreach (var pair in pawnsToAddToBipolarTicks)
                {
                    BipolarPawnLastCheckedTicks[pair.Key] = pair.Value;
                }
            }
        }

        private Mood GetMoodForPawn(Pawn pawn)
        {
            float pawnMood = pawn.needs?.mood?.CurLevel ?? 0f;
            if (pawnMood > 0.6f) return Mood.Happy;
            if (pawnMood < 0.4f) return Mood.Unhappy;
            return Mood.Neutral;
        }

        private void TryGiveRandomInspiration(Pawn pawn)
        {
            if (Rand.Chance(0.5f))
            {
                var validInspirations = DefDatabase<InspirationDef>.AllDefsListForReading
                    .Where(inspiration => pawn.InspirationDef == null)
                    .ToList();

                if (validInspirations.Any())
                {
                    var randomInspiration = validInspirations[Rand.Range(0, validInspirations.Count)];
                    pawn.mindState.inspirationHandler.TryStartInspiration(randomInspiration);
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

        private int WeightedRandomStageWithInertia(Pawn p, float lastSeverity)
        {
            lastSeverity = Mathf.Clamp01(lastSeverity);
            int currentStage = Mathf.RoundToInt(lastSeverity * (StageWeights.Length - 1));

            List<int> possibleStages = Enumerable.Range(0, StageWeights.Length).ToList();

            Log.Message($"Current stage: {currentStage}");
            Log.Message($"Possible stages before removal: {string.Join(", ", possibleStages)}");

            possibleStages.RemoveAll(stage => Math.Abs(stage - currentStage) > 1);

            Log.Message($"Possible stages after removal: {string.Join(", ", possibleStages)}");

            // Ensure all possible stages are within valid bounds
            possibleStages = possibleStages.Where(stage => stage >= 0 && stage < StageWeights.Length).ToList();

            if (!possibleStages.Any())
            {
                Log.Warning("No valid stages for bipolar thought.");
                return currentStage;
            }

            int totalWeight = possibleStages.Sum(stage => StageWeights[stage]);

            int randomNumber = Rand.RangeInclusive(1, totalWeight);

            Log.Message($"Random number: {randomNumber}, total weight: {totalWeight}");

            int cumulativeWeight = 0;
            foreach (int stage in possibleStages)
            {
                cumulativeWeight += StageWeights[stage];
                if (randomNumber <= cumulativeWeight)
                    return stage;
            }

            // If somehow no stage was selected, return the current stage
            return currentStage;
        }
    }
}