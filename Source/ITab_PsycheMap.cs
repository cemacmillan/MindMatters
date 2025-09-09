using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace MindMatters
{
    public class ITab_PsycheMap : ITab
    {
        public ITab_PsycheMap()
        {
            labelKey = "PsycheMap";
            size = new Vector2(1120f, 840f); // Increased by ~2.8x (400*2.8, 300*2.8)
        }
        
        public override bool IsVisible => true;
        
        protected override void FillTab()
        {
            var pawn = SelPawn;
            if (pawn == null) 
            {
                MMToolkit.DebugLog("[ITab_PsycheMap] No pawn selected");
                return;
            }
            
            MMToolkit.DebugLog($"[ITab_PsycheMap] Drawing for {pawn.LabelShort}, size: {size}");
            
            // Create the main rect and contract it for proper ITab frame
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Widgets.BeginGroup(rect);
            
            float curY = 0f;
            
            // Title
            var titleRect = new Rect(0f, curY, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, $"Psyche Map: {pawn.LabelShort}");
            Text.Font = GameFont.Small;
            curY += 30f;
            
            // Subtitle (smaller font, less prominent)
            var subtitleRect = new Rect(0f, curY, rect.width, 16f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(subtitleRect, "Has This Ever Happened To You?");
            Text.Font = GameFont.Small;
            curY += 20f;
            
            // Main content area
            var contentRect = new Rect(0f, curY, rect.width, rect.height - curY);
            DrawPsycheMap(contentRect, pawn);
            
            Widgets.EndGroup();
        }
        
        private void DrawPsycheMap(Rect rect, Pawn pawn)
        {
            // Get the psyche system
            var psycheSystem = Current.Game?.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null)
            {
                Widgets.Label(rect, "Psyche system not available");
                return;
            }
            
            var pawnPsyche = psycheSystem.GetOrCreatePsyche(pawn);
            if (pawnPsyche == null)
            {
                Widgets.Label(rect, "No psyche data available for this pawn");
                return;
            }
            
            float curY = 0f;
            
            // Display basic psyche information
            var infoRect = new Rect(0f, curY, rect.width, 60f);
            DrawPsycheInfo(infoRect, pawnPsyche);
            curY += 65f;
            
            // Center point for the pawn (adjusted for info area)
            var center = new Vector2(rect.width / 2f, (rect.height - curY) / 2f + curY);
            var pawnRadius = 15f;
            
            // Draw the pawn at center (blue circle with 'P')
            DrawPawnCenter(center, pawnRadius, pawn);
            
            // Get recent experiences, impressions, and thoughts
            var experiences = GetRecentExperiences(pawn);
            var impressions = GetActiveImpressions(pawn);
            var thoughts = GetCurrentThoughts(pawn);
            
            // Draw experiences as shapes around the pawn
            DrawExperiences(rect, center, experiences);
            
            // Draw impressions as colored shapes
            DrawImpressions(rect, center, impressions);
            
            // Draw thoughts as colored shapes
            DrawThoughts(rect, center, thoughts);
            
            // Draw legend
            DrawLegend(rect);
        }
        
        private void DrawPsycheInfo(Rect rect, PawnPsyche psyche)
        {
            // Background for info area
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            
            float curY = rect.y + 5f;
            float lineHeight = 15f;
            
            // Get chroma information
            var chromaInfo = psyche.GetChromaInfo();
            var activeImpressions = psyche.GetActiveImpressions();
            
            // Dominant psychological mode with chroma intensity
            var modeRect = new Rect(rect.x + 5f, curY, rect.width - 10f, lineHeight);
            Widgets.Label(modeRect, $"Chroma: {chromaInfo.DominantMode} ({chromaInfo.DominantWeight:P0})");
            curY += lineHeight;
            
            // Active impressions count
            var impressionCountRect = new Rect(rect.x + 5f, curY, rect.width - 10f, lineHeight);
            Widgets.Label(impressionCountRect, $"Active Impressions: {chromaInfo.ActiveImpressions}");
            curY += lineHeight;
            
            // Recent experiences count
            var experienceRect = new Rect(rect.x + 5f, curY, rect.width - 10f, lineHeight);
            Widgets.Label(experienceRect, $"Recent Experiences: {chromaInfo.RecentExperiences}");
            curY += lineHeight;
            
            // Mode weights (top 3) with chroma visualization
            var weights = chromaInfo.ModeWeights;
            var topModes = weights.Select((weight, index) => new { Index = index, Weight = weight })
                                 .OrderByDescending(x => x.Weight)
                                 .Take(3);
            
            foreach (var mode in topModes)
            {
                var modeName = ((PsychologicalMode)mode.Index).ToString();
                var weightRect = new Rect(rect.x + 5f, curY, rect.width - 10f, lineHeight);
                
                // Color the text based on mode intensity
                var intensity = mode.Weight;
                if (intensity > 0.4f)
                    GUI.color = Color.green;
                else if (intensity > 0.2f)
                    GUI.color = Color.yellow;
                else
                    GUI.color = Color.gray;
                
                Widgets.Label(weightRect, $"  {modeName}: {mode.Weight:F2}");
                GUI.color = Color.white;
                curY += lineHeight;
            }
            
            // Show active impressions if any
            if (activeImpressions.Count > 0)
            {
                curY += 5f; // Spacing
                var impressionHeaderRect = new Rect(rect.x + 5f, curY, rect.width - 10f, lineHeight);
                Widgets.Label(impressionHeaderRect, "Active Impressions:");
                curY += lineHeight;
                
                foreach (var impression in activeImpressions.Take(3)) // Show top 3
                {
                    var impressionRect = new Rect(rect.x + 10f, curY, rect.width - 15f, lineHeight);
                    var decayPercent = impression.DecayFactor * 100f;
                    Widgets.Label(impressionRect, $"  {impression.ModeName}: {impression.Magnitude:F2} ({decayPercent:F0}%)");
                    curY += lineHeight;
                }
            }
        }
        
        private void DrawPawnCenter(Vector2 center, float radius, Pawn pawn)
        {
            // Blue circle for the pawn (using a square approximation)
            var rect = new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2);
            GUI.color = Color.blue;
            Widgets.DrawBoxSolid(rect, Color.blue);
            GUI.color = Color.white;
            
            // 'P' label
            var labelRect = new Rect(center.x - 8f, center.y - 8f, 16f, 16f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, "P");
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private List<PsycheExperience> GetRecentExperiences(Pawn pawn)
        {
            var psycheSystem = Current.Game?.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null) return new List<PsycheExperience>();
            
            var pawnPsyche = psycheSystem.GetOrCreatePsyche(pawn);
            if (pawnPsyche == null) return new List<PsycheExperience>();
            
            // For now, return empty list - we'll need to add a method to get recent experiences
            // This would come from the Psyche system's experience tracking
            return new List<PsycheExperience>();
        }
        
        private List<Thought> GetCurrentThoughts(Pawn pawn)
        {
            if (pawn?.needs?.mood?.thoughts == null) return new List<Thought>();
            
            var thoughts = new List<Thought>();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
            return thoughts.Where(t => t != null && t.def != null).ToList();
        }
        
        private List<ImpressionInfo> GetActiveImpressions(Pawn pawn)
        {
            var psycheSystem = Current.Game?.GetComponent<MindMattersPsyche>();
            if (psycheSystem == null) return new List<ImpressionInfo>();
            
            var pawnPsyche = psycheSystem.GetOrCreatePsyche(pawn);
            if (pawnPsyche == null) return new List<ImpressionInfo>();
            
            return pawnPsyche.GetActiveImpressions();
        }
        
        private void DrawExperiences(Rect rect, Vector2 center, List<PsycheExperience> experiences)
        {
            // Draw experiences as shapes around the pawn
            var angleStep = 360f / Math.Max(experiences.Count, 1);
            var distance = 80f;
            
            for (int i = 0; i < experiences.Count; i++)
            {
                var experience = experiences[i];
                var angle = i * angleStep * Mathf.Deg2Rad;
                var pos = center + new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                DrawExperienceShape(pos, experience);
            }
        }
        
        private void DrawImpressions(Rect rect, Vector2 center, List<ImpressionInfo> impressions)
        {
            if (impressions.Count == 0) return;
            
            // Draw impressions as colored shapes around the pawn
            var angleStep = 360f / Math.Max(impressions.Count, 1);
            var distance = 140f; // Further out than experiences
            
            for (int i = 0; i < impressions.Count; i++)
            {
                var impression = impressions[i];
                var angle = i * angleStep * Mathf.Deg2Rad;
                var pos = center + new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                DrawImpressionShape(pos, impression);
            }
        }
        
        private void DrawThoughts(Rect rect, Vector2 center, List<Thought> thoughts)
        {
            if (thoughts == null || thoughts.Count == 0) return;
            
            // Filter thoughts with significant mood impact
            var significantThoughts = thoughts.Where(t => t != null && t.def != null && Mathf.Abs(t.MoodOffset()) >= 5f).ToList();
            if (significantThoughts.Count == 0) return;
            
            // Draw thoughts as colored shapes
            var angleStep = 360f / significantThoughts.Count;
            var distance = 120f;
            
            for (int i = 0; i < significantThoughts.Count; i++)
            {
                var thought = significantThoughts[i];
                var angle = i * angleStep * Mathf.Deg2Rad;
                var pos = center + new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                DrawThoughtShape(pos, thought);
            }
        }
        
        private void DrawExperienceShape(Vector2 pos, PsycheExperience experience)
        {
            var size = 15f;
            var rect = new Rect(pos.x - size/2f, pos.y - size/2f, size, size);
            
            // Color based on valency
            switch (experience.Valency)
            {
                case ExperienceValency.Positive:
                    GUI.color = Color.green;
                    break;
                case ExperienceValency.Negative:
                    GUI.color = Color.red;
                    break;
                case ExperienceValency.Eldritch:
                    GUI.color = Color.magenta;
                    break;
                case ExperienceValency.Humiliating:
                    GUI.color = Color.yellow;
                    break;
                default:
                    GUI.color = Color.gray;
                    break;
            }
            
            // Draw as diamond for experiences
            DrawDiamond(rect);
            GUI.color = Color.white;
        }
        
        private void DrawThoughtShape(Vector2 pos, Thought thought)
        {
            // Size based on mood impact magnitude
            var moodOffset = thought.MoodOffset();
            var size = Mathf.Clamp(Mathf.Abs(moodOffset) / 10f, 4f, 12f);
            
            // Color based on mood impact
            if (moodOffset > 0)
                GUI.color = Color.green;
            else if (moodOffset < 0)
                GUI.color = Color.red;
            else
                GUI.color = Color.gray;
            
            // Draw as circle for thoughts (using square approximation)
            var thoughtRect = new Rect(pos.x - size/2f, pos.y - size/2f, size, size);
            Widgets.DrawBoxSolid(thoughtRect, GUI.color);
            GUI.color = Color.white;
        }
        
        private void DrawImpressionShape(Vector2 pos, ImpressionInfo impression)
        {
            // Size based on magnitude and decay factor
            var size = Mathf.Clamp(impression.Magnitude * 20f * impression.DecayFactor, 3f, 10f);
            
            // Color based on mode type
            Color impressionColor = impression.ModeName switch
            {
                "Calm" => Color.cyan,
                "Vigilant" => new Color(1f, 0.5f, 0f), // Orange
                "Affiliative" => Color.magenta,
                "Acquisitive" => Color.yellow,
                "Despair" => Color.red,
                "Defiant" => new Color(0.8f, 0.2f, 0.8f), // Purple
                "Analyst" => Color.blue,
                "Oracle" => new Color(0.5f, 0.5f, 1f), // Light blue
                _ => Color.gray
            };
            
            // Alpha based on decay factor
            impressionColor.a = impression.DecayFactor;
            
            // Draw as triangle for impressions
            var impressionRect = new Rect(pos.x - size/2f, pos.y - size/2f, size, size);
            Widgets.DrawBoxSolid(impressionRect, impressionColor);
        }
        
        private void DrawDiamond(Rect rect)
        {
            // Draw a diamond shape
            var points = new Vector2[]
            {
                new Vector2(rect.center.x, rect.yMin),           // Top
                new Vector2(rect.xMax, rect.center.y),           // Right
                new Vector2(rect.center.x, rect.yMax),           // Bottom
                new Vector2(rect.xMin, rect.center.y)            // Left
            };
            
            // Simple diamond using lines
            for (int i = 0; i < points.Length; i++)
            {
                var next = (i + 1) % points.Length;
                Widgets.DrawLine(points[i], points[next], Color.white, 2f);
            }
        }
        
        private void DrawLegend(Rect rect)
        {
            var legendRect = new Rect(rect.width - 180f, rect.height - 140f, 170f, 130f);
            Widgets.DrawBox(legendRect);
            
            var y = legendRect.y + 5f;
            var x = legendRect.x + 5f;
            
            Text.Font = GameFont.Tiny;
            
            // Legend entries
            Widgets.Label(new Rect(x, y, 160f, 15f), "Legend:");
            y += 15f;
            
            // Pawn
            GUI.color = Color.blue;
            var pawnRect = new Rect(x + 2f, y + 2f, 12f, 12f);
            Widgets.DrawBoxSolid(pawnRect, Color.blue);
            GUI.color = Color.white;
            Widgets.Label(new Rect(x + 20f, y, 140f, 15f), "Pawn");
            y += 15f;
            
            // Impressions (triangles)
            GUI.color = Color.cyan;
            var impressionRect = new Rect(x + 2f, y + 2f, 12f, 12f);
            Widgets.DrawBoxSolid(impressionRect, Color.cyan);
            GUI.color = Color.white;
            Widgets.Label(new Rect(x + 20f, y, 140f, 15f), "Impressions");
            y += 15f;
            
            // Positive experience
            GUI.color = Color.green;
            DrawDiamond(new Rect(x, y, 12f, 12f));
            GUI.color = Color.white;
            Widgets.Label(new Rect(x + 20f, y, 140f, 15f), "Positive Experience");
            y += 15f;
            
            // Negative experience
            GUI.color = Color.red;
            DrawDiamond(new Rect(x, y, 12f, 12f));
            GUI.color = Color.white;
            Widgets.Label(new Rect(x + 20f, y, 140f, 15f), "Negative Experience");
            y += 15f;
            
            // Positive thought
            GUI.color = Color.green;
            var thoughtRect = new Rect(x + 2f, y + 2f, 8f, 8f);
            Widgets.DrawBoxSolid(thoughtRect, Color.green);
            GUI.color = Color.white;
            Widgets.Label(new Rect(x + 20f, y, 140f, 15f), "Positive Thought");
            y += 15f;
            
            // Negative thought
            GUI.color = Color.red;
            var negThoughtRect = new Rect(x + 2f, y + 2f, 8f, 8f);
            Widgets.DrawBoxSolid(negThoughtRect, Color.red);
            GUI.color = Color.white;
            Widgets.Label(new Rect(x + 20f, y, 140f, 15f), "Negative Thought");
            
            Text.Font = GameFont.Small;
        }
    }
}
