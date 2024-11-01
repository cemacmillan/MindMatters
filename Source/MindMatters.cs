using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking.Match;
using Verse;
using System;

namespace MindMatters
{
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

            Log.Message("Mind Matters v1.5.2");

            // Patch with Harmony
            var harmony = new Harmony("mod.cem.mindmatters");
           //Harmony.DEBUG = true;
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
        public float someSetting = 1.0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref someSetting, "someSetting", 1.0f);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Some setting: " + someSetting.ToString("0.0"));
            someSetting = listingStandard.Slider(someSetting, 0f, 2f);
            listingStandard.End();
            LoadedModManager.GetMod<MindMattersMod>().WriteSettings();
        }
    }
}


