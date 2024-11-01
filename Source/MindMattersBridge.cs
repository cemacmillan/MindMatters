using MindMatters;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace MindMatters
{
    public class MindMattersBridge
    {
        public static void HandlePositiveInteraction(Pawn initiator, float probability, string eventType, int valency)
        {
            // Convert the int to ExperienceValency
            ExperienceValency valencyEnum = (ExperienceValency)valency;

            // Check if experience should be added based on probability
            if (UnityEngine.Random.Range(0f, 1f) < probability)
            {
                Log.Message($"Adding experience for {initiator} with event type {eventType} and valency {valencyEnum}");
                // Add the experience to the initiator
                MindMattersUtilities.AddExperience(initiator, eventType, valencyEnum);
            }
        }

        public static void Initialize()
        {
            if (MindMattersMod.IsPositiveConnectionsActive)
            {
                // List of interaction worker types and LordJobs
                var interactionWorkerTypes = new List<string>
                {
                    "PositiveConnectionsNmSpc.InteractionWorker_Compliment",
                    "PositiveConnectionsNmSpc.InteractionWorker_DiscussIdeoligion",
                    "PositiveConnectionsNmSpc.InteractionWorker_Gift",
                    "PositiveConnectionsNmSpc.InteractionWorker_GiveComfort",
                    "PositiveConnectionsNmSpc.InteractionWorker_Mediation",
                    "PositiveConnectionsNmSpc.InteractionWorker_SharedPassion",
                    "PositiveConnectionsNmSpc.InteractionWorker_SkillShare",
                    "PositiveConnectionsNmSpc.InteractionWorker_Storytelling"
                };

                foreach (var workerType in interactionWorkerTypes)
                {
                    // Get the InteractionWorker type from the Positive Connections assembly
                    Type interactionWorkerType = Type.GetType(workerType + ", PositiveConnections");

                    if (interactionWorkerType == null)
                    {
                        Log.Error($"Failed to get type {workerType}");
                        continue;  // Skip to the next worker type
                    }

                    // Get the OnPositiveInteraction event from the InteractionWorker type
                    EventInfo onPositiveInteractionEvent = interactionWorkerType.GetEvent("OnPositiveInteraction");

                    if (onPositiveInteractionEvent == null)
                    {
                        Log.Error($"Failed to get OnPositiveInteraction event from {workerType}");
                        continue;  // Skip to the next worker type
                    }

                    // Create a delegate that references the HandlePositiveInteraction method
                    Delegate handler = Delegate.CreateDelegate(onPositiveInteractionEvent.EventHandlerType, typeof(MindMattersBridge).GetMethod("HandlePositiveInteraction"));

                    // Use reflection to subscribe to the event
                    onPositiveInteractionEvent.AddEventHandler(null, handler);
                }
            }
        }
    }
}