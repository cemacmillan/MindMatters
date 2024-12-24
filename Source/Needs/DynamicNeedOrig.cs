/*using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace MindMatters
{
    public enum DynamicNeedCategory
    {
        Primal, // Core, instinctual needs (e.g., Hunger, Thirst)
        Secondary, // Acquired needs from experiences (e.g., addictive pollen)
        Quirk, // Individualized behaviors or preferences (e.g., StableViceNeed)
        Phobia, // Fear-based needs or aversions (e.g., FireAvoidance)
        Luxury, // Optional high-level needs (e.g., fine dining, aesthetics)
        Animal, // Needs exclusive to animals (e.g., RearingDisplay)
        Machine, // Needs exclusive to machines (e.g., MaintenanceCheck)
        Eldritch // Mystical, anomalous, or supernatural needs (e.g., DryadFocus)
    }

    // this is meant to be a reusable class - redesign export or something so pragma can be removed and warning gone
#pragma warning disable CA1012
    public abstract class DynamicNeed : Need, IDynamicNeed
#pragma warning restore CA1012
    {
        public virtual DynamicNeedCategory Category => DynamicNeedCategory.Secondary;

        private float value;
        private int updateCounter;
        private const int UpdateFrequency = 4; // Throttle updates to avoid performance hits

        public Pawn Pawn { get; }

        /// <summary>
        /// Parameterless constructor with this quirky sig is required due to Reflection being used in derived classes.
        /// This is why the warning is shut off. It will not be explained in derived classes
        /// 
        /// </summary>
        public DynamicNeed() : base()
        {
            InitializeDefaults();
        }

        public DynamicNeed(Pawn pawn) : base(pawn)
        {
            InitializeDefaults();
        }

        public DynamicNeed(Pawn pawn, NeedDef needDef) : base(pawn)
        {
            this.def = needDef; // Assign the NeedDef
            InitializeDefaults();
        }

// Initialize shared default values
        private void InitializeDefaults()
        {
            this.threshPercents = new List<float> { 0.25f, 0.5f, 0.75f }; // Default thresholds
            this.def ??= DefaultNeedDef(); // Assign a default NeedDef if none provided
        }

// Helper method for providing a default NeedDef
        protected virtual NeedDef DefaultNeedDef()
        {
            return null; // Override in subclasses or provide a fallback if required
        }


        // Initialize method to set up the pawn and def
        public virtual void Initialize(Pawn pawn, NeedDef def)
        {
            this.pawn = pawn;
            this.def = def;
        }


        public virtual void Initialize(NeedDef def)
        {
            this.def = def;
        }

        public override float CurLevel
        {
            get => value;
            set => this.value = Mathf.Clamp01(value); // Clamp between 0 and 1
        }

        public override bool ShowOnNeedList => true;

        public override void NeedInterval()
        {
            updateCounter++;
            if (updateCounter >= UpdateFrequency)
            {
                updateCounter = 0;
                UpdateValue(); // Delegate to derived class for specific logic
            }
        }

        protected abstract void UpdateValue(); // To be implemented by subclasses

        public override string GetTipString()
        {
            return $"{def.label}: {(CurLevel * 100f).ToString("F0")}%";
        }

        public virtual string GetCategorySpecificTip()
        {
            return string.Empty; // Override in derived classes
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref value, $"curLevel_{def.defName}", 0f);
        }
    }
}*/