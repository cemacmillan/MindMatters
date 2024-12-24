using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class ConstraintNeed : DynamicNeed
    {
        public ConstraintNeed() : base()
        {
        }

        public ConstraintNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef)
        {
        }
        
        [JetBrains.Annotations.UsedImplicitlyAttribute]
        public override void Initialize(Pawn pawn, NeedDef def)
        {
            base.Initialize(pawn, def);
        }


        protected override void UpdateValue()
        {
            // Example: Penalize need if wearing "restrictive" gear
            var apparel = pawn.apparel?.WornApparel;
            if (apparel != null)
            {
                CurLevel = 1f - apparel.Sum(a => a.def.GetStatValueAbstract(StatDefOf.Comfort, null)) / 10f;
            }
        }

        public override string GetTipString()
        {
            return $"Constraint: {(CurLevel * 100f):F0}%\n"
                   + "This pawn becomes unhappy when wearing restrictive clothing.";
        }
    }
}