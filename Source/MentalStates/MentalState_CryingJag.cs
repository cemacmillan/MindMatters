using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using RimWorld.Planet;

namespace MindMatters
{
    public class MentalState_CryingJag : MentalState_BabyFit
    {
        private const float CryingJagRadius = 5f;
        private const int CryingInterval = 150;

        private float lastCryingTick = -1f;
        private List<Pawn> alreadyAffectedPawns = new List<Pawn>(32);

        protected override void AuraEffect(Thing source, Pawn hearer)
        {
            // Skip over psychopathic, desensitized pawns or pawns without mood
            if (hearer.story != null && (hearer.story.traits.HasTrait(TraitDefOf.Psychopath) || hearer.story.traits.HasTrait(MindMattersTraits.Desensitized)) || hearer.needs?.mood == null)
            {
                return;
            }

            hearer.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("MM_WatchingSomeoneCry"), pawn);
        }

        public override void MentalStateTick()
        {
            base.MentalStateTick();
            if ((float)Find.TickManager.TicksGame > lastCryingTick + CryingInterval && !pawn.IsWorldPawn())
            {
                GenClamor.DoClamor(pawn, CryingJagRadius, ApplyEffect);
                lastCryingTick = Find.TickManager.TicksGame;
            }
        }

        private void ApplyEffect(Thing source, Pawn? hearer)
        {
            if (hearer == null)
            {
                return;
            }

            if (hearer != source && !alreadyAffectedPawns.Contains(hearer))
            {
                alreadyAffectedPawns.Add(hearer);
                AuraEffect(source, hearer); // hearer is not null here
            }
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastCryingTick, "lastCryingTick", 0f);
            Scribe_Collections.Look(ref alreadyAffectedPawns, "alreadyAffectedPawns", LookMode.Reference);
        }
    }
}