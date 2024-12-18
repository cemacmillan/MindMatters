using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace MindMatters
{
    public abstract class DynamicNeed : Need, IDynamicNeed
    {
        private float value;
        private int updateCounter;
        private const int UpdateFrequency = 4; // Throttle updates to avoid performance hits

        public Pawn Pawn { get; }

        public DynamicNeed(Pawn pawn, NeedDef needDef) : base(pawn)
        {
            this.def = needDef; // Assign the NeedDef
            this.threshPercents = new List<float> { 0.25f, 0.5f, 0.75f };
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref value, $"curLevel_{def.defName}", 0f);
        }
    }
}