using System;
using HarmonyLib;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch("GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Pawn __result)
        {
            if (__result != null && __result.RaceProps.Humanlike)
            {
                PersonalityMatrixHandler handler = new PersonalityMatrixHandler();
                PersonalityMatrix matrix = handler.CreatePersonalityMatrix(__result);

                // Log the matrix for debugging.
                //foreach (var dimension in matrix.GetDimensions())
                //{
                //    MMToolkit.DebugLog($"{__result.NameShortColored}'s {dimension.Key}: {dimension.Value}");
                //}
            }
        }
    }
}
