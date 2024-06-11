using RimWorld;
using Verse;

namespace MindMatters
{
    [DefOf]
    public static class ThoughtDefOfMindMatters
    {
        public static ThoughtDef MM_FilledGraveTenderHearted;
        public static ThoughtDef MM_KilledPatientTenderHearted;
        public static ThoughtDef MM_SomeoneDied;
        public static ThoughtDef MM_WasBlamed;
        public static ThoughtDef MM_CalledSomeoneOut;

    }

    [DefOf]
    public static class RoomRoleDefOf_MindMatters
    {
        public static RoomRoleDef DiningRoom;
        public static RoomRoleDef RecRoom;

        static RoomRoleDefOf_MindMatters()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RoomRoleDefOf_MindMatters));
        }
    }
}
