using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Adventurous : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.story.traits.HasTrait(MindMattersTraitDef.Adventurous))
            {
                if (p.Map != null)
                {
                    Area_Home homeArea = p.Map.areaManager.Home;

                    // If pawn is outside the home area
                    if (homeArea != null && !homeArea.ActiveCells.Contains(p.Position))
                    {
                        return ThoughtState.ActiveAtStage(0); // Happy when outside home area
                    }
                    else if (!MindMattersUtilities.IsPawnInSafeSituation(p))
                    {
                        return ThoughtState.ActiveAtStage(1); // Excited when in dangerous situation
                    }
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
