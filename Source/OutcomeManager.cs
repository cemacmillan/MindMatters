using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MindMatters
{
    public class OutcomeManager
    {
        private float anxietyFactor = 0.025f;
        private float anxietyFactorAnxious = 0.1f;
        private float traumaFactor = 0.05f;
        private float traitFactor = 0.05f;

        private readonly List<TraitDef> gainableTraits = new List<TraitDef>
        {
            MindMattersTraits.Abrasive,
            MindMattersTraits.Ascetic,
            MindMattersTraits.Bloodlust,
            MindMattersTraits.Greedy,
            MindMattersTraits.Nerves,
            MindMattersTraits.Gourmand,
            MindMattersTraits.TooSmart,
            MindMattersTraits.Neurotic,
            MindMattersTraits.Masochist,
            MindMattersTraits.NightOwl,
            MindMattersTraits.Jealous,
            MindMattersTraits.Nimble,
            MindMattersTraits.TenderHearted,
            MindMattersTraits.Desensitized,
            MindMattersTraits.Adventurous,
            MindMattersTraits.Cautious,
            MindMattersTraits.Outgoing,
            MindMattersTraits.Pluviophile,
            MindMattersTraits.Relaxed,
            MindMattersTraits.Reserved,
            MindMattersTraits.SelfCentered
            // Add the other traits here...
        };

        private readonly List<TraitDef> losableTraits = new List<TraitDef>
        {
            MindMattersTraits.Abrasive,
            MindMattersTraits.Ascetic,
            MindMattersTraits.Greedy,
            MindMattersTraits.Nerves,
            MindMattersTraits.Gourmand,
            MindMattersTraits.TooSmart,
            MindMattersTraits.Neurotic,
            MindMattersTraits.Masochist,
            MindMattersTraits.NightOwl,
            MindMattersTraits.Jealous,
            MindMattersTraits.Nimble,
            MindMattersTraits.TenderHearted,
            MindMattersTraits.Adventurous,
            MindMattersTraits.Cautious,
            MindMattersTraits.Outgoing,
            MindMattersTraits.Pluviophile,
            MindMattersTraits.Relaxed,
            MindMattersTraits.Reserved,
            MindMattersTraits.SelfCentered
            // Add the other traits here...
        };
        // List of traits that make a pawn prone to anxiety
        private readonly List<TraitDef> anxietyProneTraits = new List<TraitDef>
        {
            MindMattersTraits.TenderHearted,
            MindMattersTraits.Cautious,
            MindMattersTraits.Nerves
        // Add other traits here
        };

        private readonly List<TraitDef> immunizingTraits = new List<TraitDef>
        {
            TraitDef.Named("Psychopath"),
            MindMattersTraits.Desensitized
            // Add other traits here
        };

        public void ProcessOutcomes()
        {
            var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();
            if (experienceManager == null)
            {
                MindMattersUtilities.DebugWarn("ProcessOutcomes: ExperienceManager is null.");
                return;
            }

            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                // Use the accessor method to get experiences
                List<Experience> experiences = experienceManager.GetOrCreateExperiences(pawn);

                if (HasImmunizingTrait(pawn))
                {
                    continue;
                }

                foreach (Experience experience in experiences)
                {
                    ProcessExperienceForPawn(experience, pawn);
                }

                // Clear the list of experiences for the pawn after processing
                experiences.Clear();
                // MindMattersUtilities.DebugLog($"ProcessOutcomes: Cleared experiences for {pawn.LabelShort}.");
            }
        }
        
        private void ProcessExperienceForPawn(Experience experience, Pawn pawn)
        {
            bool outcomeOccurred = false;

            switch (experience.EventType)
            {
                case "Therapy":
                    ApplyTherapyEffects(pawn);
                    MindMattersUtilities.DebugLog("Therapizing!");
                    outcomeOccurred = true;
                    break;
            }

            if (outcomeOccurred) return;

            switch (experience.Valency)
            {
                case ExperienceValency.Neutral:
                case ExperienceValency.Negative:
                    outcomeOccurred = HandleNegativeExperience(pawn);
                    break;
                case ExperienceValency.Positive:
                    MindMattersUtilities.TryGiveRandomInspiration(pawn);
                    outcomeOccurred = true;
                    break;
            }
        }

        private bool HandleNegativeExperience(Pawn pawn)
        {
            if (HasAnxietyProneTrait(pawn))
            {
                return TryDevelopAnxiety(pawn, anxietyFactorAnxious);
            }

            if (TryDevelopAnxiety(pawn, anxietyFactor)) return true;

            if (TryDevelopTrauma(pawn, traumaFactor)) return true;

            return TryAdjustTrait(pawn);
        }

        private bool TryAdjustTrait(Pawn pawn)
        {
            if (Rand.Value < traitFactor)
            {
                if (Rand.Value < 0.5f)
                {
                    GainTrait(pawn);
                }
                else
                {
                    LoseTrait(pawn);
                }
                return true;
            }
            return false;
        }

        private void ApplyTherapyEffects(Pawn patient)
        {
            // Specify the hediffs associated with trauma and anxiety
            List<string> hediffNames = new List<string> { "Trauma", "Anxiety" };  // Add or modify the list to match your hediff defNames

            // Gradually reduce the severity of the specified hediffs
            var hediffsToTreat = patient.health.hediffSet.hediffs
                .Where(h => hediffNames.Contains(h.def.defName)).ToList();

            foreach (var hediff in hediffsToTreat)
            {
                hediff.Severity -= 0.1f;
                if (hediff.Severity <= 0) // Remove the hediff if the severity drops to 0 or below
                {
                    patient.health.RemoveHediff(hediff);
                }
            }
        }
        private void GainTrait(Pawn pawn)
        {
            // Choose a random trait to gain, weighted by the defined probabilities
            TraitDef traitToGain = gainableTraits.RandomElement();

            // Determine a valid degree for the trait
            int degree = traitToGain.degreeDatas.RandomElement().degree;

            // Create a new Trait object with the chosen TraitDef and degree
            Trait newTrait = new Trait(traitToGain, degree);

            // Add the new trait to the pawn's traits
            pawn.story.traits.GainTrait(newTrait);

            // Notify the player via a letter
            string title = "Trait Gained";
            string text = $"{pawn.Name} has gained the {newTrait.Label} trait due to an experience.";
            LetterDef letterDef = LetterDefOf.NeutralEvent;  
            Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);

            // Log for debugging
            MindMattersUtilities.DebugLog($"{pawn.Name} gained the {newTrait.Label} trait due to an experience.");
        }
        private void LoseTrait(Pawn pawn)
        {
            // If the pawn has only one trait, do not remove it
            if (pawn.story.traits.allTraits.Count <= 1)
            {
                return;
            }

            // Find the pawn's losable traits
            List<Trait> pawnLosableTraits = pawn.story.traits.allTraits
                .Where(trait => losableTraits.Contains(trait.def))
                .ToList();

            if (pawnLosableTraits.Count > 0)
            {
                // Choose a random trait to lose
                Trait traitToLose = pawnLosableTraits.RandomElement();

                // Remove the chosen trait from the pawn's traits
                pawn.story.traits.allTraits.Remove(traitToLose);

                // Notify the player via a letter
                string title = "Trait Lost";
                string text = $"{pawn.Name} has lost the {traitToLose.Label} trait due to an experience.";
                LetterDef letterDef = LetterDefOf.NeutralEvent;  // This is a neutral event as per your description
                Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);

                // Log for debugging
                MindMattersUtilities.DebugLog($"{pawn.Name} lost the {traitToLose.Label} trait due to a negative experience.");
            }
        }

        private bool TryDevelopTrauma(Pawn pawn, float chance)
        {
            if (Rand.Value < chance)
            {
                string title, text;
                // Check if the pawn already has trauma
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Trauma")))
                {
                    // Get the existing trauma hediff
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Trauma"));

                    // Increase the severity
                    hediff.Severity += Rand.Range(0.05f, 0.15f);

                    // Log for debugging
                    MindMattersUtilities.DebugLog($"{pawn.Name}'s trauma worsened due to negative experience.");

                    // Notify the player via a letter
                    title = "Trauma Worsened";
                    text = $"{pawn.Name}'s trauma has worsened due to a recent negative experience.";
                }
                else
                {
                    // Develop trauma
                    var hediff = HediffMaker.MakeHediff(HediffDef.Named("Trauma"), pawn);
                    hediff.Severity = Rand.Range(0.1f, 0.4f);  // Set initial severity
                    pawn.health.AddHediff(hediff);

                    // Log for debugging
                    MindMattersUtilities.DebugLog($"{pawn.Name}'s trauma developed due to negative experience.");

                    // Notify the player via a letter
                    title = "Trauma Developed";
                    text = $"{pawn.Name} has developed trauma due to a recent negative experience.";
                }

                LetterDef letterDef = LetterDefOf.NegativeEvent;  // This is a negative event
                Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);

                return true;  // Return true to indicate that the outcome occurred
            }

            return false;  // Return false if the outcome did not occur
        }

        private bool TryDevelopAnxiety(Pawn pawn, float chance)
        {
            if (Rand.Value < chance)
            {
                string title, text;
                // Check if the pawn already has anxiety
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Anxiety")))
                {
                    // Get the existing anxiety hediff
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Anxiety"));

                    // Increase the severity
                    hediff.Severity += Rand.Range(0.1f, 0.30f);

                    // Log for debugging
                    MindMattersUtilities.DebugLog($"{pawn.Name}'s anxiety worsened due to negative experience.");

                    // Notify the player via a letter
                    title = "Anxiety Worsened";
                    text = $"{pawn.Name}'s anxiety has worsened due to a recent negative experience.";
                }
                else
                {
                    // Develop anxiety
                    var hediff = HediffMaker.MakeHediff(HediffDef.Named("Anxiety"), pawn);
                    hediff.Severity = Rand.Range(0.1f, 0.4f);  // Set initial severity
                    pawn.health.AddHediff(hediff);

                    // Log for debugging
                    MindMattersUtilities.DebugLog($"{pawn.Name}'s anxiety developed due to negative experience.");

                    // Notify the player via a letter
                    title = "Anxiety Developed";
                    text = $"{pawn.Name} has developed anxiety due to a recent negative experience.";
                }

                LetterDef letterDef = LetterDefOf.NegativeEvent;  // This is a negative event
                Find.LetterStack.ReceiveLetter(title, text, letterDef, pawn);

                return true;  // Return true to indicate that the outcome occurred
            }

            return false;  // Return false if the outcome did not occur
        }


        private bool HasImmunizingTrait(Pawn pawn)
        {
            return pawn.story.traits.allTraits.Any(trait =>
                (trait.def == MindMattersTraits.Nerves && (trait.Degree == -1 || trait.Degree == -2)) ||
                immunizingTraits.Contains(trait.def));
        }

        private bool HasAnxietyProneTrait(Pawn pawn)
        {
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (anxietyProneTraits.Contains(trait.def))
                {
                    // If the trait is Nerves, check the degree
                    if (trait.def == MindMattersTraits.Nerves)
                    {
                        if (trait.Degree == 1 || trait.Degree == 2)  // Adjust as needed
                        {
                            return true;
                        }
                    }
                    else  // If the trait is not Nerves, it's enough to just have the trait
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

