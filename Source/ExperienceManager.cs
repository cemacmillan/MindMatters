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
        public Dictionary<Pawn, List<Experience>> pawnExperiences = new Dictionary<Pawn, List<Experience>>();


        public MindMattersExperienceComponent(Game game)
        {
           // pawnExperiences = new Dictionary<Pawn, List<Experience>>();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            // Every quarter game day (6 hours)
            if (Find.TickManager.TicksGame % 15000 == 0)
            {
                int expireTime = Find.TickManager.TicksGame - GenDate.TicksPerDay;
                foreach (var pawnExperience in pawnExperiences)
                {
                    pawnExperience.Value.RemoveAll(e => e.Timestamp < expireTime);
                }
            }

            // Every full game day (24 hours)
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                CheckForTherapyThoughtsAndAddExperiences();
            }



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
            // Create a new "PawnDowned" experience
            Experience pawnDownedExperience = new Experience("PawnDowned",ExperienceValency.Negative);
            Log.Message($"OnPawnDowned: {pawn.Name} has been downed.");

            // Get the pawn's existing experiences, if any
            if (!pawnExperiences.TryGetValue(pawn, out var experiences))
            {
                // If the pawn doesn't have any experiences yet, create a new list
                experiences = new List<Experience>();
                pawnExperiences[pawn] = experiences;
            }

            // Add the "PawnDowned" experience to the pawn's list of experiences
            experiences.Add(pawnDownedExperience);
        }

        public void OnPawnKilled(Pawn killer)
        {
            // Log the event
            Log.Message($"OnPawnKilled: {killer.Name} has killed a pawn.");

            // Create a new "PawnKilled" experience
            Experience pawnKilledExperience = new Experience("PawnKilled",ExperienceValency.Neutral);

            // Get the killer's existing experiences, if any
            if (!pawnExperiences.TryGetValue(killer, out var experiences))
            {
                // If the killer doesn't have any experiences yet, create a new list
                experiences = new List<Experience>();
                pawnExperiences[killer] = experiences;
            }

            // Add the "PawnKilled" experience to the killer's list of experiences
            experiences.Add(pawnKilledExperience);
        }


        public List<Experience> GetPawnExperiences(Pawn pawn)
        {
            if (pawnExperiences.ContainsKey(pawn))
            {
                return pawnExperiences[pawn];
            }
            return new List<Experience>();
        }

        public void OnColonistDied(Pawn colonist)
        {
            if (colonist == null)
            {
                Log.Error("OnColonistDied called with null colonist.");
                return;
            }

            // Get all the colonists across all maps
            List<Pawn> allColonists = PawnsFinder.AllMaps_FreeColonists.ToList();

            // Add the experience to each colonist's list of experiences
            foreach (Pawn p in allColonists)
            {
                if (p == null)
                {
                    Log.Error("Null pawn in allColonists list.");
                    continue;
                }

                // Don't add the experience to the dead colonist
                if (p != colonist)
                {
                    // Get the colonist's list of experiences
                    if (!pawnExperiences.TryGetValue(p, out var experiences))
                    {
                        // If the colonist doesn't have a list of experiences yet, create a new one
                        experiences = new List<Experience>();
                        pawnExperiences[p] = experiences;
                    }

                    if (experiences == null)
                    {
                        Log.Error($"Experiences list for pawn {p.LabelShort} is null.");
                        continue;
                    }

                    // Check if the colonist was a relation
                    string experienceType = p.relations.RelatedPawns.Contains(colonist) ? "ColonistDiedRelation" : "ColonistDied";

                    // Add the experience to the colonist's list
                    experiences.Add(new Experience(experienceType, ExperienceValency.Negative));
                }
            }
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