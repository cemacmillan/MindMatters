using System.Collections.Generic;

namespace MindMatters;

public static class MemoryRegistry
{
    private static Dictionary<string, float> defaultDecayRates = new()
    {
        { "compliment", 0.98f }, // Slower decay
        { "insult", 0.95f },     // Faster decay unless modified by Traits
        { "gift", 0.99f },       // Persists long-term
    };

    public static float GetDecayRate(string memoryTag)
    {
        return defaultDecayRates.TryGetValue(memoryTag, out float decayRate) ? decayRate : 0.97f;
    }
}