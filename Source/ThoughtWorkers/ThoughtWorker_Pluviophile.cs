using RimWorld;
using Verse;


namespace MindMatters
{
    public class ThoughtWorker_Pluviophile : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
            {
                return ThoughtState.Inactive;
            }

            if (!p.RaceProps.Humanlike)
            {
                return ThoughtState.Inactive;
            }

            if (!p.story.traits.HasTrait(MindMattersTraitDef.Pluviophile))
            {
                return ThoughtState.Inactive;
            }

            if (p.Map.weatherManager.RainRate < 0.25f || p.Map.weatherManager.SnowRate > 0.25f)
            {
                return ThoughtState.Inactive;
            }

            if (p.Position.Roofed(p.Map))
            {
                return ThoughtState.ActiveAtStage(0); // "raining outside"
            }

            return ThoughtState.ActiveAtStage(1); // "in the rain"
        }
    }
}
