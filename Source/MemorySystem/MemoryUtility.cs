/*using System.Collections.Generic;
using Verse;

namespace MindMatters;

public static class MemoryUtility
{
    public static Pawn FindInitiator(List<string> tags)
    {
        if (tags == null || tags.Count == 0)
            return null;

        // First tag should always be a Pawn ID or name
        string potentialPawnId = tags[0];

        // Try to find the pawn by ID
        if (int.TryParse(potentialPawnId, out int pawnID))
        {
            return Find.PawnById(pawnID);
        }

        // If it's not an ID, try finding by name (fallback)
        return Find.PawnByName(potentialPawnId);
    }
}*/