using System;
using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace MindMatters
{
    public class PersonalityMatrix
    {
        private Dictionary<string, float> matrix;

        public PersonalityMatrix()
        {
            matrix = new Dictionary<string, float>
            {
        {"Openness", Rand.Range(0.3f, 0.7f)},
        {"Conscientiousness", Rand.Range(0.3f, 0.7f)},
        {"Extraversion", Rand.Range(0.3f, 0.7f)},
        {"Agreeableness", Rand.Range(0.3f, 0.7f)},
        {"Neuroticism", Rand.Range(0.3f, 0.7f)},
        {"Resilience", Rand.Range(0.3f, 0.7f)},
        {"Adventurousness", Rand.Range(0.3f, 0.7f)},
        {"Dominance", Rand.Range(-0.7f, 0.7f)},
        {"SensoryProcessingSensitivity", Rand.Range(0.3f, 0.7f)}
            };
        }

        public float this[string index]
        {
            get
            {
                if (matrix.ContainsKey(index))
                {
                    return matrix[index];
                }
                else
                {
                    Log.Warning($"Missing personality dimension: {index}");
                    return 0.5f; // Return a default value.
                }
            }
            set
            {
                if (matrix.ContainsKey(index))
                {
                    matrix[index] = Mathf.Clamp(value, 0f, 1f); // Clamp the value between 0 and 1
                }
                else
                {
                    Log.Warning($"Missing personality dimension: {index}");
                    matrix[index] = Mathf.Clamp(value, 0f, 1f); // Create the key with the provided value, clamped between 0 and 1
                }
            }
        }

        public void Adjust(string dimension, float change)
        {
            if (!matrix.ContainsKey(dimension))
            {
                Log.Warning($"Missing personality dimension: {dimension}");
                return;
            }

            // Add some randomness to the adjustment
            float randomFactor = Rand.Range(-0.1f, 0.1f);
            this[dimension] += change + randomFactor;
        }

        public void AdjustForTrait(Trait trait)
        {
            TraitPersonalityAdjustments adjustments = trait.def.GetModExtension<TraitPersonalityAdjustments>();
            if (adjustments != null)
            {
                foreach (var adjustment in adjustments.adjustments)
                {
                    Adjust(adjustment.dimension, adjustment.change);
                }
            }
        }

        public void AdjustForSkill(SkillRecord skill)
        {
            // Implementation here.
        }

        public void AdjustForCapabilities(Pawn pawn)
        {
            // Implementation here.
        }

        public Dictionary<string, float> GetDimensions()
        {
            // Create a new dictionary as a copy of the matrix
            return new Dictionary<string, float>(matrix);
        }

    }
}
