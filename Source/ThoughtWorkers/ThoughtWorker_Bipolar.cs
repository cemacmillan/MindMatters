using System;
using System.Linq;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;

namespace MindMatters
{
    public class ThoughtWorker_Bipolar : ThoughtWorker
    {


        //private Dictionary<Pawn, float> lastSeverity = new Dictionary<Pawn, float>();

        private static readonly int[] StageWeights = { 1, 2, 5, 2, 1 };
        private static readonly int TotalWeight = StageWeights.Sum();

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            //Log.Message($"Checking thought state for {p.Name.ToStringShort}");

            // Null check for pawn
            if (p == null)
            {
                Log.Error("Pawn object is null in CurrentStateInternal");
                return false;
            }

            var gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();
            if (gameComponent == null)
            {
                Log.Error("MindMattersGameComponent not found.");
                return false;
            }

            // Fetch Bipolar HediffDef and null check
            HediffDef bipolarDef = HediffDef.Named("Bipolar");
            if (bipolarDef == null)
            {
                Log.Error("Bipolar HediffDef is null");
                return false;
            }

            if (p.Map == null || !gameComponent.BipolarPawnLastCheckedTicks.ContainsKey(p.thingIDNumber))
            {
                gameComponent.BipolarPawnLastCheckedTicks[p.thingIDNumber] = 0;
            }

            int lastCheckedDay = gameComponent.BipolarPawnLastCheckedTicks[p.thingIDNumber];

            if (p.health.hediffSet.HasHediff(HediffDef.Named("Bipolar")))
            {
                if (lastCheckedDay == 0)
                {
                    gameComponent.BipolarPawnLastCheckedTicks[p.thingIDNumber] = GenLocalDate.DayOfYear(p.Map);
                }
            }

            if (!p.health.hediffSet.HasHediff(bipolarDef))
            {
              //  Log.Message($"{p.Name.ToStringShort} does not have bipolar trait");
                return ThoughtState.Inactive;
            }

            // If it's before 18:00, return the cached thought state immediately
            if (GenLocalDate.HourOfDay(p.Map) < 18)
            {
                if (MindMattersGameComponent.lastSeverity.ContainsKey(p))
                {
                  //  Log.Message($"Returning cached thought state for {p.Name.ToStringShort}");
                    return ThoughtStateForSeverity(MindMattersGameComponent.lastSeverity[p]);
                }
                else
                {
                 //   Log.Message($"{p.Name.ToStringShort} has no cached thought state");
                    return ThoughtState.Inactive;
                }
            }

            // Check for null bipolar Hediff in pawn
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(bipolarDef);
            if (hediff == null)
            {
                Log.Error("Bipolar Hediff is null for pawn " + p.LabelShort);
                return false;
            }

            if (!MindMattersGameComponent.lastSeverity.ContainsKey(p))
                MindMattersGameComponent.lastSeverity[p] = hediff.Severity;

            // Check if the current day is different from the last checked day and after 18:00
            if (GenLocalDate.DayOfYear(p.Map) != lastCheckedDay && GenLocalDate.HourOfDay(p.Map) >= 18)
            {
                gameComponent.BipolarPawnLastCheckedTicks[p.thingIDNumber] = GenLocalDate.DayOfYear(p.Map);

                //int newStage = WeightedRandomStage();
                int newStage = WeightedRandomStageWithInertia(p);

                //  float newSeverity = (newStage / (float)(StageWeights.Length - 1)) * 0.99f;
                float newSeverity = (newStage / (float)StageWeights.Length) * 0.99f;

                if (newSeverity <= 0)
                    newSeverity = 0.01f;

                if (newSeverity >= 1)
                    newSeverity = 0.99f;

             //   Log.Message($"Pawn {p.Name} has new severity {newSeverity} and stage {newStage}");

                hediff.Severity = newSeverity;
                MindMattersGameComponent.lastSeverity[p] = newSeverity;
            }

            return ThoughtStateForSeverity(MindMattersGameComponent.lastSeverity[p]);
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
        private int WeightedRandomStageWithInertia(Pawn p)
        {
            int currentStage = 0;

            // If the pawn has a last severity, find out its stage.
            if (MindMattersGameComponent.lastSeverity.ContainsKey(p))
                currentStage = Mathf.RoundToInt(MindMattersGameComponent.lastSeverity[p] * (StageWeights.Length - 1));

            // Possible stages are initially all stages.
            List<int> possibleStages = Enumerable.Range(0, StageWeights.Length).ToList();

            // Remove stages more than one step from the current stage.
            possibleStages.RemoveAll(stage => Math.Abs(stage - currentStage) > 1);

            // Sum weights of the possible stages.
            int totalWeight = possibleStages.Sum(stage => StageWeights[stage]);

            // Pick a random number within the total weight.
            int randomNumber = Rand.RangeInclusive(1, totalWeight);

            // Choose the stage based on the random number.
            foreach (int stage in possibleStages)
            {
                if (randomNumber <= StageWeights[stage])
                    return stage;
                randomNumber -= StageWeights[stage];
            }

            // Fallback in case something went wrong.
            return currentStage;
        }



    }
}
