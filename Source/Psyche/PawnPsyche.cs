using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MindMatters
{
    /// <summary>
    /// Represents the psychological state of a single pawn.
    /// This is the core of the flat 8-mode psyche system.
    /// </summary>
    public class PawnPsyche : IExposable
    {
        // Core psychological state - the "chroma vector"
        private float[] modeWeights;
        private float[] modeGates;  // Consent/ethics gates (0-1)
        private float[,] interactionMatrix;
        
        // Dynamics
        private float smoothingAlpha = 0.2f;  // How quickly modes change
        private float hysteresisTau = 0.06f;  // Prevents rapid mode switching
        private int lastDominantMode = -1;
        private int ticksSinceLastModeChange = 0;
        
        // Experience processing
        private List<PsycheExperience> recentExperiences = new List<PsycheExperience>();
        private const int MaxRecentExperiences = 50;
        private const int ExperienceMemoryTicks = 60000; // 1 day
        
        // Impression system (ring buffer of decaying influences)
        private struct Impression 
        { 
            public int channel; 
            public float magnitude; 
            public float halfLifeTicks; 
            public int tick0; 
        }
        private readonly Queue<Impression> impressionBuffer = new Queue<Impression>();
        private const int MaxImpressions = 64;

        public PawnPsyche()
        {
            InitializeWeights();
            InitializeInteractionMatrix();
        }

        /// <summary>
        /// Current mode weights (chroma vector) - always sums to 1.0
        /// </summary>
        public float[] ModeWeights => modeWeights;

        /// <summary>
        /// Current dominant mode index
        /// </summary>
        public int DominantModeIndex => GetDominantModeIndex();

        /// <summary>
        /// Current dominant mode name
        /// </summary>
        public string DominantMode => ((PsychologicalMode)DominantModeIndex).ToString();

        /// <summary>
        /// Chroma vector as a copy (for external use)
        /// </summary>
        public float[] ChromaVector => (float[])modeWeights.Clone();
        
        /// <summary>
        /// Get chroma information for visualization
        /// </summary>
        public ChromaInfo GetChromaInfo()
        {
            return new ChromaInfo
            {
                DominantMode = DominantMode,
                DominantWeight = modeWeights[DominantModeIndex],
                ModeWeights = (float[])modeWeights.Clone(),
                ActiveImpressions = impressionBuffer.Count,
                RecentExperiences = recentExperiences.Count
            };
        }
        
        /// <summary>
        /// Get active impressions for visualization
        /// </summary>
        public List<ImpressionInfo> GetActiveImpressions()
        {
            var activeImpressions = new List<ImpressionInfo>();
            var currentTick = Find.TickManager.TicksGame;
            
            foreach (var impression in impressionBuffer)
            {
                float age = currentTick - impression.tick0;
                float decayFactor = UnityEngine.Mathf.Exp(-age / impression.halfLifeTicks);
                
                if (decayFactor > 0.02f)
                {
                    var modeNames = new[] { "Calm", "Vigilant", "Affiliative", "Acquisitive", "Despair", "Defiant", "Analyst", "Oracle" };
                    activeImpressions.Add(new ImpressionInfo
                    {
                        ModeName = impression.channel < modeNames.Length ? modeNames[impression.channel] : "Unknown",
                        Magnitude = impression.magnitude,
                        DecayFactor = decayFactor,
                        Age = age,
                        HalfLife = impression.halfLifeTicks
                    });
                }
            }
            
            return activeImpressions;
        }

        /// <summary>
        /// Process a new experience and update psychological state
        /// </summary>
        public void ProcessExperience(MindMattersInterface.Experience experience)
        {
            if (experience == null) return;

            // Convert MM Experience to PsycheExperience
            var psycheExp = ConvertToPsycheExperience(experience);
            recentExperiences.Add(psycheExp);

            // Create impressions from the experience
            CreateImpressionsFromExperience(psycheExp);

            // Clean up old experiences
            CleanupOldExperiences();
        }
        
        /// <summary>
        /// Create impressions from an experience and add them to the buffer
        /// </summary>
        private void CreateImpressionsFromExperience(PsycheExperience experience)
        {
            // Map experience to mode influences based on valency and flags
            var impressions = MapExperienceToImpressions(experience);
            
            foreach (var impression in impressions)
            {
                EnqueueImpression(impression.channel, impression.magnitude, impression.halfLifeTicks);
            }
        }
        
        /// <summary>
        /// Map an experience to impression deltas for different modes
        /// </summary>
        private List<(int channel, float magnitude, float halfLifeTicks)> MapExperienceToImpressions(PsycheExperience experience)
        {
            var impressions = new List<(int channel, float magnitude, float halfLifeTicks)>();
            
            // Base intensity from valency
            float baseIntensity = CalculateIntensity(ConvertToInterfaceExperience(experience));
            
            // Map based on experience type and valency
            switch (experience.Valency)
            {
                case ExperienceValency.Positive:
                    impressions.Add((GetModeIndex("Affiliative"), +0.15f * baseIntensity, 30000f)); // 12 hours
                    impressions.Add((GetModeIndex("Calm"), +0.10f * baseIntensity, 45000f)); // 18 hours
                    break;
                    
                case ExperienceValency.Negative:
                    impressions.Add((GetModeIndex("Despair"), +0.20f * baseIntensity, 60000f)); // 1 day
                    impressions.Add((GetModeIndex("Vigilant"), +0.15f * baseIntensity, 40000f)); // 16 hours
                    impressions.Add((GetModeIndex("Affiliative"), -0.10f * baseIntensity, 30000f)); // 12 hours
                    break;
                    
                case ExperienceValency.Eldritch:
                    impressions.Add((GetModeIndex("Vigilant"), +0.25f * baseIntensity, 90000f)); // 1.5 days
                    impressions.Add((GetModeIndex("Despair"), +0.15f * baseIntensity, 120000f)); // 2 days
                    break;
                    
                case ExperienceValency.Humiliating:
                    impressions.Add((GetModeIndex("Defiant"), +0.20f * baseIntensity, 80000f)); // 1.3 days
                    impressions.Add((GetModeIndex("Despair"), +0.15f * baseIntensity, 60000f)); // 1 day
                    break;
                    
                case ExperienceValency.Transformative:
                    impressions.Add((GetModeIndex("Acquisitive"), +0.15f * baseIntensity, 100000f)); // 1.7 days
                    impressions.Add((GetModeIndex("Calm"), +0.10f * baseIntensity, 60000f)); // 1 day
                    break;
            }
            
            // Apply flag-based modifiers
            if (experience.Flags.Contains("social"))
            {
                // Social experiences have stronger affiliative impact
                var affiliativeImpression = impressions.FirstOrDefault(i => i.channel == GetModeIndex("Affiliative"));
                if (affiliativeImpression.magnitude != 0)
                {
                    impressions.Remove(affiliativeImpression);
                    impressions.Add((affiliativeImpression.channel, affiliativeImpression.magnitude * 1.5f, affiliativeImpression.halfLifeTicks));
                }
            }
            
            if (experience.Flags.Contains("physical"))
            {
                // Physical experiences have stronger vigilant impact
                var vigilantImpression = impressions.FirstOrDefault(i => i.channel == GetModeIndex("Vigilant"));
                if (vigilantImpression.magnitude != 0)
                {
                    impressions.Remove(vigilantImpression);
                    impressions.Add((vigilantImpression.channel, vigilantImpression.magnitude * 1.3f, vigilantImpression.halfLifeTicks));
                }
            }
            
            return impressions;
        }
        
        /// <summary>
        /// Get the index of a mode by name
        /// </summary>
        private int GetModeIndex(string modeName)
        {
            var modes = new[] { "Calm", "Vigilant", "Affiliative", "Acquisitive", "Despair", "Defiant", "Analyst", "Oracle" };
            return Array.IndexOf(modes, modeName);
        }
        
        /// <summary>
        /// Add an impression to the buffer
        /// </summary>
        private void EnqueueImpression(int channel, float magnitude, float halfLifeTicks)
        {
            if (channel < 0 || channel >= modeWeights.Length) return;
            
            // Remove old impressions if buffer is full
            if (impressionBuffer.Count >= MaxImpressions)
            {
                impressionBuffer.Dequeue();
            }
            
            impressionBuffer.Enqueue(new Impression 
            { 
                channel = channel, 
                magnitude = magnitude, 
                halfLifeTicks = halfLifeTicks, 
                tick0 = Find.TickManager.TicksGame 
            });
        }

        /// <summary>
        /// Update psychological state over time
        /// </summary>
        public void Tick(int ticks)
        {
            // Process impressions (decay and apply to mode weights)
            ProcessImpressions(ticks);
            
            // Apply mode interactions
            ApplyModeInteractions();

            // Apply smoothing
            ApplySmoothing();

            // Update mode change tracking
            UpdateModeChangeTracking();

            // Clean up old experiences
            CleanupOldExperiences();
        }
        
        /// <summary>
        /// Process impressions: decay them and apply their influence to mode weights
        /// </summary>
        private void ProcessImpressions(int dt)
        {
            if (impressionBuffer.Count == 0) return;
            
            // Process all impressions in the buffer
            int bufferCount = impressionBuffer.Count;
            for (int i = 0; i < bufferCount; i++)
            {
                var impression = impressionBuffer.Dequeue();
                float age = Find.TickManager.TicksGame - impression.tick0;
                float decayFactor = UnityEngine.Mathf.Exp(-age / impression.halfLifeTicks);
                
                if (decayFactor > 0.02f) // Keep impression if still significant
                {
                    // Apply decayed influence to mode weights
                    modeWeights[impression.channel] += impression.magnitude * decayFactor * 0.1f; // Small nudges
                    impressionBuffer.Enqueue(impression); // Keep until smaller
                }
            }
            
            // Normalize weights after applying impressions
            NormalizeWeights();
        }
        
        /// <summary>
        /// Apply DynamicNeeds coupling to influence mode weights
        /// </summary>
        public void ApplyDynamicNeedsCoupling(Pawn pawn)
        {
            if (pawn?.needs == null) return;
            
            // Hunger pushes Acquisitive mode up
            if (pawn.needs.food != null)
            {
                float hunger = 1f - pawn.needs.food.CurLevel; // 0..1
                int acquisitiveIdx = GetModeIndex("Acquisitive");
                if (acquisitiveIdx >= 0)
                {
                    modeWeights[acquisitiveIdx] += 0.15f * hunger;
                }
            }
            
            // Social isolation pushes Affiliative mode up
            if (pawn.needs.joy != null)
            {
                float solitude = 1f - pawn.needs.joy.CurLevel;
                int affiliativeIdx = GetModeIndex("Affiliative");
                if (affiliativeIdx >= 0)
                {
                    modeWeights[affiliativeIdx] += 0.10f * solitude;
                }
            }
            
            // Low rest pushes Vigilant mode up
            if (pawn.needs.rest != null)
            {
                float fatigue = 1f - pawn.needs.rest.CurLevel;
                int vigilantIdx = GetModeIndex("Vigilant");
                if (vigilantIdx >= 0)
                {
                    modeWeights[vigilantIdx] += 0.12f * fatigue;
                }
            }
            
            // High stress pushes Despair mode up
            if (pawn.needs.mood != null)
            {
                float stress = 1f - pawn.needs.mood.CurLevel;
                int despairIdx = GetModeIndex("Despair");
                if (despairIdx >= 0)
                {
                    modeWeights[despairIdx] += 0.08f * stress;
                }
            }
            
            // Normalize after applying needs coupling
            NormalizeWeights();
        }

        /// <summary>
        /// Nudge a specific mode (for external influence)
        /// </summary>
        public void Nudge(int modeIndex, float delta)
        {
            if (modeIndex < 0 || modeIndex >= modeWeights.Length) return;

            modeWeights[modeIndex] = UnityEngine.Mathf.Clamp(modeWeights[modeIndex] + delta, 0f, 1f);
            NormalizeWeights();
        }

        /// <summary>
        /// Scale a specific mode (for external influence)
        /// </summary>
        public void Scale(int modeIndex, float gain)
        {
            if (modeIndex < 0 || modeIndex >= modeWeights.Length) return;

            modeWeights[modeIndex] = UnityEngine.Mathf.Clamp(modeWeights[modeIndex] * gain, 0f, 1f);
            NormalizeWeights();
        }

        /// <summary>
        /// Set a mode gate (consent/ethics)
        /// </summary>
        public void SetGate(int modeIndex, bool allowed)
        {
            if (modeIndex < 0 || modeIndex >= modeGates.Length) return;

            modeGates[modeIndex] = allowed ? 1f : 0f;
        }

        /// <summary>
        /// Get the current psychological "resting persona" description
        /// </summary>
        public string GetRestingPersona()
        {
            var dominant = DominantModeIndex;
            var secondary = GetSecondaryModeIndex();
            
            if (secondary == -1) return DominantMode;
            
            return $"{DominantMode}-{((PsychologicalMode)secondary).ToString()}";
        }

        /// <summary>
        /// Check if a specific mode combination is active
        /// </summary>
        public bool HasModeCombination(PsychologicalMode primary, PsychologicalMode secondary, float threshold = 0.5f)
        {
            var primaryWeight = modeWeights[(int)primary];
            var secondaryWeight = modeWeights[(int)secondary];
            
            return primaryWeight > threshold && secondaryWeight > threshold * 0.7f;
        }

        private void InitializeWeights()
        {
            var modeCount = PsychologicalModeExtensions.Count;
            modeWeights = new float[modeCount];
            modeGates = new float[modeCount];
            
            // Initialize with small random values
            for (int i = 0; i < modeCount; i++)
            {
                modeWeights[i] = Rand.Range(0.05f, 0.15f);
                modeGates[i] = 1f; // All modes initially allowed
            }
            
            NormalizeWeights();
        }

        private void InitializeInteractionMatrix()
        {
            var modeCount = PsychologicalModeExtensions.Count;
            interactionMatrix = new float[modeCount, modeCount];
            
            // Initialize with identity matrix (modes don't interact by default)
            for (int i = 0; i < modeCount; i++)
            {
                for (int j = 0; j < modeCount; j++)
                {
                    interactionMatrix[i, j] = i == j ? 1f : 0f;
                }
            }
            
            // Add some basic interactions
            // Oracle and Reflective can reinforce each other
            interactionMatrix[(int)PsychologicalMode.Oracle, (int)PsychologicalMode.Reflective] = 0.3f;
            interactionMatrix[(int)PsychologicalMode.Reflective, (int)PsychologicalMode.Oracle] = 0.3f;
            
            // Analyst and Pragmatic work well together
            interactionMatrix[(int)PsychologicalMode.Analyst, (int)PsychologicalMode.Pragmatic] = 0.4f;
            interactionMatrix[(int)PsychologicalMode.Pragmatic, (int)PsychologicalMode.Analyst] = 0.4f;
            
            // Chaotic disrupts other modes
            for (int i = 0; i < modeCount; i++)
            {
                if (i != (int)PsychologicalMode.Chaotic)
                {
                    interactionMatrix[(int)PsychologicalMode.Chaotic, i] = -0.2f;
                }
            }
        }

        private PsycheExperience ConvertToPsycheExperience(MindMattersInterface.Experience experience)
        {
            return new PsycheExperience
            {
                EventType = experience.EventType,
                Valency = (ExperienceValency)experience.Valency,
                Flags = new HashSet<string>(experience.Flags),
                Intensity = CalculateIntensity(experience),
                Timestamp = experience.Timestamp
            };
        }

        /// <summary>
        /// Convert PsycheExperience to MindMattersInterface.Experience
        /// </summary>
        private MindMattersInterface.Experience ConvertToInterfaceExperience(PsycheExperience experience)
        {
            return new MindMattersInterface.Experience(experience.EventType, (MindMattersInterface.ExperienceValency)experience.Valency)
            {
                Flags = experience.Flags,
                Timestamp = experience.Timestamp
            };
        }
        
        private float CalculateIntensity(MindMattersInterface.Experience experience)
        {
            // Base intensity from valency
            float intensity = experience.Valency switch
            {
                MindMattersInterface.ExperienceValency.Positive => 0.3f,
                MindMattersInterface.ExperienceValency.Negative => 0.7f,
                MindMattersInterface.ExperienceValency.Neutral => 0.1f,
                MindMattersInterface.ExperienceValency.Eldritch => 0.9f,
                MindMattersInterface.ExperienceValency.Affirming => 0.4f,
                MindMattersInterface.ExperienceValency.Humiliating => 0.8f,
                MindMattersInterface.ExperienceValency.Exhilarating => 0.6f,
                MindMattersInterface.ExperienceValency.Transformative => 0.8f,
                _ => 0.5f
            };

            // Modify by flags
            if (experience.Flags.Contains("trauma")) intensity *= 1.5f;
            if (experience.Flags.Contains("social")) intensity *= 0.8f;
            if (experience.Flags.Contains("physical")) intensity *= 1.2f;

            return UnityEngine.Mathf.Clamp(intensity, 0f, 1f);
        }

        private float[] CalculatePsychologicalImpact(PsycheExperience experience)
        {
            var impact = new float[PsychologicalModeExtensions.Count];
            
            // Map experience to psychological impact based on event type and valency
            switch (experience.EventType)
            {
                case "PawnDowned":
                    impact[(int)PsychologicalMode.Reflective] = experience.Intensity * 0.8f;
                    impact[(int)PsychologicalMode.Chaotic] = experience.Intensity * 0.3f;
                    break;
                    
                case "Therapy":
                    impact[(int)PsychologicalMode.Reflective] = experience.Intensity * 1.2f;
                    impact[(int)PsychologicalMode.Social] = experience.Intensity * 0.5f;
                    break;
                    
                case "Construction":
                    impact[(int)PsychologicalMode.Builder] = experience.Intensity * 1.0f;
                    impact[(int)PsychologicalMode.Pragmatic] = experience.Intensity * 0.6f;
                    break;
                    
                case "Combat":
                    impact[(int)PsychologicalMode.Survivalist] = experience.Intensity * 0.8f;
                    impact[(int)PsychologicalMode.Chaotic] = experience.Intensity * 0.4f;
                    break;
                    
                default:
                    // Generic impact based on valency
                    for (int i = 0; i < impact.Length; i++)
                    {
                        impact[i] = experience.Intensity * 0.1f;
                    }
                    break;
            }

            return impact;
        }

        private void ApplyImpact(float[] impact)
        {
            for (int i = 0; i < modeWeights.Length; i++)
            {
                modeWeights[i] += impact[i] * modeGates[i];
            }
            NormalizeWeights();
        }

        private void ApplyModeInteractions()
        {
            var newWeights = new float[modeWeights.Length];
            
            for (int i = 0; i < modeWeights.Length; i++)
            {
                for (int j = 0; j < modeWeights.Length; j++)
                {
                    newWeights[i] += modeWeights[j] * interactionMatrix[j, i];
                }
            }
            
            modeWeights = newWeights;
            NormalizeWeights();
        }

        private void ApplySmoothing()
        {
            // Apply smoothing to prevent rapid changes
            for (int i = 0; i < modeWeights.Length; i++)
            {
                modeWeights[i] = UnityEngine.Mathf.Lerp(modeWeights[i], modeWeights[i], 1f - smoothingAlpha);
            }
            NormalizeWeights();
        }

        private void NormalizeWeights()
        {
            // Ensure weights sum to 1.0
            var sum = modeWeights.Sum();
            if (sum > 0f)
            {
                for (int i = 0; i < modeWeights.Length; i++)
                {
                    modeWeights[i] /= sum;
                }
            }
            else
            {
                // If all weights are zero, distribute evenly
                var equalWeight = 1f / modeWeights.Length;
                for (int i = 0; i < modeWeights.Length; i++)
                {
                    modeWeights[i] = equalWeight;
                }
            }
        }

        private int GetDominantModeIndex()
        {
            int maxIndex = 0;
            float maxWeight = modeWeights[0];
            
            for (int i = 1; i < modeWeights.Length; i++)
            {
                if (modeWeights[i] > maxWeight)
                {
                    maxWeight = modeWeights[i];
                    maxIndex = i;
                }
            }
            
            return maxIndex;
        }

        private int GetSecondaryModeIndex()
        {
            int maxIndex = -1;
            int secondIndex = -1;
            float maxWeight = 0f;
            float secondWeight = 0f;
            
            for (int i = 0; i < modeWeights.Length; i++)
            {
                if (modeWeights[i] > maxWeight)
                {
                    secondWeight = maxWeight;
                    secondIndex = maxIndex;
                    maxWeight = modeWeights[i];
                    maxIndex = i;
                }
                else if (modeWeights[i] > secondWeight)
                {
                    secondWeight = modeWeights[i];
                    secondIndex = i;
                }
            }
            
            return secondIndex;
        }

        private void UpdateModeChangeTracking()
        {
            var currentDominant = DominantModeIndex;
            
            if (currentDominant != lastDominantMode)
            {
                // Check hysteresis threshold
                var maxWeight = modeWeights[currentDominant];
                var secondWeight = GetSecondaryModeWeight();
                
                if (maxWeight - secondWeight < hysteresisTau)
                {
                    // Keep previous dominant mode
                    return;
                }
                
                lastDominantMode = currentDominant;
                ticksSinceLastModeChange = 0;
            }
            else
            {
                ticksSinceLastModeChange++;
            }
        }

        private float GetSecondaryModeWeight()
        {
            var sortedWeights = modeWeights.OrderByDescending(w => w).ToArray();
            return sortedWeights.Length > 1 ? sortedWeights[1] : 0f;
        }

        private void CleanupOldExperiences()
        {
            var currentTick = Find.TickManager.TicksGame;
            recentExperiences.RemoveAll(exp => currentTick - exp.Timestamp > ExperienceMemoryTicks);
            
            // Keep only the most recent experiences
            if (recentExperiences.Count > MaxRecentExperiences)
            {
                recentExperiences = recentExperiences
                    .OrderByDescending(exp => exp.Timestamp)
                    .Take(MaxRecentExperiences)
                    .ToList();
            }
        }

        public void ExposeData()
        {
            // Convert arrays to lists for serialization
            var modeWeightsList = modeWeights?.ToList() ?? new List<float>();
            var modeGatesList = modeGates?.ToList() ?? new List<float>();
            
            Scribe_Collections.Look(ref modeWeightsList, "modeWeights", LookMode.Value);
            Scribe_Collections.Look(ref modeGatesList, "modeGates", LookMode.Value);
            Scribe_Values.Look(ref smoothingAlpha, "smoothingAlpha", 0.2f);
            Scribe_Values.Look(ref hysteresisTau, "hysteresisTau", 0.06f);
            Scribe_Values.Look(ref lastDominantMode, "lastDominantMode", -1);
            Scribe_Values.Look(ref ticksSinceLastModeChange, "ticksSinceLastModeChange", 0);
            Scribe_Collections.Look(ref recentExperiences, "recentExperiences", LookMode.Deep);
            
            // Convert back to arrays after loading
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                modeWeights = modeWeightsList?.ToArray() ?? new float[PsychologicalModeExtensions.Count];
                modeGates = modeGatesList?.ToArray() ?? new float[PsychologicalModeExtensions.Count];
            }
        }
    }

    /// <summary>
    /// Internal experience representation for psyche processing
    /// </summary>
    public class PsycheExperience : IExposable
    {
        public string EventType;
        public ExperienceValency Valency;
        public HashSet<string> Flags = new HashSet<string>();
        public float Intensity;
        public int Timestamp;

        public void ExposeData()
        {
            Scribe_Values.Look(ref EventType, "eventType");
            Scribe_Values.Look(ref Valency, "valency");
            Scribe_Collections.Look(ref Flags, "flags", LookMode.Value);
            Scribe_Values.Look(ref Intensity, "intensity");
            Scribe_Values.Look(ref Timestamp, "timestamp");
        }
    }
    
    /// <summary>
    /// Information about a pawn's chroma state for visualization
    /// </summary>
    public class ChromaInfo
    {
        public string DominantMode;
        public float DominantWeight;
        public float[] ModeWeights;
        public int ActiveImpressions;
        public int RecentExperiences;
    }
    
    /// <summary>
    /// Information about an active impression for visualization
    /// </summary>
    public class ImpressionInfo
    {
        public string ModeName;
        public float Magnitude;
        public float DecayFactor;
        public float Age;
        public float HalfLife;
    }
}
