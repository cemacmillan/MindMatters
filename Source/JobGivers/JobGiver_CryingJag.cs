using Verse;
using Verse.AI;
using RimWorld;

namespace MindMatters
{
    public class JobGiver_CryingJag : JobGiver_Wander
    {
        public JobGiver_CryingJag()
        {
            this.wanderRadius = 7f;
            this.ticksBetweenWandersRange = new IntRange(300, 600);
            this.locomotionUrgency = LocomotionUrgency.Walk;
            this.wanderDestValidator = WanderDestValidator;
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return pawn.Position;
        }

        private static bool WanderDestValidator(Pawn pawn, IntVec3 loc, IntVec3 root)
        {
            // Ensure the destination is within the home area to prevent wandering off the map
            return pawn.Map.areaManager.Home[loc];
        }
    }
}