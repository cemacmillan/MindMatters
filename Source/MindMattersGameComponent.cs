using Verse;
using System.Collections.Generic;
using RimWorld;
using System.Linq;

namespace MindMatters
{
    public class MindMattersGameComponent : GameComponent
    {
        public static MindMattersGameComponent Instance;

        public enum Mood
        {
            Happy,
            Unhappy,
            Neutral
        }

        public static Dictionary<Pawn, float> lastSeverity = new Dictionary<Pawn, float>();
        public static Dictionary<Pawn, Mood> PawnMoods = new Dictionary<Pawn, Mood>();

        public Dictionary<int, List<Thought>> PawnThoughtsCache = new Dictionary<int, List<Thought>>();
        public Dictionary<int, int> PawnThoughtsLastCheckedTicks = new Dictionary<int, int>();

        public Dictionary<int, int> BipolarPawnLastCheckedTicks = new Dictionary<int, int>();

        public Dictionary<int, int> PawnLastAloneTicks = new Dictionary<int, int>();

        public MindMattersGameComponent(Game game)
        {
            Instance = this;
            if (Instance == null)
            {
                Log.Error("MindMattersGameComponent instance is null after assignment in constructor.");
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager.TicksGame % 2500 == 0)  // Every day
            {
                var pawnThoughtsUpdates = new Dictionary<int, List<Thought>>();
                var pawnsToRemoveFromBipolarTicks = new List<int>();
                var pawnsToAddToBipolarTicks = new Dictionary<int, int>();

                foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned)
                {
                    if (pawn == null)
                    {
                        Log.Error("Found null pawn in AllMaps_FreeColonistsAndPrisonersSpawned list.");
                        continue;
                    }

                    List<Thought> currentThoughts = new List<Thought>();
                    pawn.needs.mood.thoughts.GetAllMoodThoughts(currentThoughts);

                    pawnThoughtsUpdates[pawn.thingIDNumber] = currentThoughts;

                    if (pawn.health == null || pawn.health.hediffSet == null)
                    {
                        Log.Error("Pawn " + pawn.Name + "'s health or hediffSet is null.");
                        continue;
                    }

                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Bipolar")))
                    {
                        if (!BipolarPawnLastCheckedTicks.ContainsKey(pawn.thingIDNumber))
                        {
                            pawnsToAddToBipolarTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                        }
                    }
                    else
                    {
                        if (BipolarPawnLastCheckedTicks.ContainsKey(pawn.thingIDNumber))
                        {
                            pawnsToRemoveFromBipolarTicks.Add(pawn.thingIDNumber);
                        }
                    }

                    if (MindMattersUtilities.IsPawnAlone(pawn))
                    {
                        PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                    }

                    if (!PawnThoughtsLastCheckedTicks.ContainsKey(pawn.thingIDNumber) ||
                        Find.TickManager.TicksGame - PawnThoughtsLastCheckedTicks[pawn.thingIDNumber] >= 600)
                    {
                        // Update the last checked tick
                        PawnThoughtsLastCheckedTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                    }
                }

                // Now apply the changes after all iterations are done.
                foreach (var pair in pawnThoughtsUpdates)
                {
                    PawnThoughtsCache[pair.Key] = pair.Value;
                }

                foreach (var pawnId in pawnsToRemoveFromBipolarTicks)
                {
                    BipolarPawnLastCheckedTicks.Remove(pawnId);
                }

                foreach (var pair in pawnsToAddToBipolarTicks)
                {
                    BipolarPawnLastCheckedTicks[pair.Key] = pair.Value;
                }
            }
        }

        public Thought GetThoughtForPawn(Pawn p)
        {
            if (PawnThoughtsCache.TryGetValue(p.thingIDNumber, out var pawnThoughts))
            {
                foreach (var thought in pawnThoughts)
                {
                    if (thought == null || thought.def == null || thought.def.stages == null ||
                        thought.CurStageIndex < 0 || thought.CurStageIndex >= thought.def.stages.Count)
                    {
                        Log.Warning($"Invalid stage index for thought in pawn {p.LabelShort}. Skipping...");
                        continue;
                    }

                    return thought;
                }
            }

            return null; // Or return an inactive/default thought here
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Save and load the dictionaries directly without splitting into keys and values
            Scribe_Collections.Look(ref BipolarPawnLastCheckedTicks, "BipolarPawnLastCheckedTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref PawnLastAloneTicks, "PawnLastAloneTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lastSeverity, "lastSeverity", LookMode.Reference, LookMode.Value);
            Scribe_Collections.Look(ref PawnMoods, "PawnMoods", LookMode.Reference, LookMode.Value);
            Scribe_Collections.Look(ref PawnThoughtsLastCheckedTicks, "PawnThoughtsLastCheckedTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref PawnThoughtsCache, "PawnThoughtsCache", LookMode.Value, LookMode.Reference);

            if (PawnMoods == null || lastSeverity == null || BipolarPawnLastCheckedTicks == null || PawnLastAloneTicks == null || PawnThoughtsCache == null || PawnThoughtsLastCheckedTicks == null)
            {
                Log.Error("Some dictionaries in MindMattersGameComponent are null after loading game data.");
            }
            else if (PawnMoods.Count == 0 || lastSeverity.Count == 0 || BipolarPawnLastCheckedTicks.Count == 0 || PawnLastAloneTicks.Count == 0 || PawnThoughtsCache.Count == 0 || PawnThoughtsLastCheckedTicks.Count == 0)
            {
                Log.Error("Some dictionaries in MindMattersGameComponent are empty after loading game data.");
            }
        }
    }
}
