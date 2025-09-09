using HarmonyLib;
using UnityEngine;
using Verse;
using System.Linq;

namespace MindMatters;

public class MindMattersMod : Mod
{
    public static MindMattersSettings settings;
    public static bool IsVteActive { get; set; }
    public static bool IsPositiveConnectionsActive;
    
    // overall readiness to function, API readiness

    public static bool IsSystemReady { get; set; }
    public static bool ReadyToParley { get; set; }

    public MindMattersMod(ModContentPack content) : base(content)
    {
        // Initialize settings
        settings = GetSettings<MindMattersSettings>();
        
        IsVteActive = ModsConfig.IsActive("VanillaExpanded.VanillaTraitsExpanded");
        IsPositiveConnectionsActive = ModsConfig.IsActive("cem.PositiveConnections");
        IsSystemReady = false;
        ReadyToParley = false;

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

        Log.Message("<color=#00FF7F>[Mind Matters Pre-Release]</color> v1.6.0.0 sixth-sense awakening");

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
    public bool enableAPI;
    public bool NeedsApplyToGuests;
    public bool DoNotAddNeeds;

    public override void ExposeData()
    {
        base.ExposeData();
        
        Scribe_Values.Look(ref someSetting, "DummySlider", 1.0f);
        Scribe_Values.Look(ref enableAPI, "EnableAPI", true);
        Scribe_Values.Look(ref enableLogging, "EnableLogging", false);
        Scribe_Values.Look(ref NeedsApplyToGuests, "NeedsApplyToGuests", false);
        Scribe_Values.Look(ref DoNotAddNeeds, "DoNotAddNeeds", true);
    }

    public void DoWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);

        // Existing slider setting
        listingStandard.Label("Slider if you get bored: " + someSetting.ToString("0.0"));
        someSetting = listingStandard.Slider(someSetting, 0f, 2f);

        // Add EnableLogging checkbox
        listingStandard.CheckboxLabeled("Enable detailed logging", ref enableLogging, "Toggle detailed logging output for debugging purposes.");
        listingStandard.CheckboxLabeled("Enable API for other mods", ref enableAPI, "Toggle interaction with other mods via the API.");
        listingStandard.CheckboxLabeled("Guests have transient Needs like other pawns", ref NeedsApplyToGuests, "Toggle interaction with other mods via the API.");
        listingStandard.CheckboxLabeled("Do not touch Needs directly: Trust The Game", ref DoNotAddNeeds, "Toggle interaction with other mods via the API.");

        listingStandard.End();
        LoadedModManager.GetMod<MindMattersMod>().WriteSettings();
    }
}