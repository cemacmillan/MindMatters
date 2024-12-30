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
        if (pawn == null || pawn.Dead) 
            return false;

        // Instead of `pawn.Faction == Faction.OfPlayer`, do a safer null check:
        // If the player's faction isn't loaded yet, skip or fallback
        if (pawn.Faction == null || IsPlayerFaction(pawn.Faction))
        {
            // e.g. if it's the player's faction, treat them as not "guest"
            return false;
        }

        if (pawn.guest?.IsPrisoner == false && pawn.guest?.GuestStatus == GuestStatus.Guest)
        {
            return true;
        }
        if (pawn.guest?.GuestStatus == GuestStatus.Slave)
        {
            return true;
        }
        if (pawn.IsQuestLodger())
        {
            return false;
        }
        return false;
    }

// Possibly a helper that safely checks if faction is the player's
    private static bool IsPlayerFaction(Faction faction)
    {
        // If 'faction' is null or if player faction isn't known, just return false or skip
        if (faction == null || Faction.OfPlayerSilentFail == null)
        {
            // fallback => not recognized
            return false;
        }
        return faction == Faction.OfPlayerSilentFail;
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