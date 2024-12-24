using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

[StaticConstructorOnStartup]
public static class DynamicNeedFactory
{
    private static readonly Dictionary<Type, DynamicNeedProperties> Registry = new();
    private static readonly Dictionary<Type, DynamicNeedsBitmap> BitmapMap = new();
    private static bool debugLogCalled = false;

    static DynamicNeedFactory()
    {
        RegisterDynamicNeeds();
    }

    public static bool ShouldPawnHaveNeed(Pawn pawn, Type needType)
    {
        if (!Registry.TryGetValue(needType, out var props))
        {
            return false;
        }

        var needDef = GetNeedDef(needType);
        if (needDef == null)
        {
            return false;
        }

        return ShouldHaveNeed(pawn, needDef);
    }

    public static IEnumerable<DynamicNeedProperties> GetNeedsForCategory(DynamicNeedCategory? category = null)
    {
        return Registry.Values.Where(props => !category.HasValue || props.Category == category.Value);
    }

    public static DynamicNeedProperties GetProperties(Type needType)
    {
        return Registry.TryGetValue(needType, out var props) ? props : null;
    }

    public static bool HasNeed(Type needType)
    {
        return Registry.ContainsKey(needType);
    }

    public static DynamicNeedProperties GetPropertiesForBitmap(DynamicNeedsBitmap bitmap)
    {
        var needType = BitmapMap.FirstOrDefault(pair => pair.Value == bitmap).Key;
        return needType != null && Registry.TryGetValue(needType, out var props) ? props : null;
    }

    public static DynamicNeedsBitmap GetBitmapForNeed(Type needClass)
    {
        return BitmapMap.TryGetValue(needClass, out var bitmap) ? bitmap : DynamicNeedsBitmap.None;
    }

    private static void RegisterDynamicNeeds()
    {
        RegisterNeed<FreshFruitNeed>(
            "FreshFruitNeed", "Fresh Fruit",
            "This pawn craves fresh fruit to stay happy.",
            DynamicNeedCategory.Luxury,
            DynamicNeedsBitmap.FreshFruit,
            pawn => pawn.story?.traits?.HasTrait(MindMattersTraitDef.SelfCentered) == true &&
                    pawn.story.traits.DegreeOfTrait(MindMattersTraitDef.NaturalMood) >= 1
        );

        RegisterNeed<FormalityNeed>(
            "FormalityNeed", "Formality",
            "This pawn gains satisfaction from wearing formal or restrictive attire.",
            DynamicNeedCategory.Primal,
            DynamicNeedsBitmap.Formality,
            pawn => pawn.story?.traits?.HasTrait(MindMattersTraitDef.Submissive) == true ||
                    pawn.story?.traits.HasTrait(MindMattersTraitDef.Reserved) == true ||
                    pawn.story?.traits.HasTrait(MindMattersTraitDef.Perfectionist) == true
        );

        RegisterNeed<ConstraintNeed>(
            "ConstraintNeed", "Constraint",
            "This pawn feels more comfortable in restrictive environments.",
            DynamicNeedCategory.Quirk,
            DynamicNeedsBitmap.Constraint,
            pawn => (pawn.story?.traits?.HasTrait(MindMattersTraitDef.Submissive) == true &&
                     pawn.story.traits.HasTrait(MindMattersTraitDef.Masochist) == true) ||
                    pawn.story?.traits.HasTrait(MindMattersTraitDef.Prude) == true
        );
    }

    public static void RegisterNeed<TNeed>(
        string defName,
        string label,
        string description,
        DynamicNeedCategory category,
        DynamicNeedsBitmap bitmap,
        Func<Pawn, bool> pawnFilter,
        int listPriority = 100,
        Intelligence minIntelligence = Intelligence.Humanlike,
        List<DevelopmentalStage> developmentalStageFilter = null,
        bool colonistAndPrisonersOnly = true,
        bool freezeWhileSleeping = true,
        bool freezeInMentalState = false,
        float baseLevel = 0.5f,
        float seekerRisePerHour = 0.8f,
        float seekerFallPerHour = 0.23f,
        bool major = false
    ) where TNeed : DynamicNeed, new()
    {
        var needType = typeof(TNeed);
        if (Registry.ContainsKey(needType))
        {
            return;
        }

        var stageFilter = developmentalStageFilter?.Aggregate(DevelopmentalStage.None, (current, stage) => current | stage) 
                          ?? DevelopmentalStage.Adult;

        var needDef = new NeedDef
        {
            defName = defName,
            label = label,
            description = description,
            needClass = needType,
            colonistAndPrisonersOnly = colonistAndPrisonersOnly,
            minIntelligence = minIntelligence,
            listPriority = listPriority,
            freezeWhileSleeping = freezeWhileSleeping,
            freezeInMentalState = freezeInMentalState,
            major = major,
            baseLevel = baseLevel,
            seekerRisePerHour = seekerRisePerHour,
            seekerFallPerHour = seekerFallPerHour,
            developmentalStageFilter = stageFilter
        };

        Registry[needType] = new DynamicNeedProperties
        {
            NeedClass = needType,
            Label = label,
            Description = description,
            Category = category,
            PawnFilter = pawnFilter
        };
        BitmapMap[needType] = bitmap;

        DefDatabase<NeedDef>.Add(needDef);
    }

