using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace MindMatters
{
    public class JobGiver_CleaningFit : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            MMToolkit.DebugLog($"[CleaningFit] Pawn {pawn.Name} entering JobGiver_CleaningFit.", MMToolkit.DebugLevel.Basic);

            if (pawn == null || pawn.Map == null)
            {
                MMToolkit.DebugLog("[CleaningFit] Pawn or Map is null.", MMToolkit.DebugLevel.Basic);
                return null;
            }

            // Define the validator for finding filth
            bool Validator(Thing t)
            {
                bool valid = t is Filth filth &&
                             !t.IsForbidden(pawn) &&
                             pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly);

                // Only log valid filth at verbose level to reduce spam
                if (valid)
                {
                    MMToolkit.DebugLog($"[CleaningFit] Valid filth found at {t.Position}.", MMToolkit.DebugLevel.Verbose);
                }

                return valid;
            }

            // Find the closest filth
            Thing filth = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Filth),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                9999f,
                Validator);

            if (filth != null)
            {
                MMToolkit.DebugLog($"[CleaningFit] Assigning cleaning job for filth at {filth.Position}.", MMToolkit.DebugLevel.Basic);

                Job job = JobMaker.MakeJob(JobDefOf.Clean);
                job.AddQueuedTarget(TargetIndex.A, filth);
                job.playerForced = true;
                job.ignoreForbidden = true;
                job.expiryInterval = 2000;

                // Add nearby filth to the job's target queue
                int maxQueueSize = 15;
                Map map = filth.Map;
                Room room = filth.GetRoom();
                
                // More efficient approach: collect all nearby filth first, then validate
                var nearbyFilth = new List<Thing>();
                for (int i = 0; i < 100; i++)
                {
                    IntVec3 c2 = filth.Position + GenRadial.RadialPattern[i];
                    if (!c2.InBounds(map))
                    {
                        continue;
                    }
                    
                    List<Thing> thingList = c2.GetThingList(map);
                    foreach (Thing thing in thingList)
                    {
                        if (thing is Filth filth2 && filth2 != filth)
                        {
                            nearbyFilth.Add(filth2);
                        }
                    }
                    
                    if (nearbyFilth.Count >= maxQueueSize * 2) // Collect more than needed for validation
                    {
                        break;
                    }
                }

                // Validate and add to job queue
                foreach (Thing filth2 in nearbyFilth)
                {
                    if (job.GetTargetQueue(TargetIndex.A).Count >= maxQueueSize)
                    {
                        break;
                    }
                    
                    // Double-check the filth still exists and is valid
                    if (filth2 != null && filth2.Spawned && Validator(filth2))
                    {
                        job.AddQueuedTarget(TargetIndex.A, filth2);
                    }
                }

                if (job.targetQueueA != null && job.targetQueueA.Count >= 5)
                {
                    job.targetQueueA.SortBy((LocalTargetInfo targ) => targ.Cell.DistanceToSquared(pawn.Position));
                }

                MMToolkit.DebugLog($"[CleaningFit] Job created with {job.GetTargetQueue(TargetIndex.A)?.Count ?? 0} targets.", MMToolkit.DebugLevel.Basic);
                return job;
            }

            MMToolkit.DebugLog("[CleaningFit] No filth found to clean.", MMToolkit.DebugLevel.Basic);
            // No filth found; return null to allow other ThinkNodes to run
            return null;
        }
    }
}