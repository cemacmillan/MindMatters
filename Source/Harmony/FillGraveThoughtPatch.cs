using RimWorld;
using Verse;
using HarmonyLib;

namespace MindMatters
{
    [HarmonyPatch(typeof(Building_Grave), nameof(Building_Grave.Notify_HauledTo))]
    public static class Building_Grave_NotifyHauledTo_Patch
    {
        private const string ThoughtDefName = "MindMatters_FilledGraveTenderHearted";

        [HarmonyPostfix]
        public static void FillGraveThought(Building_Grave __instance, Pawn hauler, Thing thing, int count)
        {
            // Ensure the thing is a corpse before proceeding
            if (thing is Corpse corpse)
            {
                Log.Message("Entered FillGraveThought");
                Log.Message($"Hauler: {hauler.Name}");
                Log.Message($"Has TenderHearted Trait: {hauler.story.traits.HasTrait(MindMattersTraits.TenderHearted)}");

                if (hauler.story != null && hauler.story.traits.HasTrait(MindMattersTraits.TenderHearted))
                {
                    Log.Message("Trying to add tender-hearted thought");
                    ThoughtDef thoughtDef = ThoughtDef.Named(ThoughtDefName);
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);

                    // Set the stage of the thought based on the hauler's mood
                    if (hauler.needs.mood != null)
                    {
                        if (hauler.needs.mood.CurLevel < 0.4f)
                        {
                            Log.Message("Setting stage 0");
                            thought.SetForcedStage(0);
                        }
                        else if (hauler.needs.mood.CurLevel < 0.7f)
                        {
                            Log.Message("Setting stage 1");
                            thought.SetForcedStage(1);
                        }
                        else
                        {
                            Log.Message("Setting stage 2");
                            thought.SetForcedStage(2);
                        }
                    }

                    hauler.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }
        }
    }
}