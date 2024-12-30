using HarmonyLib;
using System.Collections.Generic;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(PawnNeedsUIUtility), "UpdateDisplayNeeds")]
public static class Patch_UpdateDisplayNeeds
{
    static void Postfix(Pawn pawn)
    {
        if (pawn == null)
        {
            Log.Warning("[Mind Matters] UpdateDisplayNeeds: Pawn is null.");
            return;
        }

        if (pawn.needs == null || pawn.needs.AllNeeds == null)
        {
            Log.Warning($"[Mind Matters] UpdateDisplayNeeds: No needs for {pawn.LabelShort ?? "unknown"}.");
            return;
        }

        Log.Message($"[Mind Matters] UpdateDisplayNeeds: DisplayNeeds for {pawn.LabelShort ?? "unknown"}:");
        foreach (var need in pawn.needs.AllNeeds) // Assuming displayNeeds is public or accessible.
        {
            string needInfo = need?.def != null
                ? $"Need: {need.def.defName}, Priority: {need.def.listPriority}, Level: {need.CurLevel.ToString("F2")}"
                : "Invalid or null Need.";
            Log.Message($"  {needInfo}");
        }
    }
}