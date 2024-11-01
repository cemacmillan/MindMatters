using HarmonyLib;
using MindMatters;
using Verse;

[HarmonyPatch(typeof(Pawn))]
[HarmonyPatch("Kill")]
public static class Pawn_Kill_Patch
{
    static void Postfix(Pawn __instance, DamageInfo? dinfo)
    {
        // Check if the damage info is available and the instigator is a Pawn
        if (dinfo.HasValue && dinfo.Value.Instigator is Pawn instigator)
        {
            // Check if the instigator is a human-like colonist and the killed pawn (__instance) is also human-like
            if (instigator.IsColonist && instigator.RaceProps.Humanlike && __instance.RaceProps.Humanlike)
            {
                // Get the ExperienceManager from the Current.Game object
                var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();

                // Notify the ExperienceManager that the pawn has killed another pawn
                experienceManager?.OnPawnKilled(instigator);
            }
        }
    }
}
