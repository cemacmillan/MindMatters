using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(ThoughtHandler))]
[HarmonyPatch("GetDistinctMoodThoughtGroups")]
// ReSharper disable once UnusedType.Global
#pragma warning disable CA1050
public static class Patch_GetDistinctMoodThoughtGroups
#pragma warning restore CA1050
{
    // Postfix patch
    [HarmonyPostfix]
    public static void Postfix(List<Thought> outThoughts)
    {
        if (outThoughts == null)
        {
            Log.Error("[Mind Matters] Patch_GetDistinctMoodThoughtGroups: outThoughts is null.");
            return;
        }

        for (int num = outThoughts.Count - 1; num >= 0; num--)
        {
            Thought other = outThoughts[num];
            if (other == null)
            {
                Log.Warning($"[Mind Matters] Patch_GetDistinctMoodThoughtGroups: Thought at index {num} is null. Skipping.");
                continue;
            }

            // Ensure the list size hasn't changed unexpectedly
            if (num >= outThoughts.Count)
            {
                Log.Warning($"[Mind Matters] Patch_GetDistinctMoodThoughtGroups: Index {num} is out of range for list size {outThoughts.Count}. Breaking loop.");
                break;
            }

            for (int i = 0; i < num; i++)
            {
                Thought current = outThoughts[i];
                if (current == null)
                {
                    Log.Warning($"[Mind Matters] Patch_GetDistinctMoodThoughtGroups: Thought at index {i} is null. Skipping.");
                    continue;
                }

                try
                {
                    if (current.GroupsWith(other))
                    {
                        MindMattersUtilities.DebugLog($"[Mind Matters] Thought {current.def.defName} (index {i}) groups with {other.def.defName} (index {num}). Total thoughts: {outThoughts.Count}");
                        break;
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[Mind Matters] Exception while checking GroupsWith for thoughts at indices {i} and {num}: {ex}");
                }
            }
        }
    }
}