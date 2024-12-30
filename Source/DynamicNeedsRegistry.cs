using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

[StaticConstructorOnStartup]
public static class DynamicNeedsRegistry
{
    private static readonly Dictionary<Type, DynamicNeedProperties> Registry = new();
    private static readonly Dictionary<Type, DynamicNeedsBitmap> BitmapMap = new();
    private static bool debugLogCalled = false;

    /// <summary>
    /// Populate the registry from the DefDatabase.
    /// Called during game initialization.
    /// </summary>
    public static void PopulateRegistryFromDefDatabase()
    {
        foreach (NeedDef needDef in DefDatabase<NeedDef>.AllDefs)
        {
            if (typeof(DynamicNeed).IsAssignableFrom(needDef.needClass))
            {
                DynamicNeedModExtension modExtension = needDef.GetModExtension<DynamicNeedModExtension>();

                DynamicNeedProperties properties = new()
                {
                    NeedClass = needDef.needClass,
                    DefName = needDef.defName,
                    Label = needDef.label,
                    Description = needDef.description,
                    Category = modExtension?.Category ?? DynamicNeedCategory.Secondary,
                    Bitmap = modExtension?.BitmapOverride ?? ResolveBitmapFromDefName(needDef.defName),
                    PawnFilter = _ => true // Default to always true for now
                };

                // Register the need
                Register(properties);
            }
        }

        // Optional debug output
        DebugLogRegisteredDynamicNeeds();
    }

    /// <summary>
    /// Register a new dynamic need into the registry.
    /// </summary>
    public static void Register(DynamicNeedProperties properties)
    {
        if (Registry.ContainsKey(properties.NeedClass))
            return; // Already registered

        Registry[properties.NeedClass] = properties;
        BitmapMap[properties.NeedClass] = properties.Bitmap;
    }

    /// <summary>
    /// Retrieve all registered DynamicNeedProperties.
    /// </summary>
    public static IEnumerable<DynamicNeedProperties> GetAllProperties()
    {
        return Registry.Values;
    }

    /// <summary>
    /// If you want to see if the registry has a given dynamic-need type.
    /// </summary>
    public static bool HasNeed(Type needType)
    {
        return Registry.ContainsKey(needType);
    }

    /// <summary>
    /// Retrieve properties for a specific need type.
    /// </summary>
    public static DynamicNeedProperties GetProperties(Type needType)
    {
        return Registry.TryGetValue(needType, out DynamicNeedProperties props) ? props : null;
    }
    
    public static DynamicNeedProperties GetPropertiesForBitmap(DynamicNeedsBitmap bitmap)
    {
        KeyValuePair<Type, DynamicNeedsBitmap> foundKvp = BitmapMap.FirstOrDefault(pair => pair.Value == bitmap);

        if (foundKvp.Key == null)
            return null; // No matching type found

        if (!Registry.TryGetValue(foundKvp.Key, out DynamicNeedProperties props))
            return null; // No properties in the registry for this type

        return props; // Found and valid
    }
    
    /// <summary>
    /// Resolve a bitmap from the NeedDef's defName.
    /// </summary>
    private static DynamicNeedsBitmap ResolveBitmapFromDefName(string defName)
    {
        // Validate the defName and ensure it ends with "Need"
        if (string.IsNullOrEmpty(defName))
        {
            MindMattersUtilities.GripeOnce("[MindMatters] ResolveBitmapFromDefName received a null or empty defName.");
            return DynamicNeedsBitmap.None; // Default fallback
        }

        if (!defName.EndsWith("Need"))
        {
            MindMattersUtilities.GripeOnce($"[MindMatters] Unexpected defName format for bitmap resolution: {defName}. Expected names ending with 'Need'.");
            return DynamicNeedsBitmap.None; // Default fallback
        }

        // Extract the bitmap name by removing the "Need" suffix
        string bitmapName = defName.Substring(0, defName.Length - 4);

        // Try to parse the name into the DynamicNeedsBitmap enum
        if (Enum.TryParse(bitmapName, out DynamicNeedsBitmap bitmap))
        {
            return bitmap;
        }

        // Log the issue and return a fallback value
        MindMattersUtilities.GripeOnce($"[MindMatters] No matching bitmap found for defName: {defName}. Check for typos or missing enum values.");
        return DynamicNeedsBitmap.None; // Default fallback
    }

