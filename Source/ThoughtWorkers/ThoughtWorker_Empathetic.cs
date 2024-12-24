using System.Linq; // Add this directive
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Empathetic : ThoughtWorker
    {
        private const float NearbyDistanceSquared = 64f; // 8 * 8 cells
        private const float HappinessThreshold = 0.6f;
        private const float UnhappinessThreshold = 0.4f;
        private const int TickInterval = 60;

        private int tickCounter = 0;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.story.traits.HasTrait(MindMattersTraitDef.Empathetic))
            {
                return ThoughtState.Inactive;
            }

            tickCounter++;
            if (tickCounter < TickInterval)
            {
                return ThoughtState.Inactive; // No need to check for mood transitions yet
            }

            tickCounter = 0; // Reset the tick counter

            // Find all pawns within the defined nearby distance
            var nearbyPawns = p.Map.mapPawns.AllPawnsSpawned
                .Where(pawn =>
                    pawn != p &&
                    pawn.Position.DistanceToSquared(p.Position) <= NearbyDistanceSquared &&
                    (pawn.Faction == p.Faction || pawn.HostFaction == p.Faction || pawn.guest != null))
                .ToList();

            if (nearbyPawns.Count == 0)
            {
                return ThoughtState.Inactive; // No nearby pawns, no thought
            }

            float totalMood = 0f;

            foreach (var pawn in nearbyPawns)
            {
                if (pawn.needs != null && pawn.needs.mood != null)
                {
                    totalMood += pawn.needs.mood.CurLevel;
                }
            }

            float averageMood = totalMood / nearbyPawns.Count;

            if (averageMood > HappinessThreshold)
            {
                return ThoughtState.ActiveAtStage(1); // Feeling shared joy
            }
            else if (averageMood < UnhappinessThreshold)
            {
                return ThoughtState.ActiveAtStage(2); // Feeling shared pain
            }
            else
            {
                return ThoughtState.ActiveAtStage(0); // Feeling shared emotions
            }
        }
    }
}