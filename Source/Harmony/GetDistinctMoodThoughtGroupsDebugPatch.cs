using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(ThoughtHandler))]
[HarmonyPatch("GetDistinctMoodThoughtGroups")]
public static class Patch_GetDistinctMoodThoughtGroups
{
    // Postfix patch
    [HarmonyPostfix]
    public static void Postfix(List<Thought> outThoughts)
    {
        for (int num = outThoughts.Count - 1; num >= 0; num--)
        {
            Thought other = outThoughts[num];
            for (int i = 0; i < num; i++)
            {
                if (outThoughts[i].GroupsWith(other))
                {
                    // Log current index and list size
                    Log.Message($"Current index: {num}, outThoughts size: {outThoughts.Count}");
                    break;
                }
            }
        }
    }
}