    /// <summary>
    /// Debug logging for registered needs.
    /// </summary>
    public static void DebugLogRegisteredDynamicNeeds()
    {
        if (debugLogCalled)
            return; // Already logged

        debugLogCalled = true;

        MindMattersUtilities.DebugLog("[DynamicNeedsRegistry] Registered Dynamic Needs:");

        foreach (KeyValuePair<Type, DynamicNeedProperties> kvp in Registry)
        {
            DynamicNeedProperties props = kvp.Value;
            MindMattersUtilities.DebugLog($"  - {props.DefName} ({props.NeedClass.Name}): {props.Label}");
        }
    }
    
     // --------------------------------------------------------------------
    // "Vanilla-like" checks for trait/gene disabling
    // --------------------------------------------------------------------
    public static bool VanillaDisablingChecks(Pawn pawn, NeedDef needDef)
    {
        if (pawn == null || !pawn.RaceProps.Humanlike)
            return false;

        // If this needDef isn't in our registry, skip
        if (!Registry.ContainsKey(needDef.needClass))
            return false;

        // If any trait forcibly disables this need
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

        // If any precept forcibly nullifies it
        if (needDef.nullifyingPrecepts != null && pawn.Ideo != null)
        {
            foreach (PreceptDef precept in needDef.nullifyingPrecepts)
            {
                if (pawn.Ideo.HasPrecept(precept))
                    return false;
            }
        }

        // If any Biotech gene forcibly disables it
        if (ModsConfig.BiotechActive)
        {
            var genes = pawn.genes?.GenesListForReading;
            if (genes != null)
            {
                foreach (Gene gene in genes)
                {
                    if (gene.Active && gene.def.disablesNeeds?.Contains(needDef) == true)
                        return false;
                }
            }
        }

        // Lastly, check the dynamic-need’s custom PawnFilter
        var needProps = GetProperties(needDef.needClass);
        return needProps?.PawnFilter?.Invoke(pawn) ?? true;
    }
    
    /// <summary>
    /// Return the NeedDef (from DefDatabase) associated with a dynamic-need type.
    /// Example usage: if you want to do 'pawn.needs.TryGetNeed(...)'.
    /// </summary>
    public static NeedDef GetNeedDef(Type needType)
    {
        if (Registry.TryGetValue(needType, out var _))
        {
            // We default to naming the defName after the Type name
            // (e.g. "FreshFruitNeed"), as set in RegisterNeed<TNeed>
            string defName = needType.Name;
            return DefDatabase<NeedDef>.GetNamedSilentFail(defName);
        }
        return null;
    }
    
    /// <summary>
    /// Returns all dynamic-need properties in a certain category.
    /// </summary>
    public static IEnumerable<DynamicNeedProperties> GetNeedsForCategory(DynamicNeedCategory? category = null)
    {
        return Registry.Values
            .Where(props => !category.HasValue || props.Category == category.Value);
    }

    
    /// <summary>
    /// Convert dynamic-need type => the associated bit in your enum.
    /// Typically used by your manager to track which needs a pawn has in pawnNeedsMap.
    /// </summary>
    public static DynamicNeedsBitmap GetBitmapForNeed(Type needClass)
    {
        return BitmapMap.TryGetValue(needClass, out var bitmap)
            ? bitmap
            : DynamicNeedsBitmap.None;
    }
    
 
    // Not sure about this method!
    /// <summary>
    /// Overload that takes a Type. 
    /// If the type is found in the registry, we get the NeedDef 
    /// and run VanillaDisablingChecks(...) on it.
    /// 
    /// This was your old "ShouldPawnHaveNeed(pawn, needType)" 
    /// that you used in the aggregator approach.
    /// </summary>
    public static bool ShouldPawnHaveNeed(Pawn pawn, Type needType)
    {
        if (!Registry.TryGetValue(needType, out _))
            return false;

        var needDef = GetNeedDef(needType);
        if (needDef == null)
            return false;

        // Now do the "vanilla-like" checks
        return VanillaDisablingChecks(pawn, needDef);
    }
    
}

/// <summary>
/// Properties describing a dynamic need.
/// </summary>
public class DynamicNeedProperties
{
    public Type NeedClass { get; set; }
    public string DefName { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public DynamicNeedCategory Category { get; set; }
    public DynamicNeedsBitmap Bitmap { get; set; }
    public Func<Pawn, bool> PawnFilter { get; set; }
}