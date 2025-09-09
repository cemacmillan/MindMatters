using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MindMatters;

[HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike")]
public static class SomeoneDiedPatch
{
    [HarmonyPostfix]
    public static void AppendThoughts(Pawn victim, DamageInfo? dinfo, PawnDiedOrDownedThoughtsKind thoughtsKind, ref List<IndividualThoughtToAdd> outIndividualThoughts)
    {
        // FIXED: Only process actual deaths, not downing events
        // This prevents the confusion where overdoses/fainting trigger "someone died" thoughts
        if (thoughtsKind != PawnDiedOrDownedThoughtsKind.Died)
        {
            // TODO: Handle downing events separately with appropriate near-death experiences
            // For now, we'll let the OnPawnDownedPatch handle downing events
            return;
        }

        // Skip if the victim is null or belongs to a hostile faction
        if (victim?.Faction == null || victim.Faction.HostileTo(Faction.OfPlayer))
            return;

        // Check for surgery or anesthesia
        if (victim.health != null && victim.health.hediffSet.HasHediff(HediffDefOf.Anesthetic))
        {
            MMToolkit.DebugLog($"[MindMatters] Skipping thought for {victim.LabelShort}: Undergoing surgery or anesthetized.");
            return;
        }

        // Check if the downing was caused by damage
        if (dinfo.HasValue)
        {
            // Exclude non-lethal damage or self-inflicted damage
            if (dinfo.Value.Def == DamageDefOf.SurgicalCut || dinfo.Value.Instigator == victim)
            {
                MMToolkit.DebugLog($"[MindMatters] Skipping thought for {victim.LabelShort}: Downed due to non-lethal cause (e.g., surgery or self-inflicted).");
                return;
            }

            // Check if the cause is combat-related
            if (dinfo.Value.Weapon != null || dinfo.Value.Instigator is Pawn)
            {
                MMToolkit.DebugLog($"[MindMatters] {victim.LabelShort} was downed in combat; processing thought.");
            }
        }

        var tenderHeartedTrait = TraitDef.Named("MM_TenderHearted");
        foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive)
        {
            // Skip if the pawn is null, the victim itself, or lacks the Tender-Hearted trait
            if (pawn == null || pawn == victim || pawn.story?.traits == null || !pawn.story.traits.HasTrait(tenderHeartedTrait))
                continue;

            // Skip if the pawn lacks a relations tracker
            if (pawn.relations == null)
                continue;

            // Determine the opinion of the deceased and check threshold
            var opinionOfTheDeceased = pawn.relations.OpinionOf(victim);
            if (opinionOfTheDeceased < -20) // Skip if they disliked the victim significantly
                continue;

            // Clamp the stage index based on opinion
            var stageIndex = Mathf.Clamp(opinionOfTheDeceased / 10, 0, 4);

            // Check the thought definition
            if (ThoughtDefOfMindMatters.MM_SomeoneDied == null)
            {
                Log.Error("[MindMatters] ThoughtDef 'MM_SomeoneDied' is null");
                continue;
            }

            // Create the thought
            var newThought = ThoughtMaker.MakeThought(ThoughtDefOfMindMatters.MM_SomeoneDied) as Thought_Memory;
            if (newThought == null)
            {
                Log.Error("[MindMatters] Failed to create Thought_Memory for 'MM_SomeoneDied'");
                continue;
            }

            // Set the stage for the thought
            newThought.SetForcedStage(stageIndex);

            // Ensure the pawn's memory tracker exists
            if (pawn.needs?.mood?.thoughts?.memories == null)
            {
                Log.Error($"[MindMatters] Pawn '{pawn.LabelShort}' has invalid mood or memory setup");
                continue;
            }

            // Apply the thought
            pawn.needs.mood.thoughts.memories.TryGainMemory(newThought, victim);
            MMToolkit.DebugLog($"[MindMatters] Added 'MM_SomeoneDied' to {pawn.LabelShort} for {victim.LabelShort}'s death.");
        }
    }
}