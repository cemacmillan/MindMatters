using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Alone : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            bool isAlone = MindMattersUtilities.IsPawnAlone(p);

            if (p.story.traits.HasTrait(TraitDef.Named("Outgoing")))
            {
                // Outgoing pawns are unhappy when alone
                return isAlone ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
            }
            else if (p.story.traits.HasTrait(TraitDef.Named("Reserved")))
            {
                // Reserved pawns are happy when alone
                return isAlone ? ThoughtState.ActiveAtStage(1) : ThoughtState.Inactive;
            }
            else if (p.story.traits.HasTrait(TraitDef.Named("Recluse")))
            {
                // Recluse pawns are very happy when alone
                return isAlone ? ThoughtState.ActiveAtStage(2) : ThoughtState.Inactive;
            }
            else
            {
                return ThoughtState.Inactive;
            }
        }
    }
}
