using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using MindMattersInterface;

namespace MindMatters;


public class MindMattersExperienceComponent : GameComponent, IMindMattersExperienceComponent
{
    public static MindMattersExperienceComponent Instance { get; private set; } // Singleton instance

    private Dictionary<Pawn, List<Experience>> pawnExperiences = new();
    private MindMattersPsyche psycheSystem;

    // Interface events
    public event System.Action<Pawn, MindMattersInterface.Experience> OnExperienceAdded;
    public event System.Action<Pawn, MindMattersInterface.Experience> OnExperienceRemoved;

    public MindMattersExperienceComponent(Game game)
    {
        Instance = this; // Initialize the singleton instance
        //MMToolkit.DebugLog("[MindMattersExperienceComponent] Initialized.");
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (Instance == null)
        {
            Instance = this;
            //MMToolkit.DebugLog("[MindMattersExperienceComponent] FinalizeInit completed, instance assigned.");
        }
        
        // Initialize Psyche system
        InitializePsycheSystem();
    }
    
    public static MindMattersExperienceComponent GetOrCreateInstance()
    {
        if (Instance == null)
        {
           //MMToolkit.DebugWarn("[MindMattersExperienceComponent] Instance is null. Initializing a new instance.");
            Instance = new MindMattersExperienceComponent(Current.Game);
        }

        return Instance;
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();

        // Check if TickManager is available
        if (Find.TickManager == null) return;

        int currentTick = Find.TickManager.TicksGame;

        // Every quarter day (3 hours)
        if (currentTick % 7500 == 0)
        {
            int expireThreshold = currentTick - GenDate.TicksPerDay;
            ExpireOldExperiencesInternal(expireThreshold);
        }

        // Check for therapy-related thoughts once a day
        if (currentTick % 60000 == 0)
        {
            CheckForTherapyThoughtsAndAddExperiences();
        }
    }

