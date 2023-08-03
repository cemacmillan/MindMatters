using System;
using System.Linq;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MindMatters
{
    public class ThoughtWorker_Bipolar : ThoughtWorker
    {
        private Dictionary<Pawn, int> lastSeverityChangeDay = new Dictionary<Pawn, int>();
        private Dictionary<Pawn, float> lastSeverity = new Dictionary<Pawn, float>(); // New dictionary to store the last severity for each pawn

        // Define the weights for each stage. Higher numbers = more likely to be chosen.
        private static readonly int[] StageWeights = { 1, 2, 5, 2, 1 };
        // Calculate the total weight, which is needed to calculate probabilities.
        private static readonly int TotalWeight = StageWeights.Sum();
        // Store the day of the year the last mood roll was made for each pawn
        private Dictionary<Pawn, int> lastRollDayOfYear = new Dictionary<Pawn, int>();

        static ThoughtWorker_Bipolar()
        {
            Log.Message("ThoughtWorker_Bipolar loaded");
        }

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.health.hediffSet.HasHediff(HediffDef.Named("Bipolar")))
                return ThoughtState.Inactive;

            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Bipolar"));

            // Initialize the last severity change day for new pawns
            if (!lastSeverityChangeDay.ContainsKey(p))
            {
                lastSeverityChangeDay[p] = -1;
            }

            // Initialize the last severity for new pawns
            if (!lastSeverity.ContainsKey(p))
            {
                lastSeverity[p] = hediff.Severity;
            }

            // Check if it's a new day since the last severity change and the current hour is 18
            if ((lastSeverityChangeDay[p] != GenLocalDate.DayOfYear(p.Map)) && GenLocalDate.HourOfDay(p.Map) == 18)
            {
                // Update the last severity change day
                lastSeverityChangeDay[p] = GenLocalDate.DayOfYear(p.Map);

                // Change the severity based on the weighted random stage
                int newStage = WeightedRandomStage();
                Log.Message($"Setting stage for {p.Name} to {newStage}");

                float newSeverity = (newStage / (float)(StageWeights.Length - 1)) * 0.99f;

                // Ensure severity never goes to absolute zero
                if (newSeverity <= 0)
                {
                    newSeverity = 0.01f;
                }

                if (newSeverity >= 1)
                {
                    newSeverity = 0.99f;
                }

                hediff.Severity = newSeverity;
                // Log the new severity and the corresponding stage
                Log.Message($"New severity for {p.Name} is {newSeverity}, mapping to stage {(int)(newSeverity * StageWeights.Length)}");

                // Save the updated severity
                lastSeverity[p] = newSeverity;
            }

            // Determine the active stage based on the last saved severity if the severity hasn't changed since the last call
            return ThoughtStateForSeverity(lastSeverity[p]);
        }
        private ThoughtState ThoughtStateForSeverity(float severity)
        {
            if (severity > 1.0f || severity < 0.0f)
            {
                Log.Error($"Severity {severity} is outside the expected range! Defaulting to stable.");
                return ThoughtState.ActiveAtStage(2);
            }
            else if (severity >= 0.8)
            {
                // Severely manic
                return ThoughtState.ActiveAtStage(4);
            }
            else if (severity >= 0.6)
            {
                // Mildly manic
                return ThoughtState.ActiveAtStage(3);
            }
            else if (severity >= 0.4)
            {
                // Stable
                return ThoughtState.ActiveAtStage(2);
            }
            else if (severity >= 0.2)
            {
                // Mildly depressed
                return ThoughtState.ActiveAtStage(1);
            }
            else
            {
                // Severely depressed
                return ThoughtState.ActiveAtStage(0);
            }
        }

        private int WeightedRandomStage()
        {
            // Pick a random number between 0 (inclusive) and the total weight (exclusive).
            int roll = Rand.RangeInclusive(0, TotalWeight - 1);
            Log.Message($"Rolled {roll} for bipolar stage.");

            // Determine which stage this roll corresponds to.
            int accumulatedWeight = 0;
            for (int i = 0; i < StageWeights.Length; i++)
            {
                accumulatedWeight += StageWeights[i];
                if (roll < accumulatedWeight)
                {
                    Log.Message($"Selected stage {i} based on roll.");
                    return i;
                }
            }

            // If we get here, something went wrong.
            Log.Error("Failed to select a bipolar stage!");

            // Default to the last defined stage if something goes wrong.
            int defaultStage = Math.Max(0, StageWeights.Length - 1);
            Log.Warning($"Defaulting to stage {defaultStage} due to selection failure.");

            return defaultStage;
        }
    }
}
