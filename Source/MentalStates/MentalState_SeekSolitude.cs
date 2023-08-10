using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace MindMatters
{
    public class MentalState_SeekSolitude : MentalState
    {
        public override void MentalStateTick()
        {
            base.MentalStateTick();

            // If pawn is not alone and not already performing a GotoWander job, find a new spot to go to
            if (!MindMattersUtilities.IsPawnAlone(pawn) && pawn.CurJobDef != JobDefOf.GotoWander && pawn.pather.Moving)
            {
                // Get a random cell on the map
                IntVec3 loc = CellFinder.RandomCell(pawn.Map);

                // Check if the cell is walkable
                if (loc.Walkable(pawn.Map))
                {
                    // End current job and give the pawn a job to go to the new location
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptOptional, true);
                    pawn.jobs.StartJob(new Job(JobDefOf.GotoWander, loc), JobCondition.None, null, false, true, null, null, false, false);
                }
            }
        }
    }
}

