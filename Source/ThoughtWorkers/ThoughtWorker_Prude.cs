using RimWorld;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Prude : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.RaceProps.Humanlike || p.story == null || p.story.traits == null)
                return ThoughtState.Inactive;

            if (p.story.traits.HasTrait(MindMattersTraits.Prude))
            {
                if (p.Awake() && p.CurJob != null && p.CurJob.def == JobDefOf.LayDown && p.CurJob.targetA.Thing != null && p.CurJob.targetA.Thing.def.IsBed)
                {
                    // Prude pawn is trying to rest on a bed, but is uncomfortable due to nudity
                    return ThoughtState.ActiveAtStage(0);
                }

                if (p.apparel != null)
                {
                    foreach (Apparel apparel in p.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) && apparel.HitPoints < apparel.MaxHitPoints)
                        {
                            // Prude pawn is wearing worn-out apparel
                            return ThoughtState.ActiveAtStage(1);
                        }

                        if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) && apparel.HitPoints < apparel.MaxHitPoints * 0.5f)
                        {
                            // Prude pawn is wearing tattered apparel
                            return ThoughtState.ActiveAtStage(2);
                        }
                    }
                }
            }

            return ThoughtState.Inactive;
        }
    }
}