using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace MindMatters
{
    public static class MindMattersBridge
    {
        private static MindMattersExperienceComponent experienceComponent;

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (!ModsConfig.IsActive("cem.mindmatters") && !ModsConfig.IsActive("cem.mindmatterspr"))
            {
                MMToolkit.GripeOnce("[MindMattersBridge] Mind Matters is not active because no Mind Matters Candidate Mod. Bridge initialization skipped.");
                return;
            }

            experienceComponent = MindMattersExperienceComponent.GetOrCreateInstance();

            if (experienceComponent == null)
            {
                Log.Error("[MindMattersBridge] Failed to retrieve MindMattersExperienceComponent. Initialization aborted.");
                return;
            }

            MMToolkit.DebugWarn("[MindMattersBridge] Successfully initialized with MindMattersExperienceComponent.");
        }
        
        /*
        public static void Initialize()
        {
            if (!ModsConfig.IsActive("cem.mindmatters"))
            {
                MMToolkit.DebugWarn("[MindMattersBridge] Mind Matters is not active. Bridge initialization skipped.");
                return;
            }

            // Directly retrieve the component using RimWorld's GameComponent system
            experienceComponent = Current.Game.GetComponent<MindMattersExperienceComponent>();

            if (experienceComponent == null)
            {
                throw new Exception("[MindMattersBridge] Failed to retrieve a valid MindMattersExperienceComponent. Ensure Mind Matters is loaded correctly.");
            }

            MMToolkit.DebugLog("[MindMattersBridge] Successfully initialized with MindMattersExperienceComponent.");
        }
        */
        
        // Add an experience to a pawn
        public static void AddExperience(Pawn pawn, string eventType, ExperienceValency valency, HashSet<string> tags = null)
        {
            if (pawn == null || string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("[MindMattersBridge] Invalid parameters for AddExperience.");
            }

            if (experienceComponent == null)
            {
                throw new InvalidOperationException("[MindMattersBridge] Attempted to add an experience, but the bridge is not initialized.");
            }

            Experience experience = new Experience(eventType, valency)
            { 
                Flags = tags ?? new HashSet<string>()
            };

            experienceComponent.AddExperience(pawn, experience);
            MMToolkit.DebugLog($"[MindMattersBridge] Added experience '{eventType}' ({valency}) for {pawn.LabelShort}.");
        }

        // Example specific handler: Positive interactions from Positive Connections
        public static void HandlePositiveInteraction(Pawn initiator, string eventType, ExperienceValency valency, float probability)
        {
            if (initiator == null || string.IsNullOrEmpty(eventType))
            {
                throw new ArgumentException("[MindMattersBridge] Invalid parameters for HandlePositiveInteraction.");
            }

            if (UnityEngine.Random.Range(0f, 1f) < probability)
            {
                MMToolkit.DebugLog($"[MindMattersBridge] Handling positive interaction '{eventType}' ({valency}) for {initiator.LabelShort}.");
                AddExperience(initiator, eventType, valency, new HashSet<string> { "PositiveInteraction" });
            }
        }
    }
}