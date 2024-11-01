using RimWorld;
using Verse;
using HarmonyLib;
using System;

namespace MindMatters
{
    [HarmonyPatch(typeof(Building_Grave), "Notify_CorpseBuried")]
    public static class Building_Grave_Notify_CorpseBuried_Patch_1_4
    {
        private const string ThoughtDefName = "MindMatters_FilledGraveTenderHearted";

        [HarmonyPostfix]
        public static void FillGraveThought(Building_Grave __instance, Pawn worker)
        {
            try
            {
                Log.Message("Entered FillGraveThought");
                Log.Message($"Worker: {worker.Name}");
                Log.Message($"Has TenderHearted Trait: {worker.story.traits.HasTrait(MindMattersTraits.TenderHearted)}");

                if (worker.story != null && worker.story.traits.HasTrait(MindMattersTraits.TenderHearted))
                {
                    Log.Message("Trying to add tender-hearted thought");
                    ThoughtDef thoughtDef = ThoughtDef.Named(ThoughtDefName);
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);

                    if (worker.needs.mood != null)
                    {
                        if (worker.needs.mood.CurLevel < 0.4f)
                        {
                            Log.Message("Setting stage 0");
                            thought.SetForcedStage(0);
                        }
                        else if (worker.needs.mood.CurLevel < 0.7f)
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

                    worker.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }
            catch (Exception ex)
            {
                Log.Message($"MindMatters: Not establishing connection. Exception: {ex.Message}");
            }
        }
    }
}