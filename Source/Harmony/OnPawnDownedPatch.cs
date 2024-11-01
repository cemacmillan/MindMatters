using HarmonyLib;
using MindMatters;
using Verse;

[HarmonyPatch(typeof(Pawn_HealthTracker))]
[HarmonyPatch("MakeDowned")]
public static class Pawn_HealthTracker_MakeDowned_Patch
{
    static void Prefix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff)
    {
        // Get the Pawn from the health tracker
        Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

        // Check if the pawn is null, not dead, spawned, a human-like colonist
        if (pawn != null && !pawn.Dead && pawn.Spawned && pawn.IsColonist && pawn.RaceProps.Humanlike)
        {
            // Get the ExperienceManager from the Current.Game object
            var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();

            // If ExperienceManager is not null, then notify it that the pawn has been downed
            if (experienceManager != null)
            {
                Log.Message("Downed and adding experience.");
                experienceManager.OnPawnDowned(pawn);
            }
            else
            {
                Log.Error("ExperienceManager is null.");
            }
        }
        else
        {
            //Log.Message("Something else was wrong.");
        }
    }
}