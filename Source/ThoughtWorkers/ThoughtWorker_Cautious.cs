using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Cautious : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.story.traits.HasTrait(MindMattersTraitDef.Cautious))
            {
                if (p.Map != null)
                {
                    Area_Home homeArea = p.Map.areaManager.Home;

                    // If pawn is outside the home area
                    if (homeArea != null && !homeArea.ActiveCells.Contains(p.Position))
                    {
                        return ThoughtState.ActiveAtStage(0); // Unhappy when outside home area
                    }
                    else if (MindMattersUtilities.IsPawnInSafeSituation(p))
                    {
                        return ThoughtState.ActiveAtStage(1); // Happy when in safe situation
                    }
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
