using UnityEngine;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace MindMatters
{
    public class TraitPersonalityAdjustments : DefModExtension
    {
        public List<TraitAdjustment> adjustments;

        public void Apply(PersonalityMatrix personalityMatrix)
        {
            foreach (var adjustment in adjustments)
            {
                personalityMatrix.Adjust(adjustment.dimension, adjustment.change);
            }
        }
    }

    public struct TraitAdjustment
    {
        public string dimension;
        public float change;
    }
}

