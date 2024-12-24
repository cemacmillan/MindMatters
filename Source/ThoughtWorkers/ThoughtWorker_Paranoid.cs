using System;
using System.Linq; // Add this directive
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Paranoid : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (!pawn.story.traits.HasTrait(MindMattersTraitDef.Paranoid))
            {
                return ThoughtState.Inactive;
            }

            // If the pawn is alone, they feel scared
            int numPawnsNearby = pawn.Map.mapPawns.AllPawnsSpawned.Count(p => p.Position.DistanceToSquared(pawn.Position) <= 9); // square of 3
            if (numPawnsNearby == 1)
            {
                return ThoughtState.ActiveAtStage(1);
            }

            // If there are strangers on the map, they feel uncomfortable
            bool strangersPresent = pawn.Map.mapPawns.AllPawnsSpawned.Any(p => p.Faction != Faction.OfPlayer && !p.NonHumanlikeOrWildMan());
            if (strangersPresent)
            {
                return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.Inactive;
        }
    }
}
