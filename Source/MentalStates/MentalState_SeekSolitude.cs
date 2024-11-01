using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MindMatters
{
    public class MentalState_SeekSolitude : MentalState
    {
        private const int SafeMargin = 25;
        private const int MaxWanderRadius = 20;
        private const int MaxAttemptsToGatherSpot = 3;
        private int attemptsToGatherSpot = 0;

        public override void MentalStateTick()
        {
            base.MentalStateTick();

            List<Pawn> allPawns = Current.Game.GetComponent<MindMattersGameComponent>().GetAllPawns();
            if (MindMattersUtilities.IsPawnAlone(pawn,allPawns))
            {
                // If the pawn is alone and not already waiting or maintaining posture, give them a job to just stand
                if (pawn.CurJobDef != JobDefOf.Wait && pawn.CurJobDef != JobDefOf.Wait_MaintainPosture)
                {
                    Job standJob = new Job(JobDefOf.Wait, 2500); // Wait for 2500 ticks or adjust as required
                    pawn.jobs.StartJob(standJob, JobCondition.None, null, false, true, null, null, false, false);
                }
            }
            else
            {
                if (pawn.CurJobDef != JobDefOf.GotoWander && pawn.pather.Moving)
                {
                    IntVec3 wanderRoot = WanderUtility.GetColonyWanderRoot(pawn);

                    if (wanderRoot == pawn.Position && attemptsToGatherSpot < MaxAttemptsToGatherSpot)
                    {
                        attemptsToGatherSpot++;
                    }
                    else if (wanderRoot == pawn.Position || attemptsToGatherSpot >= MaxAttemptsToGatherSpot)
                    {
                        // Fallback to random wandering
                        wanderRoot = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, MaxWanderRadius, null);
                        attemptsToGatherSpot = 0;
                    }

                    // Check if the cell is safe from map edges
                    if (IsCellSafeFromEdges(wanderRoot, pawn.Map))
                    {
                        Job newJob = new Job(JobDefOf.GotoWander, wanderRoot);
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptOptional, true);
                        pawn.jobs.StartJob(newJob, JobCondition.None, null, false, true, null, null, false, false);

                        // For debugging
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"Target: {wanderRoot}", 12f);
                    }
                }
            }
        }

        private bool IsCellSafeFromEdges(IntVec3 cell, Map map)
        {
            return cell.x >= SafeMargin &&
                   cell.z >= SafeMargin &&
                   cell.x < map.Size.x - SafeMargin &&
                   cell.z < map.Size.z - SafeMargin;
        }

        public override void PostEnd()
        {
            base.PostEnd();

            // If the pawn is still waiting, stop that job
            if (pawn.CurJobDef == JobDefOf.Wait || pawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
