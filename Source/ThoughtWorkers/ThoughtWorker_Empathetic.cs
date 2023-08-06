using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Empathetic : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            try
            {
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

                HediffDef empatheticDef = HediffDef.Named("Empathetic");
                if (empatheticDef == null)
                {
                    Log.Error("Empathetic HediffDef is null");
                    return false;
                }

                const float happyPawnThreshold = 0.6f;
                const float unhappyPawnThreshold = 0.4f;

                if (!p.health.hediffSet.HasHediff(empatheticDef))
                {
                    return ThoughtState.Inactive;
                }

                Thought thought = gameComponent.GetThoughtForPawn(p);
                if (thought == null)
                {
                    return ThoughtState.Inactive;
                }

                int happyCount = 0;
                int unhappyCount = 0;

                if (thought.def.defName == "Empathetic")
                {
                    return ThoughtState.Inactive;
                }

                if (thought.MoodOffset() > happyPawnThreshold)
                {
                    happyCount++;
                }
                else if (thought.MoodOffset() < unhappyPawnThreshold)
                {
                    unhappyCount++;
                }

                Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(empatheticDef);
                if (hediff == null)
                {
                    Log.Error("Empathetic Hediff is null for pawn " + p.LabelShort);
                    return false;
                }

                if (happyCount > unhappyCount && happyCount >= 1)
                {
                    hediff.Severity = 0.5f;
                    MindMattersGameComponent.PawnMoods[p] = MindMattersGameComponent.Mood.Happy;
                    return ThoughtState.ActiveAtStage(1);
                }
                else if (unhappyCount > happyCount && unhappyCount >= 1)
                {
                    hediff.Severity = 1f;
                    MindMattersGameComponent.PawnMoods[p] = MindMattersGameComponent.Mood.Unhappy;
                    return ThoughtState.ActiveAtStage(2);
                }
                else
                {
                    hediff.Severity = 0.01f;
                    MindMattersGameComponent.PawnMoods[p] = MindMattersGameComponent.Mood.Neutral;
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception caught in CurrentStateInternal: {ex}");
                return ThoughtState.Inactive;
            }
        }
    }
}
