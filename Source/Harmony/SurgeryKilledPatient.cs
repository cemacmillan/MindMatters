using Verse;
using RimWorld;
using HarmonyLib;

namespace MindMatters
{
    [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
    public static class Recipe_Surgery_FailPatch
    {
        private const string ThoughtDefName = "MindMatters_KilledPatientTenderHearted";

        [HarmonyPostfix]
        public static void TenderHeartedThought(bool __result, Pawn surgeon, Pawn patient)
        {
            if (surgeon.needs.mood != null && __result && patient.Dead)
            {
                ThoughtDef thoughtDef = ThoughtDef.Named(ThoughtDefName);
                Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef, null);

                thought.pawn = patient; // Assign the patient to the thought's pawn field

                // Find the appropriate stage based on the surgeon's opinion of the patient
                int opinion = surgeon.relations.OpinionOf(patient);
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

                surgeon.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }
    }
}