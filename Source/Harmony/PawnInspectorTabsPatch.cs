using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(ThingDef), "ResolveReferences")]
    public static class PawnInspectorTabsPatch
    {
        public static void Postfix(ThingDef __instance)
        {
            // Only add the Psyche Map tab to pawns
            if (__instance.thingClass == typeof(Pawn))
            {
                if (__instance.inspectorTabsResolved == null)
                {
                    __instance.inspectorTabsResolved = new List<InspectTabBase>();
                }
                
                // Add the Psyche Map tab if it's not already there
                bool hasPsycheTab = false;
                foreach (var tab in __instance.inspectorTabsResolved)
                {
                    if (tab is ITab_PsycheMap)
                    {
                        hasPsycheTab = true;
                        break;
                    }
                }
                
                if (!hasPsycheTab)
                {
                    __instance.inspectorTabsResolved.Add(new ITab_PsycheMap());
                }
            }
        }
    }
}
