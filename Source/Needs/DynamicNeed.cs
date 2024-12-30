using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace MindMatters
{
    public enum DynamicNeedCategory
    {
        Primal,
        Secondary,
        Quirk,
        Phobia,
        Luxury,
        Animal,
        Machine,
        Eldritch
    }

    public abstract class DynamicNeed : Need, IDynamicNeed
    {
        public virtual DynamicNeedCategory Category => DynamicNeedCategory.Secondary;

        private float value;
        private int updateCounter;
        private const int UpdateFrequency = 4;

        public NeedDef def;

        // This property remains as before:
        public NeedDef NeedDef
        {
            get => def;
            private set
            {
                if (value == null)
                {
                    MindMattersUtilities.GripeOnce("[MindMatters] Attempted to assign a null NeedDef. Skipping assignment.");
                    return; // Soft handling; do not crash
                }
                def = value;
            }
        }

        // Pawn reference remains as before
        public Pawn Pawn { get; }

        protected DynamicNeed() : base()
        {
            // InitializeDefaults();
        }

        protected DynamicNeed(Pawn pawn) : base(pawn)
        {
            def = NeedDef;
            InitializeDefaults();
        }

        protected DynamicNeed(Pawn pawn, NeedDef needDef) : base(pawn)
        {
            def = needDef;
           // InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            this.threshPercents = new List<float> { 0.25f, 0.5f, 0.75f };
            // If def is null, we let the subclass provide something in DefaultNeedDef().
            this.def ??= DefaultNeedDef();
        }

        // Subclasses typically override this:
        protected virtual NeedDef DefaultNeedDef()
        {
            return null; // base does nothing
        }

        // Overridable init method (as before):
        public virtual void Initialize(Pawn pawn, NeedDef def)
        {
            this.pawn = pawn;
            this.def = def;
        }

        // The new virtual method:
        //   Called by Harmony patch or external code to decide if a Pawn "qualifies" for this dynamic need.
        public virtual bool ShouldPawnHaveThisNeed(Pawn pawn)
        {
            // Default is just "true" so that if a subclass doesn't override, it won't block anything.
            // Logging optional—leaving it minimal here.
            // MindMattersUtilities.DebugLog("[MindMatters] DynamicNeed: default ShouldPawnHaveThisNeed => true for pawn " + pawn);
            return true;
        }
        
        public override float CurLevel
        {
            get => value;
            set => this.value = Mathf.Clamp01(value);
        }
        public override bool ShowOnNeedList => true;

        public override void NeedInterval()
        {
            updateCounter++;
            if (updateCounter >= UpdateFrequency)
            {
                updateCounter = 0;
                UpdateValue();
            }
        }

        protected abstract void UpdateValue();

        public override string GetTipString()
        {
            return $"{def?.label ?? "Undefined Need"}: {(CurLevel * 100f).ToString("F0")}%";
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Use defName as a key for serialization
            string defNameKey = def?.defName ?? "undefined";
            string curLevelKey = $"curLevel_{defNameKey}";
            // float oldValue = curLevel;

            // Serialize the current level with a unique key
            Scribe_Values.Look(ref value, curLevelKey, 0.5f);

            /*if (MindMattersMod.settings.EnableDebugLogging)
            {
                string pawnName = pawn?.LabelShort ?? "Unknown Pawn";
                Log.Message($"[Mind Matters] ExposeData for {pawnName} (ID: {pawn?.thingIDNumber}). Old Value: {oldValue}, New Value: {curLevel}, Key: {curLevelKey}");
            }*/
        }
    }
}