using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using static MindMatters.MindMattersUtilities;

namespace MindMatters
{
    public class ThoughtWorker_SocialActivity : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.story.traits.HasTrait(MindMattersTraitDef.Socialite))
            {
                //MindMattersUtilities.DebugLog("Pawn is not a Socialite.");
                return ThoughtState.Inactive;
            }

            // Check if they're attending a party or a ceremony.
            if (p.CurJobDef == JobDefOf.StandAndBeSociallyActive || p.CurJobDef == JobDefOf.SpectateCeremony)
            {
               // MindMattersUtilities.DebugLog("Pawn is attending a social event.");
                return ThoughtState.ActiveAtStage(0);
            }

            // Check if they're having a deep conversation.
            if (p.jobs.curDriver is JobDriver_ChatWithPrisoner || p.jobs.curDriver is JobDriver_SocialRelax)
            {
                // MindMattersUtilities.DebugLog("Pawn is having a deep conversation.");
                return ThoughtState.ActiveAtStage(1);
            }

            // Check if they're wearing high-quality clothes.
            if (p.apparel.WornApparel.Any(a => a.def.useHitPoints && a.HitPoints >= a.MaxHitPoints * 0.9f))
            {
                // MindMattersUtilities.DebugLog("Pawn is wearing high-quality clothes.");
                return ThoughtState.ActiveAtStage(2);
            }

            // Get the room the pawn is currently in
            Room room = p.GetRoom();
            RoomRoleDef roomRole = room?.Role;

            if (roomRole == RoomRoleDefOf_MindMatters.DiningRoom || roomRole == RoomRoleDefOf_MindMatters.RecRoom)
            {
                // Log a message for debugging purposes
                MindMattersUtilities.DebugLog("Pawn is in a social space.");
                return ThoughtState.ActiveAtStage(3);
            }

            // If none of the conditions are met, the thought is inactive.
            // MindMattersUtilities.DebugLog("No conditions met for Socialite activity.");
            return ThoughtState.Inactive;
        }
    }
}