    public static DynamicNeed CreateNeedInstance(Type needType, Pawn pawn)
    {
        if (!Registry.ContainsKey(needType))
        {
            return null;
        }

        try
        {
            NeedDef def = GetNeedDef(needType);
            if (def == null)
            {
                return null;
            }

            if (Activator.CreateInstance(needType) is DynamicNeed need)
            {
                need.Initialize(pawn, def);
                return need;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static NeedDef GetNeedDef(Type needType)
    {
        if (Registry.TryGetValue(needType, out var props))
        {
            string defName = needType.Name;
            return DefDatabase<NeedDef>.GetNamedSilentFail(defName);
        }

        return null;
    }

    public static bool ShouldHaveNeed(Pawn pawn, NeedDef needDef)
    {
        if (pawn == null || !pawn.RaceProps.Humanlike)
        {
            return false;
        }

        if (!DynamicNeedFactory.Registry.ContainsKey(needDef.needClass))
        {
            return false;
        }

        if (pawn.story?.traits != null)
        {
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (!trait.Suppressed && trait.CurrentData?.disablesNeeds?.Contains(needDef) == true)
                {
                    return false;
                }
            }
        }

        if (needDef.nullifyingPrecepts != null && pawn.Ideo != null)
        {
            foreach (PreceptDef precept in needDef.nullifyingPrecepts)
            {
                if (pawn.Ideo.HasPrecept(precept))
                {
                    return false;
                }
            }
        }

        if (ModsConfig.BiotechActive)
        {
            if (pawn.genes?.GenesListForReading != null)
            {
                foreach (Gene gene in pawn.genes.GenesListForReading)
                {
                    if (gene.Active && gene.def.disablesNeeds?.Contains(needDef) == true)
                    {
                        return false;
                    }
                }
            }
        }

        var needProps = DynamicNeedFactory.GetProperties(needDef.needClass);
        return needProps?.PawnFilter?.Invoke(pawn) ?? true;
    }
    public static void DebugLogRegisteredDynamicNeeds()
    {
        if (debugLogCalled)
        {
            // MindMattersUtilities.DebugLog("[DynamicNeedFactory] DebugLogRegisteredDynamicNeeds already called.");
            return;
        }

        debugLogCalled = true; // Set the flag to true
        MindMattersUtilities.DebugLog("[DynamicNeedFactory] Verifying registered DynamicNeeds...");

        if (Registry.Keys.Count == 0)
        {
            MindMattersUtilities.DebugLog("[DynamicNeedFactory] Registry is empty. No DynamicNeeds registered.");
            return;
        }

        foreach (var needType in Registry.Keys)
        {
            var needDef = DefDatabase<NeedDef>.GetNamedSilentFail(needType.Name);
            if (needDef == null)
            {
                MindMattersUtilities.DebugLog($"[DynamicNeedFactory] NeedDef for '{needType.Name}' not found in DefDatabase.");
            }
            else if (typeof(DynamicNeed).IsAssignableFrom(needDef.needClass)) // Filter for DynamicNeeds
            {
                MindMattersUtilities.DebugLog($"[DynamicNeedFactory] DynamicNeed found: {needDef.defName}");
                MindMattersUtilities.DebugLog($"  Label: {needDef.label}");
                MindMattersUtilities.DebugLog($"  Description: {needDef.description}");
                MindMattersUtilities.DebugLog($"  NeedClass: {needDef.needClass?.Name}");
                MindMattersUtilities.DebugLog($"  MinIntelligence: {needDef.minIntelligence}");
                MindMattersUtilities.DebugLog($"  BaseLevel: {needDef.baseLevel}");
                MindMattersUtilities.DebugLog($"  FreezeWhileSleeping: {needDef.freezeWhileSleeping}");
            }
        }
        
        foreach (var needDef in DefDatabase<NeedDef>.AllDefs)
        {
            if (needDef.defName == "FreshFruitNeed")
            {
                MindMattersUtilities.DebugLog($"NeedDef '{needDef.defName}' references NeedClass: {needDef.needClass}");
            }
        }
    }
}


public class DynamicNeedProperties
{
    public Type NeedClass { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public DynamicNeedCategory Category { get; set; }
    public Func<Pawn, bool> PawnFilter { get; set; }
}