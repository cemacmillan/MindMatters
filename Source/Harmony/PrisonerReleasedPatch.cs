using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class PrisonerReleasedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus)
        {
            // Access the private pawn field through reflection
            var pawnField = typeof(Pawn_GuestTracker).GetField("pawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pawn = pawnField?.GetValue(__instance) as Pawn;
            if (pawn == null) return;
            
            // Check if pawn was released from prison (was prisoner, now not)
            if (guestStatus != GuestStatus.Guest && __instance.IsPrisoner)
            {
                // Pawn is being released from prison
                var experienceManager = Current.Game?.GetComponent<MindMattersExperienceComponent>();
                if (experienceManager != null)
                {
                    var experience = new Experience
                    {
                        EventType = "PrisonerReleased",
                        Valency = ExperienceValency.Positive,
                        Timestamp = Find.TickManager.TicksGame,
                        Flags = new System.Collections.Generic.HashSet<string> { "freedom", "reintegration" }
                    };
                    
                    experienceManager.AddExperience(pawn, experience);
                    MMToolkit.DebugLog($"[MindMatters] Added 'PrisonerReleased' experience for {pawn.LabelShort}");
                }
            }
        }
    }
}
