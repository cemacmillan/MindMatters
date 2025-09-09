using HarmonyLib;
using RimWorld;
using Verse;

namespace MindMatters
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved")]
    public static class PawnStrippedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var pawn = __instance.pawn;
            if (pawn == null || apparel == null) return;
            
            // Check if pawn is being stripped (multiple items removed quickly or by force)
            // This is a simple check - in practice you might want more sophisticated detection
            if (pawn.IsPrisoner || pawn.Downed)
            {
                var experienceManager = Current.Game?.GetComponent<MindMattersExperienceComponent>();
                if (experienceManager != null)
                {
                    // Check if nudity is considered wrongful by ideology
                    // Use dynamic precept lookup since nudity precepts are in Ideology DLC
                    bool nudityWrongful = false;
                    if (pawn.Ideo != null)
                    {
                        var nudityMalePrecept = DefDatabase<PreceptDef>.GetNamedSilentFail("Nudity_Male_UncoveredGroinDisapproved");
                        var nudityFemalePrecept = DefDatabase<PreceptDef>.GetNamedSilentFail("Nudity_Female_UncoveredGroinDisapproved");
                        
                        nudityWrongful = (nudityMalePrecept != null && pawn.Ideo.HasPrecept(nudityMalePrecept)) ||
                                       (nudityFemalePrecept != null && pawn.Ideo.HasPrecept(nudityFemalePrecept));
                    }
                    
                    // Check for conflicting traits (e.g., Nudist)
                    bool hasConflictingTrait = pawn.story?.traits?.HasTrait(TraitDefOf.Nudist) == true;
                    
                    if (nudityWrongful && !hasConflictingTrait)
                    {
                        var experience = new Experience
                        {
                            EventType = "Stripped",
                            Valency = ExperienceValency.Humiliating,
                            Timestamp = Find.TickManager.TicksGame,
                            Flags = new System.Collections.Generic.HashSet<string> { "humiliation", "institutional", "nudity" }
                        };
                        
                        experienceManager.AddExperience(pawn, experience);
                        MMToolkit.DebugLog($"[MindMatters] Added 'Stripped' experience for {pawn.LabelShort}");
                    }
                }
            }
        }
    }
}
