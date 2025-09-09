using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "HealthTick")]
    public static class PawnWakingUpPatch
    {
        private static System.Collections.Generic.Dictionary<int, bool> wasSleeping = new System.Collections.Generic.Dictionary<int, bool>();
        
        [HarmonyPostfix]
        public static void Postfix(Pawn_HealthTracker __instance)
        {
            // Access the private pawn field through reflection
            var pawnField = typeof(Pawn_HealthTracker).GetField("pawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pawn = pawnField?.GetValue(__instance) as Pawn;
            if (pawn == null) return;
            
            var pawnId = pawn.thingIDNumber;
            var currentlySleeping = pawn.CurJob?.def == JobDefOf.LayDown || 
                                   pawn.CurJob?.def == JobDefOf.LayDownResting ||
                                   pawn.CurJob?.def == JobDefOf.Wait_WithSleeping;
            
            // Check if pawn just woke up (was sleeping, now not)
            if (wasSleeping.TryGetValue(pawnId, out bool wasSleepingBefore) && 
                wasSleepingBefore && !currentlySleeping)
            {
                // Pawn just woke up naturally
                var experienceManager = Current.Game?.GetComponent<MindMattersExperienceComponent>();
                if (experienceManager != null)
                {
                    var experience = new Experience
                    {
                        EventType = "WakingUp",
                        Valency = ExperienceValency.Positive,
                        Timestamp = Find.TickManager.TicksGame,
                        Flags = new System.Collections.Generic.HashSet<string> { "consciousness", "homeostasis", "reset" }
                    };
                    
                    experienceManager.AddExperience(pawn, experience);
                    MMToolkit.DebugLog($"[MindMatters] Added 'WakingUp' experience for {pawn.LabelShort}");
                }
            }
            
            // Update sleeping state
            wasSleeping[pawnId] = currentlySleeping;
        }
    }
}
