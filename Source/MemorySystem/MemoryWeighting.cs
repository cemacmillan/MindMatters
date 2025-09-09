/*using Verse;
using RimWorld;

namespace MindMatters;

public static class MemoryWeighting
{
    public static float AdjustDecayRate(Pawn pawn, string memoryTag, float baseDecayRate)
    {
        if (pawn.story?.traits?.HasTrait(TraitDef.Named("Vengeful")) == true && memoryTag == "insult")
            return baseDecayRate * 0.8f; // Slower decay

        if (pawn.story?.traits?.HasTrait(TraitDef.Named("Forgiving")) == true && memoryTag == "insult")
            return baseDecayRate * 1.2f; // Faster decay

        return baseDecayRate;
    }

    public static float AdjustWeight(Pawn pawn, MemoryEntry memory)
    {
        float weight = memory.Weight;

        Pawn initiator = FindInitiator(memory.Tags);
        if (initiator != null)
        {
            if (pawn.relations?.OpinionOf(initiator) > 40)
                weight *= 1.2f; // Friends reinforce positive memories

            if (pawn.relations?.OpinionOf(initiator) < -40)
                weight *= 1.5f; // Rivals reinforce negative memories
        }

        return weight;
    }
}*/