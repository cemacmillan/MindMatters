using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Alone : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            List<Pawn> allPawns = Current.Game.GetComponent<MindMattersGameComponent>().GetAllPawns();
            bool isAlone = MindMattersUtilities.IsPawnAlone(p,allPawns);

            if (p.story.traits.HasTrait(MindMattersTraits.Outgoing) || p.story.traits.HasTrait(MindMattersTraits.Socialite))
            {
                // Outgoing and Socialite pawns are unhappy when alone
                return isAlone ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
            }
            else
            {
                TraitDef reservedTrait = MindMattersTraits.Reserved;
                if (reservedTrait != null && p.story.traits.HasTrait(reservedTrait))
                {
                    // Reserved pawns are happy when alone
                    return isAlone ? ThoughtState.ActiveAtStage(1) : ThoughtState.Inactive;
                }
                else
                {
                    TraitDef recluseTrait = TraitDef.Named("Recluse");
                    if (recluseTrait != null && p.story.traits.HasTrait(recluseTrait))
                    {
                        // Recluse pawns are very happy when alone
                        return isAlone ? ThoughtState.ActiveAtStage(2) : ThoughtState.Inactive;
                    }
                }
            }

            return ThoughtState.Inactive;
        }
    }
}