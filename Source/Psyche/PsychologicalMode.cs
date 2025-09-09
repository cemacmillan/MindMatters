using System;

namespace MindMatters
{
    /// <summary>
    /// Psychological modes that represent different ways of processing and expressing experiences.
    /// These are the "voices" through which pawns interpret and respond to their world.
    /// </summary>
    public enum PsychologicalMode
    {
        Oracle,      // Prophetic - sees patterns, gives advice, mystical
        Analyst,     // Strategic - plans, analyzes, methodical
        Pragmatic,   // Practical - immediate needs, resource-focused
        Reflective,  // Confessional - processes emotions, seeks therapy
        Chaotic,     // Misbehaving - disruptive, rule-breaking
        Survivalist, // Explorer - adventurous, risk-taking
        Builder,     // Creative - construction, planning, vision
        Social       // Communicative - relationships, group-focused
    }

    /// <summary>
    /// Extension methods for PsychologicalMode enum
    /// </summary>
    public static class PsychologicalModeExtensions
    {
        /// <summary>
        /// Gets the total number of psychological modes
        /// </summary>
        public static int Count => Enum.GetValues(typeof(PsychologicalMode)).Length;

        /// <summary>
        /// Gets all mode names as strings
        /// </summary>
        public static string[] GetModeNames()
        {
            return Enum.GetNames(typeof(PsychologicalMode));
        }

        /// <summary>
        /// Gets a human-readable description of the mode
        /// </summary>
        public static string GetDescription(this PsychologicalMode mode)
        {
            return mode switch
            {
                PsychologicalMode.Oracle => "Sees patterns others miss, gives cryptic advice, drawn to mysterious events",
                PsychologicalMode.Analyst => "Plans ahead, analyzes situations, methodical problem-solving",
                PsychologicalMode.Pragmatic => "Focuses on immediate needs, resource management, practical solutions",
                PsychologicalMode.Reflective => "Processes emotions, seeks therapy, values relationships",
                PsychologicalMode.Chaotic => "Ignores social norms, creates mischief, challenges authority",
                PsychologicalMode.Survivalist => "Explores, takes risks, seeks new experiences",
                PsychologicalMode.Builder => "Loves construction, planning, creating",
                PsychologicalMode.Social => "Values relationships, seeks social interaction",
                _ => "Unknown mode"
            };
        }
    }
}
