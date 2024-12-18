using Verse;
using RimWorld;
using HarmonyLib;

namespace MindMatters;

[HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
public static class Recipe_Surgery_FailPatch
{
    private const string ThoughtDefName = "MindMatters_KilledPatientTenderHearted";

    [HarmonyPostfix]
    public static void TenderHeartedThought(bool __result, Pawn surgeon, Pawn patient)
    {
        // Exit early if the surgery didn't fail or the patient didn't die
        if (!__result || !patient.Dead)
            return;

        // Ensure the surgeon has mood and thoughts
        if (surgeon.needs?.mood?.thoughts?.memories == null)
            return;

        // Look up the thought definition
        var thoughtDef = ThoughtDef.Named(ThoughtDefName);
        if (thoughtDef == null)
        {
            Log.Error($"[MindMatters] Could not find ThoughtDef '{ThoughtDefName}'. Ensure the definition exists.");
            return;
        }

        // Create the thought memory
        var thought = ThoughtMaker.MakeThought(thoughtDef) as Thought_Memory;
        if (thought == null)
        {
            Log.Error("[MindMatters] Failed to create Thought_Memory for 'MindMatters_KilledPatientTenderHearted'.");
            return;
        }

        // Assign the thought stage based on opinion of the patient
        var opinion = surgeon.relations?.OpinionOf(patient) ?? 0; // Default opinion to 0 if relations are null
        if (opinion >= 20)
        {
            thought.SetForcedStage(2);
        }
        else if (opinion >= -20)
        {
            thought.SetForcedStage(1);
        }
        else
        {
            thought.SetForcedStage(0);
        }

        // Add the memory to the surgeon
        surgeon.needs.mood.thoughts.memories.TryGainMemory(thought);

        // Debug log for confirmation
        MindMattersUtilities.DebugLog($"[MindMatters] Added 'KilledPatientTenderHearted' thought to {surgeon.LabelShort} after failing surgery on {patient.LabelShort}.");
    }
}