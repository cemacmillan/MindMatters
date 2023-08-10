using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace MindMatters
{
    public class ThoughtWorker_SocialActivity : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.story.traits.HasTrait(MindMattersTraits.Socialite))
            {
                //Log.Message("Pawn is not a Socialite.");
                return ThoughtState.Inactive;
            }

            // Check if they're attending a party or a ceremony.
            if (p.CurJobDef == JobDefOf.StandAndBeSociallyActive || p.CurJobDef == JobDefOf.SpectateCeremony)
            {
               // Log.Message("Pawn is attending a social event.");
                return ThoughtState.ActiveAtStage(0);
            }

            // Check if they're having a deep conversation.
            if (p.jobs.curDriver is JobDriver_ChatWithPrisoner || p.jobs.curDriver is JobDriver_SocialRelax)
            {
                // Log.Message("Pawn is having a deep conversation.");
                return ThoughtState.ActiveAtStage(1);
            }

            // Check if they're wearing high-quality clothes.
            if (p.apparel.WornApparel.Any(a => a.def.useHitPoints && a.HitPoints >= a.MaxHitPoints * 0.9f))
            {
                // Log.Message("Pawn is wearing high-quality clothes.");
                return ThoughtState.ActiveAtStage(2);
            }

            // Check if they're in a dining or recreation room.
            Room room = p.GetRoom();
            if (room != null && (room.Role == RoomRoleDefOf.DiningRoom || room.Role == RoomRoleDefOf.RecRoom))
            {
                // Log.Message("Pawn is in a social space.");
                return ThoughtState.ActiveAtStage(3);
            }

            // If none of the conditions are met, the thought is inactive.
            // Log.Message("No conditions met for Socialite activity.");
            return ThoughtState.Inactive;
        }
    }
}