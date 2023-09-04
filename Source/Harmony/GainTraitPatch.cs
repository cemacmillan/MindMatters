using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(TraitSet))]
    [HarmonyPatch("GainTrait")]
    public static class TraitSet_GainTrait_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(TraitSet __instance, Trait trait)
        {
            Pawn pawn = AccessTools.Field(typeof(TraitSet), "pawn").GetValue(__instance) as Pawn;

            if (trait.def.defName == "MM_Bipolar" && !pawn.health.hediffSet.HasHediff(HediffDef.Named("Bipolar")))
            {
                Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("Bipolar"), pawn);
                pawn.health.AddHediff(hediff);

                // Register the pawn in the MindMattersGameComponent
                var gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();
                if (gameComponent != null)
                {
                    // Only do this if Map is not null
                    if (pawn.Map != null)
                    {
                        gameComponent.BipolarPawnLastCheckedTicks[pawn.thingIDNumber] = GenLocalDate.DayOfYear(pawn.Map);
                    }
                    // If the Map is null, we don't register the date yet
                }
                else
                {
                    Log.Error("MindMattersGameComponent not found.");
                }
            }
            else if (trait.def.defName == "Empathetic" && !pawn.health.hediffSet.HasHediff(HediffDef.Named("Empathetic")))
            {
                Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("Empathetic"), pawn);
                pawn.health.AddHediff(hediff);
            }
        }
    }
}