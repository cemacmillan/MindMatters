using HarmonyLib;
using MindMatters;
using Verse;

[HarmonyPatch(typeof(Pawn))]
[HarmonyPatch("Kill")]
public static class Pawn_Died_Patch
{
    static void Postfix(Pawn __instance)
    {
        // Check if the pawn is a colonist
        if (__instance.IsColonist)
        {
            // Get the ExperienceManager from the Current.Game object
            var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();

            // Notify the ExperienceManager that a colonist has died
            experienceManager?.OnColonistDied(__instance);
        }
    }
}
