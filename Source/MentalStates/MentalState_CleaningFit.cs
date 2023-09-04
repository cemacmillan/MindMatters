using RimWorld;
using Verse;
using Verse.AI;

namespace MindMatters
{
    public class MentalState_CleaningFit : MentalState
    {
        public override void MentalStateTick()
        {
            base.MentalStateTick();

            // If the pawn is currently performing a Clean job, exit
            if (pawn.jobs.curJob != null && pawn.jobs.curJob.def == JobDefOf.Clean)
            {
                return;
            }

            // Determine if the pawn's current job is interruptible
            if (pawn.jobs.curJob != null && !CanInterruptCurrentJob(pawn))
            {
                return;
            }

            // Get the closest filth item in the home area
            Filth filth = (Filth)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Filth), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999, x => pawn.Map.areaManager.Home[x.Position], null, 0, -1, false, RegionType.Set_Passable, false);

            if (filth != null)
            {
                // Interrupt the current job (if any)
                if (pawn.jobs.curJob != null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }

                // Create and start a new cleaning job for the filth item
                Job job = JobMaker.MakeJob(JobDefOf.Clean, filth);
                pawn.jobs.StartJob(job);
            }
        }

        private bool CanInterruptCurrentJob(Pawn pawn)
        {
            // For example, if the pawn is eating or resting, you might not want to interrupt.
            // Adjust the conditions based on which tasks you think are crucial.
            if (pawn.jobs.curJob.def == JobDefOf.Ingest || pawn.jobs.curJob.def == JobDefOf.LayDown)
            {
                return false;
            }

            // Add more conditions as needed.

            return true; // By default, any other job can be interrupted
        }
    }
}
