using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class InteractionWorker_ScapegoatBlame : InteractionWorker
    {
        private MindMattersVictimManager victimManager = MindMattersVictimManager.Instance;

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            // If the recipient is not the scapegoat, immediately return 0
            if (!victimManager.IsScapegoat(recipient))
            {
                return 0f;
            }

            // Check if the initiator's mood is less than 50, they haven't blamed the scapegoat this cycle,
            // and they don't have the Kind or TenderHearted traits.
            if (initiator.needs.mood.CurLevelPercentage > 0.7f ||
                victimManager.AlreadyBlamedThisCycle(initiator) ||
                initiator.story.traits.HasTrait(TraitDefOf.Kind) ||
                initiator.story.traits.HasTrait(MindMattersTraits.TenderHearted)) // Assuming TenderHearted is a defined TraitDef
            {
                return 0f;
            }

            // Check if the scapegoat's mood is higher than the initiator's
            if (recipient.needs.mood.CurLevelPercentage <= initiator.needs.mood.CurLevelPercentage ||
                recipient.Downed || recipient.IsBurning())
            {
                return 0f;
            }

            // If all conditions are met, return a weight for this interaction
            //return 0.05f; // This value determines how often this interaction occurs relative to other interactions
            return 0.2f;
        }
        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            // Apply the "called someone out" mood buff to the initiator
            initiator.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfMindMatters.MM_CalledSomeoneOut);

            // Apply a mood debuff to the scapegoat
            recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfMindMatters.MM_WasBlamed);

            // Record that the initiator has blamed the scapegoat
            victimManager.RecordBlame(initiator);

            // Show a notification
            string notificationText = $"{initiator.Name.ToStringFull} has blamed {recipient.Name.ToStringFull} for their problems.";
            Messages.Message(notificationText, MessageTypeDefOf.NeutralEvent, false);

            // Set out parameters to null as we're not using them
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
        }
    }
}

