using System.Collections.Generic;
using Verse;

namespace MindMatters
{
    /// <summary>
    /// Main psyche management system that extends the existing MindMatters experience system.
    /// This bridges the existing ExperienceManager with the new per-pawn psyche system.
    /// </summary>
    public class MindMattersPsyche : GameComponent
    {
        private Dictionary<Pawn, PawnPsyche> pawnPsyches = new Dictionary<Pawn, PawnPsyche>();
        private bool isInitialized = false;

        public MindMattersPsyche(Game game) : base()
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (!isInitialized)
            {
                Initialize();
                isInitialized = true;
            }

            // Process psyche updates for all pawns
            ProcessPsycheUpdates();
            
            // Test the system once after initialization (for debugging)
            if (Find.TickManager?.TicksGame == 1000) // After 1000 ticks
            {
                TestPsycheSystem();
            }
        }

        /// <summary>
        /// Get or create a psyche for a specific pawn
        /// </summary>
        public PawnPsyche GetOrCreatePsyche(Pawn pawn)
        {
            if (pawn == null) return null;

            if (!pawnPsyches.TryGetValue(pawn, out var psyche))
            {
                psyche = new PawnPsyche();
                pawnPsyches[pawn] = psyche;
                
                MindMattersUtilities.DebugLog($"Created new psyche for {pawn.LabelShort}");
            }

            return psyche;
        }

        /// <summary>
        /// Get existing psyche for a pawn (returns null if not found)
        /// </summary>
        public PawnPsyche GetPsyche(Pawn pawn)
        {
            if (pawn == null) return null;
            pawnPsyches.TryGetValue(pawn, out var psyche);
            return psyche;
        }

        /// <summary>
        /// Process a new experience for a pawn's psyche
        /// </summary>
        public void ProcessExperience(Pawn pawn, MindMattersInterface.Experience experience)
        {
            if (pawn == null || experience == null) return;

            var psyche = GetOrCreatePsyche(pawn);
            psyche.ProcessExperience(experience);

            MindMattersUtilities.DebugLog($"Processed experience {experience.EventType} ({experience.Valency}) for {pawn.LabelShort}'s psyche");
        }

        /// <summary>
        /// Get all pawns with active psyches
        /// </summary>
        public IEnumerable<Pawn> GetPawnsWithPsyches()
        {
            return pawnPsyches.Keys;
        }

        /// <summary>
        /// Get psychological statistics for debugging
        /// </summary>
        public PsycheStatistics GetStatistics()
        {
            var stats = new PsycheStatistics();
            
            foreach (var kvp in pawnPsyches)
            {
                var pawn = kvp.Key;
                var psyche = kvp.Value;
                
                if (pawn == null || psyche == null) continue;

                stats.TotalPawns++;
                
                // Track mode distribution
                var dominantMode = psyche.DominantMode;
                if (stats.ModeDistribution.ContainsKey(dominantMode))
                {
                    stats.ModeDistribution[dominantMode]++;
                }
                else
                {
                    stats.ModeDistribution[dominantMode] = 1;
                }
                
                // Track mode combinations
                var persona = psyche.GetRestingPersona();
                if (stats.PersonaDistribution.ContainsKey(persona))
                {
                    stats.PersonaDistribution[persona]++;
                }
                else
                {
                    stats.PersonaDistribution[persona] = 1;
                }
            }
            
            return stats;
        }

