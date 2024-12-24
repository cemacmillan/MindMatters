using Verse;
using System.Collections.Generic;

namespace MindMatters
{
    public class Experience : IExposable
    {
        public string EventType;               // The type of event
        public ExperienceValency Valency;      // The valency of the experience
        public int Timestamp;                  // When the event occurred (in game ticks)
        public HashSet<string> Flags;          // Optional flags/tags providing additional context

        public Experience() 
        {
            Flags = new HashSet<string>(); // Initialize to avoid null references
        }

        public Experience(string eventType, ExperienceValency valency)
        {
            EventType = eventType;
            Valency = valency;
            Timestamp = Find.TickManager.TicksGame;
            Flags = new HashSet<string>();
        }

        public Experience(string eventType, ExperienceValency valency, HashSet<string> flags)
        {
            EventType = eventType;
            Valency = valency;
            Timestamp = Find.TickManager.TicksGame;
            Flags = flags ?? new HashSet<string>();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref EventType, "EventType");
            Scribe_Values.Look(ref Valency, "Valency");
            Scribe_Values.Look(ref Timestamp, "Timestamp");
            Scribe_Collections.Look(ref Flags, "Flags", LookMode.Value);
        }
    }
}