using System.Collections.Generic;
using UnityEngine;

namespace MindMatters;

public class MemoryEntry
{
    public float Weight { get; private set; }
    public int Timestamp { get; private set; }
    public List<string> Tags { get; private set; }
    public float DecayRate { get; private set; } 

    public MemoryEntry(float weight, int timestamp, List<string> tags, float decayRate = 1f)
    {
        Weight = weight;
        Timestamp = timestamp;
        Tags = tags;
        DecayRate = decayRate; // Adjusted per Trait, Relationship
    }

    public void ApplyDecay(int currentTime)
    {
        int elapsedTicks = currentTime - Timestamp;
        Weight *= Mathf.Pow(DecayRate, elapsedTicks / 60000f); // Decays per in-game day
    }
}