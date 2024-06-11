using RimWorld;
using Verse;

namespace MindMatters
{
    public class PersonalityMatrixHandler
    {
        public PersonalityMatrix CreatePersonalityMatrix(Pawn pawn)
        {
            PersonalityMatrix personalityMatrix = new PersonalityMatrix();

            // Adjust for traits.
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                personalityMatrix.AdjustForTrait(trait);
            }

            // Adjust for skills.
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                personalityMatrix.AdjustForSkill(skill);
            }

            // Adjust for capabilities.
            personalityMatrix.AdjustForCapabilities(pawn);

            return personalityMatrix;
        }
    }
}

