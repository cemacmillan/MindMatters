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
            MindMattersUtilities.DebugLog($"[CleaningFit] Pawn {pawn.Name} entering JobGiver_CleaningFit.");

            if (pawn == null || pawn.Map == null)
            {
                MindMattersUtilities.DebugLog("[CleaningFit] Pawn or Map is null.");
                return null;
            }

            // Define the validator for finding filth
            bool Validator(Thing t)
            {
                bool valid = t is Filth filth &&
                             !t.IsForbidden(pawn) &&
                             pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly);

                if (valid)
                {
                    MindMattersUtilities.DebugLog($"[CleaningFit] Valid filth found at {t.Position}.");
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
                MindMattersUtilities.DebugLog($"[CleaningFit] Assigning cleaning job for filth at {filth.Position}.");

                Job job = JobMaker.MakeJob(JobDefOf.Clean);
                job.AddQueuedTarget(TargetIndex.A, filth);
                job.playerForced = true;
                job.ignoreForbidden = true;
                job.expiryInterval = 2000;

                // Add nearby filth to the job's target queue
                int maxQueueSize = 15;
                Map map = filth.Map;
                Room room = filth.GetRoom();
                for (int i = 0; i < 100; i++)
                {
                    IntVec3 c2 = filth.Position + GenRadial.RadialPattern[i];
                    if (!c2.InBounds(map))
                    {
                        continue;
                    }
                    List<Thing> thingList = c2.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        Thing thing = thingList[j];
                        if (thing is Filth filth2 && filth2 != filth && Validator(filth2))
                        {
                            job.AddQueuedTarget(TargetIndex.A, filth2);
                        }
                    }
                    if (job.GetTargetQueue(TargetIndex.A).Count >= maxQueueSize)
                    {
                        break;
                    }
                }

                if (job.targetQueueA != null && job.targetQueueA.Count >= 5)
                {
                    job.targetQueueA.SortBy((LocalTargetInfo targ) => targ.Cell.DistanceToSquared(pawn.Position));
                }

                return job;
            }

            MindMattersUtilities.DebugLog("[CleaningFit] No filth found to clean.");
            // No filth found; return null to allow other ThinkNodes to run
            return null;
        }
    }
}