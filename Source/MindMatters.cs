using HarmonyLib;
using UnityEngine;
using Verse;
using System.Linq;

namespace MindMatters;

public class MindMattersMod : Mod
{
    public static MindMattersSettings settings;
    public static bool IsVTEActive;
    public static bool IsPositiveConnectionsActive;

    public MindMattersMod(ModContentPack content) : base(content)
    {
        // Initialize settings
        settings = GetSettings<MindMattersSettings>();
        IsVTEActive = ModsConfig.IsActive("VanillaExpanded.VanillaTraitsExpanded");
        IsPositiveConnectionsActive = ModsConfig.IsActive("cem.PositiveConnections");

        // Initialize MindMattersGameComponent
        Current.Game?.components.Add(new MindMattersGameComponent(Current.Game));

        // Initialize MindMattersExperienceComponent
        if (Current.Game != null && !Current.Game.components.Any(x => x is MindMattersExperienceComponent))
        {
            Current.Game.components.Add(new MindMattersExperienceComponent(Current.Game));
        }

        if (IsPositiveConnectionsActive)
        {
            MindMattersBridge.Initialize();
        }

        MindMattersUtilities.DebugLog("<color=#00FF7F>[Mind Matters]</color> v1.5.8 fifth-horseperson");

        // Patch with Harmony
        var harmony = new Harmony("mod.cem.mindmatters");
        // Harmony.DEBUG = true;
        harmony.PatchAll();
    }

    // Override Mod Settings
    public override string SettingsCategory() => "Mind Matters";

    public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
    {
        settings.DoWindowContents(inRect);
    }
}

public class MindMattersSettings : ModSettings
{
    // ReSharper disable once MemberCanBePrivate.Global
    public float someSetting = 1.0f;
    public bool enableLogging = false; // Add EnableLogging toggle

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref someSetting, "someSetting", 1.0f);
        Scribe_Values.Look(ref enableLogging, "EnableLogging", false); // Save/Load EnableLogging
    }

    public void DoWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);

        // Existing slider setting
        listingStandard.Label("Some setting: " + someSetting.ToString("0.0"));
        someSetting = listingStandard.Slider(someSetting, 0f, 2f);

        // Add EnableLogging checkbox
        listingStandard.CheckboxLabeled("Enable detailed logging", ref enableLogging, "Toggle detailed logging output for debugging purposes.");

        listingStandard.End();
        LoadedModManager.GetMod<MindMattersMod>().WriteSettings();
    }
}