    private void ExpireOldExperiencesInternal(int expireThreshold, bool isReloaded = false)
    {
        foreach (KeyValuePair<Pawn, List<Experience>> pair in pawnExperiences.ToList())
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
            MMToolkit.DebugLog("[MindMattersExperienceComponent] Expired old experiences.");
        }
    }

    public List<Experience> GetOrCreateExperiences(Pawn pawn)
    {
        if (pawn == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] GetOrCreateExperiences called with null pawn.");
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
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] AddExperience called with null pawn.");
            return;
        }

        if (experience == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] AddExperience called with null experience.");
            return;
        }

        List<Experience> experiences = GetOrCreateExperiences(pawn);
        experiences.Add(experience);

        // Trigger the interface event
        OnExperienceAdded?.Invoke(pawn, ConvertToInterfaceExperience(experience));

                // Process experience through Psyche system
                if (psycheSystem != null)
                {
                    psycheSystem.ProcessExperience(pawn, ConvertToInterfaceExperience(experience));
                }

        MMToolkit.DebugLog($"[MindMattersExperienceComponent] Added experience {experience.EventType} ({experience.Valency}) for {pawn.LabelShort}");
    }

    // Interface method: AddExperience for MindMattersInterface.Experience
    public void AddExperience(Pawn pawn, MindMattersInterface.Experience experience)
    {
        if (pawn == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] AddExperience called with null pawn.");
            return;
        }

        if (experience == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] AddExperience called with null experience.");
            return;
        }

        // Convert to internal Experience type
        Experience internalExperience = ConvertFromInterfaceExperience(experience);
        List<Experience> experiences = GetOrCreateExperiences(pawn);
        experiences.Add(internalExperience);

        // Trigger the interface event
        OnExperienceAdded?.Invoke(pawn, experience);

        MMToolkit.DebugLog($"[MindMattersExperienceComponent] Added experience {experience.EventType} ({experience.Valency}) for {pawn.LabelShort}");
    }

    // Interface method: RemoveExperience
    public bool RemoveExperience(Pawn pawn, string eventType)
    {
        if (pawn == null || string.IsNullOrEmpty(eventType))
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] RemoveExperience called with invalid parameters.");
            return false;
        }

        if (!pawnExperiences.TryGetValue(pawn, out var experiences))
        {
            return false;
        }

        var experienceToRemove = experiences.FirstOrDefault(exp => exp.EventType == eventType);
        if (experienceToRemove != null)
        {
            experiences.Remove(experienceToRemove);
            OnExperienceRemoved?.Invoke(pawn, ConvertToInterfaceExperience(experienceToRemove));
            return true;
        }

        return false;
    }

    // Interface method: GetPawnExperiences (already exists below, just need to make it public)

    // Interface method: ExpireOldExperiences (public wrapper for the private method)
    public void ExpireOldExperiences(int expireThreshold)
    {
        ExpireOldExperiencesInternal(expireThreshold, false);
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


    public void OnPawnDowned(Pawn? pawn)
    {
        if (pawn == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] OnPawnDowned called with null pawn.");
            return;
        }

        AddExperience(pawn, new Experience("PawnDowned", ExperienceValency.Negative));
        MMToolkit.DebugLog($"[MindMattersExperienceComponent] {pawn.LabelShort} has been downed.");
    }

    public void OnPawnKilled(Pawn killer)
    {
        if (killer == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] OnPawnKilled called with null killer.");
            return;
        }

        AddExperience(killer, new Experience("PawnKilled", ExperienceValency.Neutral));
        MMToolkit.DebugLog($"[MindMattersExperienceComponent] {killer.LabelShort} killed a pawn.");
    }

    public List<MindMattersInterface.Experience> GetPawnExperiences(Pawn pawn)
    {
        if (pawn == null) return new List<MindMattersInterface.Experience>();
        
        var internalExperiences = GetOrCreateExperiences(pawn);
        return internalExperiences.Select(ConvertToInterfaceExperience).ToList();
    }

    public void OnColonistDied(Pawn colonist)
    {
        if (colonist == null)
        {
            MMToolkit.DebugWarn("[MindMattersExperienceComponent] OnColonistDied called with null colonist.");
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

    // Interface method: IsTherapyRelated (already exists, just need to make it public)
    public bool IsTherapyRelated(Thought thought)
    {
        return thought.def.defName == "TherapyRelieved";
    }

    // Conversion methods between MindMatters.Experience and MindMattersInterface.Experience
    private MindMattersInterface.Experience ConvertToInterfaceExperience(Experience experience)
    {
        return new MindMattersInterface.Experience(experience.EventType, ConvertToInterfaceValency(experience.Valency))
        {
            Flags = experience.Flags,
            Timestamp = experience.Timestamp
        };
    }

    private Experience ConvertFromInterfaceExperience(MindMattersInterface.Experience experience)
    {
        return new Experience(experience.EventType, ConvertFromInterfaceValency(experience.Valency), experience.Flags)
        {
            Timestamp = experience.Timestamp
        };
    }

    // Conversion methods for ExperienceValency enums
    private MindMattersInterface.ExperienceValency ConvertToInterfaceValency(ExperienceValency valency)
    {
        return valency switch
        {
            ExperienceValency.Positive => MindMattersInterface.ExperienceValency.Positive,
            ExperienceValency.Negative => MindMattersInterface.ExperienceValency.Negative,
            ExperienceValency.Neutral => MindMattersInterface.ExperienceValency.Neutral,
            ExperienceValency.Eldritch => MindMattersInterface.ExperienceValency.Eldritch,
            ExperienceValency.Affirming => MindMattersInterface.ExperienceValency.Affirming,
            ExperienceValency.Humiliating => MindMattersInterface.ExperienceValency.Humiliating,
            ExperienceValency.Exhilarating => MindMattersInterface.ExperienceValency.Exhilarating,
            ExperienceValency.Transformative => MindMattersInterface.ExperienceValency.Transformative,
            _ => MindMattersInterface.ExperienceValency.Neutral
        };
    }

    private ExperienceValency ConvertFromInterfaceValency(MindMattersInterface.ExperienceValency valency)
    {
        return valency switch
        {
            MindMattersInterface.ExperienceValency.Positive => ExperienceValency.Positive,
            MindMattersInterface.ExperienceValency.Negative => ExperienceValency.Negative,
            MindMattersInterface.ExperienceValency.Neutral => ExperienceValency.Neutral,
            MindMattersInterface.ExperienceValency.Eldritch => ExperienceValency.Eldritch,
            MindMattersInterface.ExperienceValency.Affirming => ExperienceValency.Affirming,
            MindMattersInterface.ExperienceValency.Humiliating => ExperienceValency.Humiliating,
            MindMattersInterface.ExperienceValency.Exhilarating => ExperienceValency.Exhilarating,
            MindMattersInterface.ExperienceValency.Transformative => ExperienceValency.Transformative,
            _ => ExperienceValency.Neutral
        };
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
    
    private void InitializePsycheSystem()
    {
        if (psycheSystem == null)
        {
            psycheSystem = Current.Game.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null)
            {
                // Create and add the Psyche system if it doesn't exist
                psycheSystem = new MindMattersPsyche(Current.Game);
                Current.Game.components.Add(psycheSystem);
                MMToolkit.DebugLog("[MindMattersExperienceComponent] Created MindMattersPsyche system");
            }
        }
    }
}
