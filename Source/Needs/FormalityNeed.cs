using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters
{
    public class FormalityNeed : DynamicNeed
    {
        public FormalityNeed(Pawn pawn, NeedDef needDef) : base(pawn, needDef) { }

        protected override void UpdateValue()
        {
            // Example: Increase need satisfaction for wearing "high formal" gear
            var apparel = pawn.apparel?.WornApparel;
            if (apparel != null)
            {
                CurLevel = apparel.Sum(a => a.def.GetStatValueAbstract(StatDefOf.Beauty, null)) / 10f;
            }
        }

        public override string GetTipString()
        {
            return $"Formality: {(CurLevel * 100f):F0}%\n"
                   + "This pawn gains satisfaction from wearing formal or restrictive attire.";
        }
    }
}