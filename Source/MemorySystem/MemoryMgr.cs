
using System.Collections.Generic;

namespace MindMatters;

public class MemoryMgr
{
    private Dictionary<string, List<MemoryEntry>> memoryEntries = new();

    public void AddMemory(string tag, float weight, int timestamp, float decayRate = 1f)
    {
        if (!memoryEntries.ContainsKey(tag))
            memoryEntries[tag] = new List<MemoryEntry>();

        memoryEntries[tag].Add(new MemoryEntry(weight, timestamp, new List<string> { tag }, decayRate));
    }

    public float GetWeightedImpact(string tag, int currentTime)
    {
        if (!memoryEntries.ContainsKey(tag))
            return 0f;

        float totalImpact = 0f;
        List<MemoryEntry> entries = memoryEntries[tag];

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            entries[i].ApplyDecay(currentTime);
            if (entries[i].Weight <= 0.001f) // Remove near-zero memories
                entries.RemoveAt(i);
            else
                totalImpact += entries[i].Weight;
        }

        return totalImpact;
    }
}