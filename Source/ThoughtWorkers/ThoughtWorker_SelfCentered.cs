using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_SelfCentered : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // A pawn without the SelfCentered trait is not affected
            if (!p.story.traits.HasTrait(MindMattersTraits.SelfCentered))
            {
                return ThoughtState.Inactive;
            }

            // Get the current needs of the pawn
            var needs = p.needs.AllNeeds;
            int significantNeedsCount = 0;

            // Check each need and count how many are significantly unfulfilled
            // (below a certain threshold which you can adjust)
            foreach (var need in needs)
            {
                if (need.CurLevelPercentage < 0.5)  // adjust the threshold as needed
                {
                    significantNeedsCount++;
                }
            }

            // You can modify these conditions as you see fit for your mod
            if (significantNeedsCount >= 3)  // if 3 or more needs are significantly unfulfilled
            {
                return ThoughtState.ActiveAtStage(2);  // major negative mood impact
            }
            else if (significantNeedsCount >= 1)  // if 1 or 2 needs are significantly unfulfilled
            {
                return ThoughtState.ActiveAtStage(1);  // minor negative mood impact
            }
            else  // if no significant needs are unfulfilled
            {
                return ThoughtState.ActiveAtStage(0);  // neutral or positive mood impact
            }
        }
    }
}
