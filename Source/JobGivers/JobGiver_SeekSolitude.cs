using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace MindMatters
{
    public class JobGiver_SeekSolitude : ThinkNode_JobGiver
    {
        private const int SafeMargin = 25;
        private const int MaxWanderRadius = 20;
        private const int MaxAttempts = 10;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return null;
            }

            // If the pawn is already alone, have them wait
            if (IsPawnAlone(pawn))
            {
                // Check if the pawn is already waiting
                if (pawn.CurJobDef == JobDefOf.Wait || pawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
                {
                    return null;
                }

                Job waitJob = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                waitJob.expiryInterval = 2500; // Adjust as needed
                waitJob.checkOverrideOnExpire = true;
                return waitJob;
            }
            else
            {
                // Find a spot away from other pawns
                IntVec3 targetCell = FindSolitudeSpot(pawn);

                if (targetCell.IsValid)
                {
                    Job goToJob = JobMaker.MakeJob(JobDefOf.GotoWander, targetCell);
                    goToJob.locomotionUrgency = LocomotionUrgency.Walk;
                    goToJob.expiryInterval = 2000;
                    goToJob.checkOverrideOnExpire = true;
                    return goToJob;
                }
                else
                {
                    // Fallback to wandering within home area
                    Job wanderJob = JobMaker.MakeJob(JobDefOf.GotoWander);
                    wanderJob.locomotionUrgency = LocomotionUrgency.Walk;
                    wanderJob.expiryInterval = 2000;
                    wanderJob.checkOverrideOnExpire = true;
                    return wanderJob;
                }
            }
        }

        private bool IsPawnAlone(Pawn pawn)
        {
            Map map = pawn.Map;
            List<Pawn> otherPawns = map.mapPawns.AllPawnsSpawned.Where(p => p != pawn && p.Faction == pawn.Faction).ToList();
            foreach (Pawn otherPawn in otherPawns)
            {
                if (otherPawn.Position.InHorDistOf(pawn.Position, 10f)) // Adjust distance as needed
                {
                    return false;
                }
            }
            return true;
        }

        private IntVec3 FindSolitudeSpot(Pawn pawn)
        {
            Map map = pawn.Map;
            for (int i = 0; i < MaxAttempts; i++)
            {
                IntVec3 randomCell = CellFinder.RandomClosewalkCellNear(pawn.Position, map, MaxWanderRadius);
                if (IsCellSafeFromEdges(randomCell, map) && IsCellAwayFromOthers(randomCell, pawn))
                {
                    return randomCell;
                }
            }
            return IntVec3.Invalid;
        }

        private bool IsCellSafeFromEdges(IntVec3 cell, Map map)
        {
            return cell.x >= SafeMargin &&
                   cell.z >= SafeMargin &&
                   cell.x < map.Size.x - SafeMargin &&
                   cell.z < map.Size.z - SafeMargin;
        }

        private bool IsCellAwayFromOthers(IntVec3 cell, Pawn pawn)
        {
            Map map = pawn.Map;
            List<Pawn> otherPawns = map.mapPawns.AllPawnsSpawned.Where(p => p != pawn && p.Faction == pawn.Faction).ToList();
            foreach (Pawn otherPawn in otherPawns)
            {
                if (cell.InHorDistOf(otherPawn.Position, 10f)) // Adjust distance as needed
                {
                    return false;
                }
            }
            return true;
        }
    }
}