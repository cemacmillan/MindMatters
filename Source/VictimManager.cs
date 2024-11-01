using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MindMatters
{
    public class MindMattersVictimManager
    {
        public Pawn DesignatedVictim { get; private set; }

        private Dictionary<Pawn, int> lastBlameTicks = new Dictionary<Pawn, int>();

        private static MindMattersVictimManager _instance;
        public static MindMattersVictimManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MindMattersVictimManager();
                }
                return _instance;
            }
        }

        public void DesignateNewVictim()
        {
            // Get a list of all colonist pawns
            var allPawns = PawnsFinder.AllMaps_FreeColonistsSpawned;

            // If there are 4 or fewer pawns, set DesignatedVictim to null and return
            if (allPawns.Count() <= 4)
            {
                DesignatedVictim = null;
                return;
            }

            var badTraitNames = new List<string>
            {
                "Abrasive",
                "TooSmart",
                "Pyromaniac",
                "Wimp",
                "Gourmand",
                "Slothful",
                "Relaxed",
                "VTE_Lazy" // Vanilla Traits Expanded - Lazy
            };

            var badTraits = badTraitNames
                .Select(name => DefDatabase<TraitDef>.GetNamedSilentFail(name))
                .Where(def => def != null)
                .ToList();

            // Select pawns with bad traits and calculate their scores
            var potentialVictims = allPawns
                .Select(pawn => new
                {
                    Pawn = pawn,
                    Score = badTraits.Count(trait => pawn.story.traits.HasTrait(trait))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            // If no potential victims, set DesignatedVictim to null and return
            if (!potentialVictims.Any())
            {
                DesignatedVictim = null;
                return;
            }

            // Take the top 3 potential victims
            var topVictims = potentialVictims.Take(3).ToList();

            // Roll a die to determine if there will be a victim this quadrum
            var roll = Rand.Value; // Generates a random float between 0 and 1
            if (roll < 1 / 3f)
            {
                DesignatedVictim = null;
                Log.Message("Scapegoat: Did not choose a scapegoat");
            }
            else
            {
                // Weight the selection towards pawns with higher scores
                DesignatedVictim = topVictims.RandomElementByWeight(x => x.Score).Pawn;
                Log.Message($"Scapegoat: {DesignatedVictim.Label}");
            }

            // Add the victim thought to the designated victim, if there is one
            if (DesignatedVictim != null)
            {
                DesignatedVictim.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("MM_DesignatedVictim"));
                Messages.Message($"It seems {DesignatedVictim.Label} is the new scapegoat.", DesignatedVictim, MessageTypeDefOf.NeutralEvent, true);
            } else
            {
                Messages.Message("No scapegoat has emerged.", MessageTypeDefOf.NeutralEvent);

            }

            // Clear the blame tracking dictionary when a new victim is designated
            lastBlameTicks.Clear();
        }

        public bool IsScapegoat(Pawn pawn)
        {
            return pawn == DesignatedVictim;
        }

        public bool AlreadyBlamedThisCycle(Pawn pawn)
        {
            if (lastBlameTicks.TryGetValue(pawn, out var lastBlameTick))
            {
                // If the pawn blamed the scapegoat less than 15 days (3600000 ticks) ago, return true
                return Find.TickManager.TicksGame - lastBlameTick < 3600000;
            }
            else
            {
                // If the pawn has never blamed the scapegoat, return false
                return false;
            }
        }

        public void RecordBlame(Pawn pawn)
        {
            lastBlameTicks[pawn] = Find.TickManager.TicksGame;
        }
    }
}