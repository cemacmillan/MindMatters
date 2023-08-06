using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using System.Linq;

namespace MindMatters
{
    public static class MindMattersUtilities
    {
        public const float AloneDistanceSquared = 18f * 18f;  // Adjust this to match the "alone" radius

        public static bool IsPawnAlone(Pawn pawn)
        {
            MindMattersGameComponent gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();

            // If the pawn has never been alone or was last alone more than X ticks ago (adjust to match your desired frequency),
            // then calculate if the pawn is currently alone
            if (!gameComponent.PawnLastAloneTicks.ContainsKey(pawn.thingIDNumber) ||
                Find.TickManager.TicksGame - gameComponent.PawnLastAloneTicks[pawn.thingIDNumber] > 60)  // Adjust this to match your desired frequency
            {
                foreach (Pawn otherPawn in pawn.Map.mapPawns.AllPawnsSpawned)
                {
                    if (otherPawn != pawn && otherPawn.Position.DistanceToSquared(pawn.Position) <= AloneDistanceSquared)
                    {
                        return false;
                    }
                }

                // If we've gotten to this point, the pawn is alone, so update the last alone tick
                gameComponent.PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }

            // If the pawn was alone the last time we checked, consider them still alone
            return true;
        }

        public static bool IsPawnInSafeSituation(Pawn p)
        {
            int inSafeSituationThreshhold = 2500; // ticks 
            if (p.Map == null)
            {
                return false;
            }

            Area_Home homeArea = p.Map.areaManager.Home;

            // If pawn is outside the home area
            if (homeArea != null && !homeArea.ActiveCells.Contains(p.Position))
            {
                return false; // Considered unsafe when outside home area
            }

            int currentTick = Find.TickManager.TicksGame;

            // If the pawn was in a dangerous situation recently
            if (currentTick - p.mindState.lastMeleeThreatHarmTick < inSafeSituationThreshhold ||
                currentTick - p.mindState.lastEngageTargetTick < inSafeSituationThreshhold ||
                currentTick - p.mindState.lastAttackTargetTick < inSafeSituationThreshhold )
            {
                return false; // Considered unsafe when recently in danger
            }

            return true; // Otherwise, considered safe
        }
    }
}
