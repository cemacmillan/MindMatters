using RimWorld;
using Verse;
using HarmonyLib;
using System;

namespace MindMatters
{
    [HarmonyPatch(typeof(Building_Grave))]
    [HarmonyPatch("Notify_HauledTo")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(Thing), typeof(int) })]
    public static class Building_Grave_NotifyHauledTo_Patch_1_5
    {
        private const string ThoughtDefName = "MindMatters_FilledGraveTenderHearted";

        [HarmonyPostfix]
        public static void FillGraveThought(Building_Grave __instance, Pawn hauler, Thing thing, int count)
        {
            try
            {
                if (thing is Corpse corpse)
                {
                    if (hauler.story != null && hauler.story.traits.HasTrait(MindMattersTraitDef.TenderHearted))
                    {
                        ThoughtDef thoughtDef = ThoughtDef.Named(ThoughtDefName);
                        Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);

                        if (hauler.needs.mood != null)
                        {
                            if (hauler.needs.mood.CurLevel < 0.4f)
                            {
                                thought.SetForcedStage(0);
                            }
                            else if (hauler.needs.mood.CurLevel < 0.7f)
                            {
                                thought.SetForcedStage(1);
                            }
                            else
                            {
                                thought.SetForcedStage(2);
                            }
                        }

                        hauler.needs.mood.thoughts.memories.TryGainMemory(thought);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"MindMatters: Error in FillGraveThought patch (1.5). Exception: {ex}");
            }
        }
    }
}