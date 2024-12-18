using Verse;
using Verse.AI;

namespace MindMatters
{
    public class MentalStateWorker_SeekSolitude : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (pawn.IsSlave)
            {
                return false; // Prevent slaves from entering this mental state
            }
            return base.StateCanOccur(pawn);
        }
    }
}