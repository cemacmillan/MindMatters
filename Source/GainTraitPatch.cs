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

            if (trait.def.defName == "Bipolar" && !pawn.health.hediffSet.HasHediff(HediffDef.Named("Bipolar")))
            {
                Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("Bipolar"), pawn);
                pawn.health.AddHediff(hediff);
                Log.Message($"Bipolar hediff added to {pawn.Name.ToStringShort}");
            }
        }
    }
}
