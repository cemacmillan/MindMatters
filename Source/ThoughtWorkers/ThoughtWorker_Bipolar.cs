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
        private Dictionary<Pawn, float> lastSeverity = new Dictionary<Pawn, float>();

        private static readonly int[] StageWeights = { 1, 2, 5, 2, 1 };
        private static readonly int TotalWeight = StageWeights.Sum();

        static ThoughtWorker_Bipolar()
        {
            Log.Message("ThoughtWorker_Bipolar loaded");
        }

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.health.hediffSet.HasHediff(HediffDef.Named("Bipolar")))
                return ThoughtState.Inactive;

            // If it's not 18:00, return the cached thought state immediately
            if (GenLocalDate.HourOfDay(p.Map) != 18)
            {
                if (lastSeverity.ContainsKey(p)) // Make sure we have a cached severity for this pawn
                    return ThoughtStateForSeverity(lastSeverity[p]);
                else
                    return ThoughtState.Inactive; // Default state if we have no cached severity
            }

            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Bipolar"));

            if (!lastSeverityChangeDay.ContainsKey(p))
                lastSeverityChangeDay[p] = -1;

            if (!lastSeverity.ContainsKey(p))
                lastSeverity[p] = hediff.Severity;

            if (lastSeverityChangeDay[p] != GenLocalDate.DayOfYear(p.Map))
            {
                lastSeverityChangeDay[p] = GenLocalDate.DayOfYear(p.Map);

                int newStage = WeightedRandomStage();

                float newSeverity = (newStage / (float)(StageWeights.Length - 1)) * 0.99f;

                if (newSeverity <= 0)
                    newSeverity = 0.01f;

                if (newSeverity >= 1)
                    newSeverity = 0.99f;

                hediff.Severity = newSeverity;
                lastSeverity[p] = newSeverity;
            }

            return ThoughtStateForSeverity(lastSeverity[p]);
        }

        private ThoughtState ThoughtStateForSeverity(float severity)
        {
            if (severity > 1.0f || severity < 0.0f)
                return ThoughtState.ActiveAtStage(2);
            else if (severity >= 0.8)
                return ThoughtState.ActiveAtStage(4);
            else if (severity >= 0.6)
                return ThoughtState.ActiveAtStage(3);
            else if (severity >= 0.4)
                return ThoughtState.ActiveAtStage(2);
            else if (severity >= 0.2)
                return ThoughtState.ActiveAtStage(1);
            else
                return ThoughtState.ActiveAtStage(0);
        }

        private int WeightedRandomStage()
        {
            int roll = Rand.RangeInclusive(0, TotalWeight - 1);

            int accumulatedWeight = 0;
            for (int i = 0; i < StageWeights.Length; i++)
            {
                accumulatedWeight += StageWeights[i];
                if (roll < accumulatedWeight)
                    return i;
            }

            return Math.Max(0, StageWeights.Length - 1);
        }
    }
}
