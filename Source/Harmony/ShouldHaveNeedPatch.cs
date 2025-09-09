using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters;

[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
static class Patch_Pawn_NeedsTracker_ShouldHaveNeed
{
    static bool Prefix(Pawn ___pawn, NeedDef nd, ref bool __result)
    {
        // Skip logic for non-dynamic needs
        if (!typeof(DynamicNeed).IsAssignableFrom(nd.needClass))
        {
            return true; // Proceed with vanilla logic
        }
        
        // MMToolkit.GripeOnce("ShouldHaveNeedPatch: We do pass IsAssignableFrom");

        // Handle dynamic needs logic
        bool shouldHave = MindMattersNeedsMgr.CanPawnHaveDynamicNeed(___pawn, nd);

        // Hospitality or QuestLodger-specific exclusions
        if (!CanPawnHaveDynamicNeed_Exclusions(___pawn))
        {
            shouldHave = false; // Exclude these pawns
            // MMToolkit.GripeOnce($"ShouldHaveNeedPatch: Excluding {___pawn.LabelShort}"); // Disabled - exclusions are for non-real pawns (templates, incomplete pawns)
        }

        if (shouldHave)
        {
            MMToolkit.DebugLog($"ShouldHaveNeedPatch: shouldHave == true: {___pawn.LabelShort} for {nd.defName}");
        }
        __result = shouldHave;
        return false; // Skip vanilla logic
    }

    private static bool CanPawnHaveDynamicNeed_Exclusions(Pawn pawn)
    {
        // Null or dead pawns are outright excluded.
        // Fix Me - Exclude other types of dead to the world by ascension, etc.
        if (pawn == null || pawn.Dead || pawn.IsMutant)
            return false;

        // Check mod setting: exclude guests
        // Ensure colony pawns (including slaves and prisoners) aren't mistakenly excluded
        if (!MindMattersMod.settings.NeedsApplyToGuests &&
            pawn.guest != null &&
            (pawn.guest.GuestStatus == GuestStatus.Guest || pawn.Faction != Faction.OfPlayer))
        {
            // Slaves, prisoners, and other colony pawns are exceptions
            if (pawn.IsSlave || pawn.IsPrisoner)
                return true; // Allow slaves and prisoners

            return false; // Exclude non-colony guests
        }

        // Explicitly exclude quest lodgers
        if (pawn.IsQuestLodger())
            return false;

        // Handle ambiguous cases
        // Free colonists and prisoners/slaves are allowed
        if (pawn.IsFreeColonist || pawn.IsSlave || pawn.IsPrisoner)
            return true;

        // Default fallback for other cases
        return false;
    }
}