        /// <summary>
        /// Test method to verify the psyche system is working
        /// </summary>
        public void TestPsycheSystem()
        {
            MindMattersUtilities.DebugLog("=== PSYCHE SYSTEM TEST ===");
            
            var stats = GetStatistics();
            MindMattersUtilities.DebugLog($"Total pawns with psyches: {stats.TotalPawns}");
            
            if (stats.TotalPawns > 0)
            {
                MindMattersUtilities.DebugLog("Mode Distribution:");
                foreach (var kvp in stats.ModeDistribution)
                {
                    MindMattersUtilities.DebugLog($"  {kvp.Key}: {kvp.Value}");
                }
                
                MindMattersUtilities.DebugLog("Persona Distribution:");
                foreach (var kvp in stats.PersonaDistribution)
                {
                    MindMattersUtilities.DebugLog($"  {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                MindMattersUtilities.DebugLog("No pawns with psyches found!");
            }
            
            MindMattersUtilities.DebugLog("=== END PSYCHE TEST ===");
        }

        private void Initialize()
        {
            // Subscribe to experience events from the main ExperienceManager
            var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();
            if (experienceManager != null)
            {
                experienceManager.OnExperienceAdded += OnExperienceAdded;
                MindMattersUtilities.DebugLog("Psyche system initialized and connected to ExperienceManager");
            }
            else
            {
                MindMattersUtilities.DebugWarn("Psyche system initialized but ExperienceManager not found");
            }
        }

        private void OnExperienceAdded(Pawn pawn, MindMattersInterface.Experience experience)
        {
            ProcessExperience(pawn, experience);
        }

        private void ProcessPsycheUpdates()
        {
            // Process psyche updates for all active pawns
            var activePawns = Find.CurrentMap?.mapPawns?.AllPawnsSpawned ?? new List<Pawn>();
            
            foreach (var pawn in activePawns)
            {
                var psyche = GetPsyche(pawn);
                if (psyche != null)
                {
                    psyche.Tick(Find.TickManager.TicksGame);
                    psyche.ApplyDynamicNeedsCoupling(pawn);
                }
            }

            // Clean up psyches for pawns that no longer exist
            CleanupDeadPawns();
        }

        private void CleanupDeadPawns()
        {
            var toRemove = new List<Pawn>();
            
            foreach (var kvp in pawnPsyches)
            {
                var pawn = kvp.Key;
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                {
                    toRemove.Add(pawn);
                }
            }
            
            foreach (var pawn in toRemove)
            {
                pawnPsyches.Remove(pawn);
                MindMattersUtilities.DebugLog($"Cleaned up psyche for {pawn?.LabelShort ?? "null pawn"}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            
            // Convert dictionary to lists for serialization
            var pawnList = new List<Pawn>();
            var psycheList = new List<PawnPsyche>();
            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                foreach (var kvp in pawnPsyches)
                {
                    pawnList.Add(kvp.Key);
                    psycheList.Add(kvp.Value);
                }
            }
            
            Scribe_Collections.Look(ref pawnList, "pawnList", LookMode.Reference);
            Scribe_Collections.Look(ref psycheList, "psycheList", LookMode.Deep);
            Scribe_Values.Look(ref isInitialized, "isInitialized", false);
            
            // Reconstruct dictionary after loading
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                pawnPsyches.Clear();
                if (pawnList != null && psycheList != null && pawnList.Count == psycheList.Count)
                {
                    for (int i = 0; i < pawnList.Count; i++)
                    {
                        if (pawnList[i] != null && psycheList[i] != null)
                        {
                            pawnPsyches[pawnList[i]] = psycheList[i];
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Statistics about the psyche system for debugging and monitoring
    /// </summary>
    public class PsycheStatistics
    {
        public int TotalPawns = 0;
        public Dictionary<string, int> ModeDistribution = new Dictionary<string, int>();
        public Dictionary<string, int> PersonaDistribution = new Dictionary<string, int>();

        public override string ToString()
        {
            var result = $"Total Pawns: {TotalPawns}\n";
            result += "Mode Distribution:\n";
            foreach (var kvp in ModeDistribution)
            {
                result += $"  {kvp.Key}: {kvp.Value}\n";
            }
            result += "Persona Distribution:\n";
            foreach (var kvp in PersonaDistribution)
            {
                result += $"  {kvp.Key}: {kvp.Value}\n";
            }
            return result;
        }
    }
}
