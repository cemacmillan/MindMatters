using System.Text;
using RimWorld;
using Verse;

namespace MindMatters
{
    /// <summary>
    /// Debug commands for testing the psyche system
    /// </summary>
    public static class PsycheDebugCommands
    {
        //[DebugAction("MindMatters", "Show Psyche Stats", actionType = DebugActionType.Action)]
        public static void ShowPsycheStats()
        {
            var psycheSystem = Current.Game.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null)
            {
                Log.Message("[MindMatters] Psyche system not found");
                return;
            }

            var stats = psycheSystem.GetStatistics();
            Log.Message($"[MindMatters] Psyche Statistics:\n{stats}");
        }

        //[DebugAction("MindMatters", "Show Pawn Psyche", actionType = DebugActionType.Action)]
        public static void ShowPawnPsyche()
        {
            var selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault(p => p != null);
            if (selectedPawn == null)
            {
                Log.Message("[MindMatters] No pawn selected");
                return;
            }

            var psycheSystem = Current.Game.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null)
            {
                Log.Message("[MindMatters] Psyche system not found");
                return;
            }

            var psyche = psycheSystem.GetOrCreatePsyche(selectedPawn);
            if (psyche == null)
            {
                Log.Message($"[MindMatters] No psyche found for {selectedPawn.LabelShort}");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[MindMatters] Psyche for {selectedPawn.LabelShort}:");
            sb.AppendLine($"Dominant Mode: {psyche.DominantMode}");
            sb.AppendLine($"Resting Persona: {psyche.GetRestingPersona()}");
            sb.AppendLine("Mode Weights:");
            
            for (int i = 0; i < psyche.ModeWeights.Length; i++)
            {
                var mode = (PsychologicalMode)i;
                sb.AppendLine($"  {mode}: {psyche.ModeWeights[i]:P1}");
            }

            Log.Message(sb.ToString());
        }

        //[DebugAction("MindMatters", "Nudge Pawn Psyche", actionType = DebugActionType.Action)]
        public static void NudgePawnPsyche()
        {
            var selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault(p => p != null);
            if (selectedPawn == null)
            {
                Log.Message("[MindMatters] No pawn selected");
                return;
            }

            var psycheSystem = Current.Game.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null)
            {
                Log.Message("[MindMatters] Psyche system not found");
                return;
            }

            var psyche = psycheSystem.GetOrCreatePsyche(selectedPawn);
            
            // Randomly nudge a mode
            var randomMode = Rand.Range(0, PsychologicalModeExtensions.Count);
            var nudgeAmount = Rand.Range(-0.2f, 0.2f);
            
            psyche.Nudge(randomMode, nudgeAmount);
            
            Log.Message($"[MindMatters] Nudged {selectedPawn.LabelShort}'s {((PsychologicalMode)randomMode)} mode by {nudgeAmount:F2}");
        }

        //[DebugAction("MindMatters", "Add Test Experience", actionType = DebugActionType.Action)]
        public static void AddTestExperience()
        {
            var selectedPawn = Find.Selector.SelectedPawns.FirstOrDefault(p => p != null);
            if (selectedPawn == null)
            {
                Log.Message("[MindMatters] No pawn selected");
                return;
            }

            var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();
            if (experienceManager == null)
            {
                Log.Message("[MindMatters] Experience manager not found");
                return;
            }

            // Create a test experience
            var testExperience = new Experience("TestEvent", ExperienceValency.Positive);
            testExperience.Flags.Add("test");
            testExperience.Flags.Add("debug");

            experienceManager.AddExperience(selectedPawn, testExperience);
            
            Log.Message($"[MindMatters] Added test experience to {selectedPawn.LabelShort}");
        }
    }
}
