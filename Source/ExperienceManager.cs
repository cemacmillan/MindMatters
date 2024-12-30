using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public enum ExperienceValency
{
    Positive,     // Explicitly good events (compliments, achievements, gifts)
    Negative,     // Explicitly bad events (insults, failures, trauma)
    Neutral,      // Neutral events (routine work, non-controversial ceremonies)
    Eldritch,     // Mysterious, unsettling, or reality-altering events
    Affirming,    // Experiences reinforcing norms, roles, or ideoligions
    Humiliating,  // Events that degrade dignity, self-worth, or social standing
    Exhilarating, // Thrilling, adrenaline-pumping experiences (victories, near misses)
    Transformative // Events that fundamentally change the pawn's self-concept
}

public class MindMattersExperienceComponent : GameComponent
{
    public static MindMattersExperienceComponent Instance { get; private set; } // Singleton instance

    private Dictionary<Pawn, List<Experience>> pawnExperiences = new();

    public MindMattersExperienceComponent(Game game)
    {
        Instance = this; // Initialize the singleton instance
        MindMattersUtilities.DebugLog("[MindMattersExperienceComponent] Initialized.");
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (Instance == null)
        {
            Instance = this;
            MindMattersUtilities.DebugLog("[MindMattersExperienceComponent] FinalizeInit completed, instance assigned.");
        }
    }
    
    public static MindMattersExperienceComponent GetOrCreateInstance()
    {
        if (Instance == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] Instance is null. Initializing a new instance.");
            Instance = new MindMattersExperienceComponent(Current.Game);
        }

        return Instance;
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();

        // Every quarter day (3 hours)
        if (Find.TickManager.TicksGame % 7500 == 0)
        {
            int expireThreshold = Find.TickManager.TicksGame - GenDate.TicksPerDay;
            ExpireOldExperiences(expireThreshold);
        }

        // Check for therapy-related thoughts once a day
        if (Find.TickManager.TicksGame % 60000 == 0)
        {
            CheckForTherapyThoughtsAndAddExperiences();
        }
    }

    private void ExpireOldExperiences(int expireThreshold, bool isReloaded = false)
    {
        foreach (var pair in pawnExperiences.ToList())
        {
            Pawn pawn = pair.Key;
            List<Experience> experiences = pair.Value;

            if (pawn == null || experiences == null)
            {
                pawnExperiences.Remove(pawn);
                continue;
            }

            experiences.RemoveAll(exp => exp.Timestamp < expireThreshold);

            if (experiences.Count == 0)
            {
                pawnExperiences.Remove(pawn);
            }
        }

        if (!isReloaded)
        {
            MindMattersUtilities.DebugLog("[MindMattersExperienceComponent] Expired old experiences.");
        }
    }

    public List<Experience> GetOrCreateExperiences(Pawn pawn)
    {
        if (pawn == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] GetOrCreateExperiences called with null pawn.");
            return new List<Experience>();
        }

        // Fixme Inline or verify declaration sense.
        List<Experience> experiences;
        if (!pawnExperiences.TryGetValue(pawn, out experiences))
        {
            experiences = new List<Experience>();
            pawnExperiences[pawn] = experiences;
        }

        return experiences;
    }

    public void AddExperience(Pawn pawn, Experience experience)
    {
        if (pawn == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] AddExperience called with null pawn.");
            return;
        }

        if (experience == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] AddExperience called with null experience.");
            return;
        }

        var experiences = GetOrCreateExperiences(pawn);
        experiences.Add(experience);

        MindMattersUtilities.DebugLog($"[MindMattersExperienceComponent] Added experience {experience.EventType} ({experience.Valency}) for {pawn.LabelShort}");
    }

    private void CheckForTherapyThoughtsAndAddExperiences()
    {
        foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
        {
            List<Thought_Memory> pawnMemories = pawn.needs.mood.thoughts.memories.Memories;

            foreach (Thought_Memory memory in pawnMemories)
            {
                if (IsTherapyRelated(memory))
                {
                    AddExperience(pawn, new Experience("Therapy", ExperienceValency.Positive));
                    break;
                }
            }
        }
    }

    private bool IsTherapyRelated(Thought thought)
    {
        return thought.def.defName == "TherapyRelieved";
    }

    public void OnPawnDowned(Pawn? pawn)
    {
        if (pawn == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] OnPawnDowned called with null pawn.");
            return;
        }

        AddExperience(pawn, new Experience("PawnDowned", ExperienceValency.Negative));
        MindMattersUtilities.DebugLog($"[MindMattersExperienceComponent] {pawn.LabelShort} has been downed.");
    }

    public void OnPawnKilled(Pawn killer)
    {
        if (killer == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] OnPawnKilled called with null killer.");
            return;
        }

        AddExperience(killer, new Experience("PawnKilled", ExperienceValency.Neutral));
        MindMattersUtilities.DebugLog($"[MindMattersExperienceComponent] {killer.LabelShort} killed a pawn.");
    }

    public List<Experience> GetPawnExperiences(Pawn pawn)
    {
        return pawn == null ? new List<Experience>() : GetOrCreateExperiences(pawn);
    }

    public void OnColonistDied(Pawn colonist)
    {
        if (colonist == null)
        {
            MindMattersUtilities.DebugWarn("[MindMattersExperienceComponent] OnColonistDied called with null colonist.");
            return;
        }

        foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
        {
            if (pawn == colonist) continue;

            bool isRelated = pawn.relations?.RelatedPawns?.Contains(colonist) == true;
            string experienceType = isRelated ? "ColonistDiedRelation" : "ColonistDied";

            AddExperience(pawn, new Experience(experienceType, ExperienceValency.Negative));
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        // Create lists for keys and values
        List<Pawn> keys = pawnExperiences.Keys.ToList();
        List<List<Experience>> values = pawnExperiences.Values.ToList();

        // Use Scribe to look up keys and values
        Scribe_Collections.Look(ref keys, "pawnExperienceKeys", LookMode.Reference);
        Scribe_Collections.Look(ref values, "pawnExperienceValues", LookMode.Deep);

        // Reconstruct dictionary on loading
        if (Scribe.mode == LoadSaveMode.LoadingVars && keys != null && values != null)
        {
            pawnExperiences = keys.Zip(values, (key, value) => new { key, value })
                .ToDictionary(pair => pair.key, pair => pair.value);
        }
    }
    
    public void NotExposeData()
    {
        base.ExposeData();

        Scribe_Collections.Look(ref pawnExperiences, "pawnExperiences", LookMode.Reference, LookMode.Deep);
    }
}
