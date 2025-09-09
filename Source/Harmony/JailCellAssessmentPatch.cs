using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class JailCellAssessmentPatch
    {
        private static System.Collections.Generic.Dictionary<int, int> lastAssessmentTick = new System.Collections.Generic.Dictionary<int, int>();
        
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.IsPrisoner) return;
            
            var currentTick = Find.TickManager.TicksGame;
            var pawnId = __instance.thingIDNumber;
            
            // Only assess once per day (60000 ticks = 1 day)
            if (lastAssessmentTick.TryGetValue(pawnId, out int lastTick) && 
                currentTick - lastTick < 60000)
            {
                return;
            }
            
            // Check if pawn is in a prison cell
            var room = __instance.GetRoom();
            if (room != null && room.IsPrisonCell)
            {
                var experienceManager = Current.Game?.GetComponent<MindMattersExperienceComponent>();
                if (experienceManager != null)
                {
                    // Assess the cell conditions
                    var cellSize = room.CellCount;
                    var cleanliness = room.GetStat(RoomStatDefOf.Cleanliness);
                    var beauty = room.GetStat(RoomStatDefOf.Beauty);
                    
                    ExperienceValency valency = ExperienceValency.Neutral;
                    var flags = new System.Collections.Generic.HashSet<string> { "prison", "assessment" };
                    
                    // Small cell = negative, large cell = positive
                    if (cellSize < 16) // 4x4 or smaller
                    {
                        valency = ExperienceValency.Negative;
                        flags.Add("cramped");
                    }
                    else if (cellSize > 25) // Larger than 5x5
                    {
                        valency = ExperienceValency.Positive;
                        flags.Add("spacious");
                    }
                    
                    // Poor conditions = negative
                    if (cleanliness < 0.5f || beauty < -2f)
                    {
                        valency = ExperienceValency.Negative;
                        flags.Add("poor_conditions");
                    }
                    
                    var experience = new Experience
                    {
                        EventType = "JailCellAssessment",
                        Valency = valency,
                        Timestamp = currentTick,
                        Flags = flags
                    };
                    
                    experienceManager.AddExperience(__instance, experience);
                    lastAssessmentTick[pawnId] = currentTick;
                    
                    MMToolkit.DebugLog($"[MindMatters] Added 'JailCellAssessment' experience for {__instance.LabelShort} (cell size: {cellSize}, cleanliness: {cleanliness:F2}, beauty: {beauty:F2})");
                }
            }
        }
    }
}
