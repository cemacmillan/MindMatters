using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace MindMatters
{
    public class MentalState_CleaningFit : MentalState
    {
        private int cooldownTicks = 0; // Cooldown timer after cleaning
        private int checkFrequency = 600; // Check for filth every 600 ticks (10 seconds)
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

            // If we cannot interrupt the current job, exit
            if (!CanInterruptCurrentJob(pawn))
            {
                return;
            }

            // End the current job properly
            if (pawn.jobs.curJob != null)
            {
                Log.Message($"[{pawn.Name}] Ending current job: {pawn.jobs.curJob.def.defName}");
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: true);
                pawn.jobs.ClearQueuedJobs();
            }

            // Define the validator for filth
            bool Validator(Thing t)
            {
                if (t.Map != pawn.Map)
                {
                    return false;
                }

                if (t.IsForbidden(pawn))
                {
                    return false;
                }

                if (!pawn.CanReserve(t))
                {
                    return false;
                }

                if (!pawn.Map.areaManager.Home[t.Position])
                {
                    return false;
                }

                if (!pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    return false;
                }

                return true;
            }

            // Get the closest filth item in the home area
            Filth filth = (Filth)GenClosest.ClosestThingReachable(
                root: pawn.Position,
                map: pawn.Map,
                thingReq: ThingRequest.ForGroup(ThingRequestGroup.Filth),
                peMode: PathEndMode.ClosestTouch,
                traverseParams: TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                maxDistance: 60f,
                validator: Validator,
                customGlobalSearchSet: null,
                searchRegionsMin: 0,
                searchRegionsMax: -1,
                forceAllowGlobalSearch: false,
                traversableRegionTypes: RegionType.Set_Passable,
                ignoreEntirelyForbiddenRegions: false
            );

            if (filth != null && filth.Position.IsValid && filth.Map == pawn.Map)
            {
                // Create and start a new cleaning job for the filth item
                Job job = JobMaker.MakeJob(JobDefOf.Clean, filth);

                // Start the new job with appropriate parameters
                pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true);

                ResetCooldown(); // Reset the cooldown after starting a cleaning job.

                // Log the cleaning action
                Log.Message($"[{pawn.Name}] Starting new cleaning job at {filth.Position}.");
            }
            else
            {
                // Find a random cell within the home area to wander
                IntVec3 wanderCell = RCellFinder.RandomWanderDestFor(
                    pawn: pawn,
                    root: pawn.Position,
                    radius: 10f,
                    validator: (Pawn p, IntVec3 c, IntVec3 root) => p.Map.areaManager.Home[c] && c.Standable(p.Map),
                    maxDanger: Danger.Some,
                    canBashDoors: false // Optional, include if you want to specify
                );

                if (wanderCell.IsValid)
                {
                    Job wanderJob = JobMaker.MakeJob(JobDefOf.GotoWander, wanderCell);
                    wanderJob.expiryInterval = 4000; // Adjust as needed

                    // Start the new wander job
                    pawn.jobs.StartJob(wanderJob, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true);

                    // Log the wandering action
                    Log.Message($"[{pawn.Name}] Starting new wander job to {wanderCell}.");
                }
                else
                {
                    // If no valid cell is found, log a warning
                    Log.Warning($"{pawn.Name} could not find a valid wander cell during CleaningFit.");
                }
            }
        }

        private void ResetCooldown()
        {
            cooldownTicks = 1800; // Or however long you want the cooldown to be.
        }

        private bool CanInterruptCurrentJob(Pawn pawn)
        {
            if (pawn.jobs.curJob == null)
            {
                return true;
            }
            
            // Do not interrupt player-forced jobs
            if (pawn.jobs.curJob.playerForced)
            {
                return false;
            }

            // Do not interrupt important jobs
            if (pawn.jobs.curJob.def == JobDefOf.Ingest ||
                pawn.jobs.curJob.def == JobDefOf.LayDown ||
                pawn.jobs.curJob.def == JobDefOf.Clean ||
                pawn.jobs.curJob.def == JobDefOf.Flee)
            {
                return false;
            }

            // Add any other jobs you don't want to interrupt

            return true;
        }
    }
}