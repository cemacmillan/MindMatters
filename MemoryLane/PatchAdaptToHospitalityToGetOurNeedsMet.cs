using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters;

[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
public static class PatchAdaptToGuestsForDynamicNeeds
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, NeedDef nd, Pawn ___pawn)
    {
        // If Hospitality is loaded, allow it to handle guests after our conditions
        if (ModLister.HasActiveModWithName("Hospitality"))
        {
            if (ApplyMindMattersLogic(nd, ___pawn, ref __result))
            {
                return false; // Skip the original method if we handle it
            }
            
            return true; // Let Hospitality handle the rest
        }

        // If Hospitality is NOT loaded, fully handle the logic here
        return ApplyMindMattersLogic(nd, ___pawn, ref __result) ? false : true;
    }

    private static bool ApplyMindMattersLogic(NeedDef nd, Pawn pawn, ref bool __result)
    {
        // Allow DynamicNeeds for guests
        if (GuestUtility.IsGuestOrEquivalent(pawn) &&
            typeof(IDynamicNeed).IsAssignableFrom(nd.needClass))
        {
            __result = true;
            return true; // Handled
        }

        // Exclude quest lodgers unless explicitly allowed
        if (GuestUtility.IsQuestLodger(pawn) &&
            typeof(IDynamicNeed).IsAssignableFrom(nd.needClass))
        {
            __result = false;
            return true; // Handled
        }

        return false; // Not handled
    }
}