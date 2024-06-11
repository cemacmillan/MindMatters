using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike")]
    public static class SomeoneDiedPatch
    {
        [HarmonyPostfix]
        public static void AppendThoughts(Pawn victim, DamageInfo? dinfo, PawnDiedOrDownedThoughtsKind thoughtsKind, ref List<IndividualThoughtToAdd> outIndividualThoughts)
        {
            if (victim?.Faction == null || victim.Faction.HostileTo(Faction.OfPlayer))
                return;

            var tenderHeartedTrait = TraitDef.Named("MM_TenderHearted");
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive)
            {
                if (pawn == null || pawn == victim)
                    continue;

                if (pawn.story?.traits == null || !pawn.story.traits.HasTrait(tenderHeartedTrait))
                    continue;

                if (pawn.relations == null)
                    continue;

                var opinionOfTheDeceased = pawn.relations.OpinionOf(victim);
                var stageIndex = Mathf.Clamp(opinionOfTheDeceased / 10, 0, 4);

                if (ThoughtDefOfMindMatters.MM_SomeoneDied == null)
                {
                    Log.Error("MindMatters_SomeoneDied ThoughtDef is null");
                    continue;
                }

                Thought_Memory newThought = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOfMindMatters.MM_SomeoneDied);
                if (newThought == null)
                {
                    Log.Error("Failed to create new Thought_Memory");
                    continue;
                }
                newThought.SetForcedStage(stageIndex);

                if (pawn.needs?.mood?.thoughts?.memories == null)
                {
                    Log.Error("Pawn's needs, mood, thoughts, or memories is null");
                    continue;
                }

                pawn.needs.mood.thoughts.memories.TryGainMemory(newThought);
            }
        }
    }
}