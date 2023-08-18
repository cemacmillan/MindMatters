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
            TraitDef.Named("Desensitized")
            // Add other traits here
        };

        public void ProcessOutcomes()
        {
            // Get the ExperienceManager from the Current.Game object
            var experienceManager = Current.Game.GetComponent<MindMattersExperienceComponent>();

            if (experienceManager == null)
            {
                Log.Error("ExperienceManager is null.");
                return;
            }

            // Loop through all colonist pawns
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                // Get the experiences for this pawn
                if (experienceManager.pawnExperiences.TryGetValue(pawn, out List<Experience> experiences))
                {
                    if (HasImmunizingTrait(pawn))
                    {
                        continue;
                    }
                    // Process each experience
                    foreach (Experience experience in experiences)
                    {
                        bool outcomeOccurred = false;  // Flag to track if an outcome has occurred for this experience

                        // If the pawn has a "Sensitive" trait, they may be more likely to develop anxiety from negative experiences
                        if (!outcomeOccurred && experience.Valency == ExperienceValency.Negative && HasAnxietyProneTrait(pawn))
                        {
                            // Roll for anxiety
                            outcomeOccurred = TryDevelopAnxiety(pawn, anxietyFactorAnxious);  // 10% chance
                        }
                        else
                        {
                            // Try to develop anxiety for 1/4 the frequency of pawns with HasAnxietyProneTrait
                            outcomeOccurred = TryDevelopAnxiety(pawn, anxietyFactor);  // 2.5% chance
                        }

                        // Roll for trauma
                        if (!outcomeOccurred)
                        {
                            outcomeOccurred = TryDevelopTrauma(pawn, traumaFactor);  // 10% chance
                        }

                        if (!outcomeOccurred)
                        {
                            // Roll for gain/lose trait
                            if (Rand.Value < traitFactor) 
                            {
                                // Roll for gain or lose trait
                                if (Rand.Value < 0.5f)  // 50% chance
                                {
                                    GainTrait(pawn);
                                }
                                else
                                {
                                    LoseTrait(pawn);
                                }
                            }
                        }
                    }

                    // Clear the list of experiences for the pawn
                    experiences.Clear();
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

            // Log for debugging
            Log.Message($"{pawn.Name} gained the {newTrait.Label} trait due to an experience.");
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

                // Log for debugging
                Log.Message($"{pawn.Name} lost the {traitToLose.Label} trait due to a negative experience.");
            }
        }

        private bool TryDevelopTrauma(Pawn pawn, float chance)
        {
            if (Rand.Value < chance)
            {
                // Check if the pawn already has trauma
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Trauma")))
                {
                    // Get the existing trauma hediff
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Trauma"));

                    // Increase the severity
                    hediff.Severity += Rand.Range(0.05f, 0.15f);

                    // Log for debugging
                    Log.Message($"{pawn.Name}'s trauma worsened due to negative experience.");
                    
                }
                else
                {
                    // Develop trauma
                    var hediff = HediffMaker.MakeHediff(HediffDef.Named("Trauma"), pawn);
                    hediff.Severity = Rand.Range(0.1f, 0.4f);  // Set initial severity
                    pawn.health.AddHediff(hediff);

                    // Log for debugging
                    Log.Message($"{pawn.Name}'s trauma developed due to negative experience.");
                }

                return true;  // Return true to indicate that the outcome occurred
            }

            return false;  // Return false if the outcome did not occur
        }
        private bool TryDevelopAnxiety(Pawn pawn, float chance)
        {
            if (Rand.Value < chance)
            {
                // Check if the pawn already has anxiety
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Anxiety")))
                {
                    // Get the existing anxiety hediff
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Anxiety"));

                    // Increase the severity
                    hediff.Severity += Rand.Range(0.05f, 0.15f);

                    // Log for debugging
                    Log.Message($"{pawn.Name}'s anxiety worsened due to negative experience.");
                }
                else
                {
                    // Develop anxiety
                    var hediff = HediffMaker.MakeHediff(HediffDef.Named("Anxiety"), pawn);
                    hediff.Severity = Rand.Range(0.1f, 0.4f);  // Set initial severity
                    pawn.health.AddHediff(hediff);

                    // Log for debugging
                    Log.Message($"{pawn.Name}'s anxiety developed due to negative experience.");
                }

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

