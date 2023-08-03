using HarmonyLib;
using UnityEngine;
using Verse;

namespace MindMatters
{
    public class MindMattersMod : Mod
    {
        public static MindMattersSettings settings;

        public MindMattersMod(ModContentPack content) : base(content)
        {
            // Initialize settings
            settings = GetSettings<MindMattersSettings>();

            Log.Message("Mind Matters v0.0.1");

            // Patch with Harmony
            new Harmony("mod.cem.mindmatters").PatchAll();
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


