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
        private static readonly int[] StageWeights = { 1, 2, 5, 2, 1 };
        private static readonly int TotalWeight = StageWeights.Sum();

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p == null)
            {
                Log.Error("Pawn object is null in CurrentStateInternal");
                return ThoughtState.Inactive;
            }

            HediffDef bipolarDef = HediffDef.Named("Bipolar");
            if (bipolarDef == null || !p.health.hediffSet.HasHediff(bipolarDef))
            {
                return ThoughtState.Inactive;
            }

            var gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();
            if (gameComponent == null)
            {
                Log.Error("MindMattersGameComponent not found.");
                return ThoughtState.Inactive;
            }

            if (!gameComponent.BipolarPawnLastCheckedTicks.TryGetValue(p.thingIDNumber, out int lastCheckedTick))
            {
                lastCheckedTick = Find.TickManager.TicksGame;
                gameComponent.BipolarPawnLastCheckedTicks[p.thingIDNumber] = lastCheckedTick;
            }

            if (Find.TickManager.TicksGame < lastCheckedTick + GenDate.TicksPerDay)
            {
                return gameComponent.lastSeverity.ContainsKey(p) ? ThoughtStateForSeverity(gameComponent.lastSeverity[p]) : ThoughtState.Inactive;
            }

            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(bipolarDef);
            if (hediff == null)
            {
                Log.Error("Bipolar Hediff is null for pawn " + p.LabelShort);
                return ThoughtState.Inactive;
            }

            float lastSeverity = gameComponent.lastSeverity.ContainsKey(p) ? gameComponent.lastSeverity[p] : hediff.Severity;

            gameComponent.BipolarPawnLastCheckedTicks[p.thingIDNumber] = Find.TickManager.TicksGame;

            int newStage = WeightedRandomStageWithInertia(p, lastSeverity);

            float newSeverity = (newStage / (float)StageWeights.Length) * 0.99f;
            newSeverity = Mathf.Clamp(newSeverity, 0.01f, 0.99f);

            LongEventHandler.QueueLongEvent(() =>
            {
                hediff.Severity = newSeverity;
                gameComponent.lastSeverity[p] = newSeverity;
                MMToolkit.DebugLog($"Setting new severity for pawn {p.Name.ToStringShort}: {newSeverity} (stage {newStage})");
            }, "UpdateBipolarSeverity", false, null);

            return ThoughtStateForSeverity(lastSeverity);
        }

        private ThoughtState ThoughtStateForSeverity(float severity)
        {
            if (severity > 1.0f || severity < 0.0f)
            {
                MMToolkit.GripeOnce($"Unexpected severity value: {severity}. Defaulting to stage 2.");
                return ThoughtState.ActiveAtStage(2);
            }
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

        private int WeightedRandomStageWithInertia(Pawn p, float lastSeverity)
        {
            int currentStage = Mathf.RoundToInt(lastSeverity * (StageWeights.Length - 1));

            List<int> possibleStages = Enumerable.Range(0, StageWeights.Length).ToList();

            possibleStages.RemoveAll(stage => Math.Abs(stage - currentStage) > 1);

            // Ensure all possible stages are within valid bounds
            possibleStages = possibleStages.Where(stage => stage >= 0 && stage < StageWeights.Length).ToList();

            if (!possibleStages.Any())
            {
                MMToolkit.GripeOnce("No valid stages for bipolar thought.");
                return currentStage;
            }

            int totalWeight = possibleStages.Sum(stage => StageWeights[stage]);

            int randomNumber = Rand.RangeInclusive(1, totalWeight);

            foreach (int stage in possibleStages)
            {
                if (randomNumber <= StageWeights[stage])
                    return stage;
                randomNumber -= StageWeights[stage];
            }

            // If somehow no stage was selected, return the current stage
            return currentStage;
        }
    }
}