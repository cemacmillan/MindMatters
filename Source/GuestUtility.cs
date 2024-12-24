using RimWorld;
using Verse;

namespace MindMatters;

public static class GuestUtility
{
    /// <summary>
    /// Determines if the pawn is a guest, prisoner, or other equivalent status.
    /// </summary>
    public static bool IsGuestOrEquivalent(Pawn pawn)
    {
        if (pawn == null || pawn.Dead || pawn.Faction == Faction.OfPlayer)
        {
            return false; // Exclude null, dead, or player faction pawns
        }

        // Check Hospitality-style guest logic (simulate `IsGuest`)
        if (pawn.guest?.IsPrisoner == false && pawn.guest?.GuestStatus == GuestStatus.Guest)
        {
            return true; // Recognize non-prisoner guests
        }

        // Slaves
        if (pawn.guest?.GuestStatus == GuestStatus.Slave)
        {
            return true; // Slaves are "guest-like" in some contexts
        }

        // Quest Lodgers
        if (pawn.IsQuestLodger())
        {
            return false; // Exclude quest lodgers unless overridden
        }

        return false; // Fallback
    }

    /// <summary>
    /// Determines if a pawn is a slave.
    /// </summary>
    public static bool IsSlave(Pawn pawn)
    {
        return pawn.guest?.GuestStatus == GuestStatus.Slave;
    }

    /// <summary>
    /// Determines if a pawn is a quest lodger.
    /// </summary>
    public static bool IsQuestLodger(Pawn pawn)
    {
        return pawn.IsQuestLodger();
    }
}