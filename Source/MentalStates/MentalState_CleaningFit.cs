using RimWorld;
using Verse;
using Verse.AI;

namespace MindMatters
{
    public class MentalState_CleaningFit : MentalState
    {
        private int cooldownTicks = 0; // Cooldown timer after cleaning
        private int checkFrequency = 600; // Check for filth every 100 ticks
        private int ticksSinceLastCheck = 0; // Counter to keep track of ticks since the last filth check

        public override void MentalStateTick()
        {
            base.MentalStateTick();

            // Increment the counter
            ticksSinceLastCheck++;

            // If we're on cooldown or it's not time to check for filth yet, do nothing else this tick.
            if (cooldownTicks > 0 || ticksSinceLastCheck < checkFrequency)
            {
                if (cooldownTicks > 0) cooldownTicks--;
                return;
            }

            // Reset the counter
            ticksSinceLastCheck = 0;

            // If the pawn is currently performing a Clean job, exit
            if (pawn.jobs.curJob != null && (pawn.jobs.curJob.def == JobDefOf.Clean || !CanInterruptCurrentJob(pawn)))
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
                ResetCooldown(); // Reset the cooldown after starting a cleaning job.
            }
        }

        private void ResetCooldown()
        {
            cooldownTicks = 1800; // Or however long you want the cooldown to be.
        }

        private bool CanInterruptCurrentJob(Pawn pawn)
        {
            // If the pawn is eating, resting, or cleaning, you might not want to interrupt.
            if (pawn.jobs.curJob.def == JobDefOf.Ingest || pawn.jobs.curJob.def == JobDefOf.LayDown || pawn.jobs.curJob.def == JobDefOf.Clean)
            {
                return false;
            }

            // Add more conditions as needed.

            return true; // By default, any other job can be interrupted
        }
    }
}
