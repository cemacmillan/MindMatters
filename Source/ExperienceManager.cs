using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Noise;

namespace MindMatters
{
    public class Experience : IExposable
    {
        public string EventType;
        public ExperienceValency Valency;
        public int Timestamp;

        // Parameterless constructor for deserialization
        public Experience() { }

        public Experience(string eventType, ExperienceValency valency)
        {
            EventType = eventType;
            Valency = valency;
            Timestamp = Find.TickManager.TicksGame;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref EventType, "EventType");
            Scribe_Values.Look(ref Valency, "Valency");
            Scribe_Values.Look(ref Timestamp, "Timestamp");
        }
    }

    public enum ExperienceValency
    {
        Positive,
        Negative,
        Neutral
    }
    public class MindMattersExperienceComponent : GameComponent
    {
       // private Dictionary<Pawn, List<Experience>> pawnExperiences;
        private Dictionary<Pawn, List<Experience>> pawnExperiences = new Dictionary<Pawn, List<Experience>>();


        public MindMattersExperienceComponent(Game game)
        {
           // pawnExperiences = new Dictionary<Pawn, List<Experience>>();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

           
            // Every quarter day (3 hours)
            if (Find.TickManager.TicksGame % 7500 == 0)
            {
                int expireThreshold = Find.TickManager.TicksGame - GenDate.TicksPerDay;
                ExpireOldExperiences(expireThreshold);
            }

            // Check for therapy-related thoughts once a day
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                CheckForTherapyThoughtsAndAddExperiences();
            }
        }
        
        private void ExpireOldExperiences(int expireThreshold, bool isReloaded = false)
        {
            foreach (var pair in pawnExperiences.ToList()) // Work on a copy to avoid collection modification
            {
                Pawn pawn = pair.Key;
                List<Experience> experiences = pair.Value;

                if (pawn == null || experiences == null)
                {
                    // Cleanup null references
                    pawnExperiences.Remove(pawn);
                    continue;
                }

                // Remove expired experiences
                experiences.RemoveAll(exp => exp.Timestamp < expireThreshold);

                // Optional: Log debug information
                if (experiences.Count == 0)
                {
                    pawnExperiences.Remove(pawn); // Remove empty lists
                    // MindMattersUtilities.DebugLog($"ExpireOldExperiences: Removed all experiences for {pawn.LabelShort}.");
                }
                else
                {
                    // MindMattersUtilities.DebugLog($"ExpireOldExperiences: {experiences.Count} experiences remain for {pawn.LabelShort}.");
                }
            }

            if (!isReloaded)
            {
                // MindMattersUtilities.DebugLog("ExpireOldExperiences: Expiration completed for all pawns.");
            }
            else
            {
               // MindMattersUtilities.DebugLog("ExpireOldExperiences: Expiration deferred due to game reload.");
            }
        }
        
        public List<Experience> GetOrCreateExperiences(Pawn pawn)
        {
            if (pawn == null)
            {
                MindMattersUtilities.DebugWarn("GetOrCreateExperiences: Called with a null pawn.");
                return new List<Experience>(); // Return an empty list for safety
            }

            if (!pawnExperiences.TryGetValue(pawn, out var experiences))
            {
                // If the pawn doesn't have a list of experiences yet, create one
                experiences = new List<Experience>();
                pawnExperiences[pawn] = experiences;
                // MindMattersUtilities.DebugLog($"GetOrCreateExperiences: Created new experience list for {pawn.LabelShort}.");
            }

            return experiences;
        }
        
        public void AddExperience(Pawn pawn, Experience experience)
        {
            if (pawn == null)
            {
                MindMattersUtilities.DebugWarn("AddExperience: Called with a null pawn.");
                return;
            }

            if (experience == null)
            {
                MindMattersUtilities.DebugWarn("AddExperience: Called with a null experience.");
                return;
            }

            // Use the accessor to safely retrieve or create the experiences list
            var experiences = GetOrCreateExperiences(pawn);
            experiences.Add(experience);

            MindMattersUtilities.DebugLog($"Added experience {experience.EventType} ({experience.Valency}) for {pawn.LabelShort}");
        }

        private void CheckForTherapyThoughtsAndAddExperiences()
        {
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned) // Iterating over all spawned colonists across maps
            {
                List<Thought_Memory> pawnMemories = pawn.needs.mood.thoughts.memories.Memories;

                foreach (Thought_Memory memory in pawnMemories)
                {
                    if (IsTherapyRelated(memory))
                    {
                        // Create an Experience and pass it to "Mind Matters"
                        Experience newExperience = new Experience("Therapy", ExperienceValency.Positive );
                        MindMattersUtilities.AddExperience(pawn, "Therapy", newExperience.Valency);
                        break; // If you only want one experience per therapy session, exit loop early
                    }
                }
            }
        }


        private bool IsTherapyRelated(Thought thought)
        {
            return thought.def.defName == "TherapyRelieved";
        }



        public void OnPawnDowned(Pawn pawn)
        {
            if (pawn == null)
            {
                MindMattersUtilities.DebugWarn("OnPawnDowned called with a null pawn.");
                return;
            }

            // Create a new "PawnDowned" experience
            Experience pawnDownedExperience = new Experience("PawnDowned", ExperienceValency.Negative);

            // Use AddExperience method for consistent experience handling
            AddExperience(pawn, pawnDownedExperience);

            // Optional DebugLog for tracking
            MindMattersUtilities.DebugLog($"OnPawnDowned: {pawn.LabelShort} has been downed. Experience added: {pawnDownedExperience.EventType} ({pawnDownedExperience.Valency}).");
        }

        public void OnPawnKilled(Pawn killer)
        {
            if (killer == null)
            {
                MindMattersUtilities.DebugWarn("OnPawnKilled: Called with a null killer.");
                return;
            }

            MindMattersUtilities.DebugLog($"OnPawnKilled: {killer.Name} has killed a pawn.");

            // Create a new "PawnKilled" experience
            var experience = new Experience("PawnKilled", ExperienceValency.Neutral);

            // Use AddExperience to add the new experience
            AddExperience(killer, experience);

            MindMattersUtilities.DebugLog($"OnPawnKilled: {killer.Name} now has {GetOrCreateExperiences(killer).Count} experiences recorded.");
        }

        public List<Experience> GetPawnExperiences(Pawn pawn)
        {
            if (pawn == null)
            {
                MindMattersUtilities.DebugWarn("GetPawnExperiences: Called with a null pawn.");
                return new List<Experience>();
            }

            // Use the accessor to safely retrieve or create the experiences for the pawn
            return GetOrCreateExperiences(pawn);
        }
        
        public void OnColonistDied(Pawn colonist)
        {
            if (colonist == null)
            {
                MindMattersUtilities.DebugWarn("OnColonistDied: Called with a null colonist.");
                return;
            }

            List<Pawn> allColonists = PawnsFinder.AllMaps_FreeColonists.ToList();

            foreach (Pawn pawn in allColonists)
            {
                if (pawn == null)
                {
                    MindMattersUtilities.DebugWarn("OnColonistDied: Found a null pawn in the colonists list.");
                    continue;
                }

                if (pawn == colonist) continue;

                bool isRelated = pawn.relations?.RelatedPawns?.Contains(colonist) == true;
                string experienceType = isRelated ? "ColonistDiedRelation" : "ColonistDied";

                Experience newExperience = new Experience(experienceType, ExperienceValency.Negative);
                AddExperience(pawn, newExperience);
            }

            MindMattersUtilities.DebugLog($"OnColonistDied: Added death experiences for {allColonists.Count} colonists.");
        }

        public override void ExposeData()
        {
            base.ExposeData();

            List<Pawn> keys = new List<Pawn>();
            List<List<Experience>> values = new List<List<Experience>>();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                keys = pawnExperiences.Keys.ToList();
                values = pawnExperiences.Values.Select(x => new List<Experience>(x)).ToList();
            }

            Scribe_Collections.Look(ref keys, "keys", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    List<Experience> experiences = values[i];
                    Scribe_Collections.Look(ref experiences, $"values_{i}", LookMode.Deep);
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                values = new List<List<Experience>>(keys.Count);
                for (int i = 0; i < keys.Count; i++)
                {
                    List<Experience> experiences = null;
                    Scribe_Collections.Look(ref experiences, $"values_{i}", LookMode.Deep);
                    values.Add(experiences);
                }
                pawnExperiences = keys.Zip(values, (k, v) => new { k, v })
                                      .ToDictionary(x => x.k, x => x.v);
            }
        }
    }
}