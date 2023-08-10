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

            // If pawn has nothing to do, find something to clean
            if (pawn.jobs.curJob == null || pawn.jobs.curJob.def != JobDefOf.Clean)
            {
                // Get the closest filth item in the home area
                Filth filth = (Filth)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Filth), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999, x => pawn.Map.areaManager.Home[x.Position], null, 0, -1, false, RegionType.Set_Passable, false);

                if (filth != null)
                {
                    // Create a new cleaning job for the filth item
                    Job job = JobMaker.MakeJob(JobDefOf.Clean, filth);
                    pawn.jobs.StartJob(job);
                }
            }
        }
    }
}
