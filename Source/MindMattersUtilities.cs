using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using System.Linq;
using UnityEngine;
using static MindMatters.MindMattersGameComponent;
using static MindMatters.MindMattersExperienceComponent;

namespace MindMatters
{
    public static class MindMattersUtilities
    {
        public static readonly int[] StageWeights = { 1, 2, 5, 2, 1 };

        public const float AloneDistanceSquared = 9f * 9f;  // Adjust this to match the "alone" radius


        public static class RoomRoleUtility
        {
            public static RoomRoleDef GetRoomRole(Room room)
            {
                if (room == null)
                {
                    return RoomRoleDefOf.None;
                }

                RoomRoleDef highestRole = RoomRoleDefOf.None;
                float highestScore = 0f;

                foreach (RoomRoleDef roleDef in DefDatabase<RoomRoleDef>.AllDefs)
                {
                    float score = roleDef.Worker.GetScore(room);
                    if (score > highestScore)
                    {
                        highestScore = score;
                        highestRole = roleDef;
                    }
                }

                return highestRole;
            }
        }

        public static void AddExperience(Pawn pawn, string eventType, ExperienceValency valency)
        {
            // Get the MindMattersExperienceComponent
            MindMattersExperienceComponent gameComponent =
                Current.Game.GetComponent<MindMattersExperienceComponent>();

            // If the component exists, add the experience
            if (gameComponent != null)
            {
                // Create a new experience
                Experience newExperience = new Experience(eventType, valency);

                // If the pawn doesn't have an entry in the dictionary yet, create it
                if (!gameComponent.pawnExperiences.ContainsKey(pawn))
                {
                    gameComponent.pawnExperiences[pawn] = new List<Experience>();
                }

                // Add the experience to the pawn's list of experiences
                gameComponent.pawnExperiences[pawn].Add(newExperience);
            }
        }

        public static bool IsPawnAlone(Pawn pawn, List<Pawn> allPawns)
        {
            // Check if our game instance is null
            if (Current.Game == null)
            {
                Log.Error("Current.Game is null.");
                return false;
            }

            MindMattersGameComponent gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();

            // Check if our game component is null
            if (gameComponent == null)
            {
                Log.Error("MindMattersGameComponent is null.");
                return false;
            }

            // Check if our dictionary is null
            if (gameComponent.PawnLastAloneTicks == null)
            {
                Log.Error("PawnLastAloneTicks dictionary is null.");
                return false;
            }

            // Check if our pawn is null
            if (pawn == null)
            {
                Log.Error("Passed pawn is null.");
                return false;
            }

            // Check if our allPawns list is null
            if (allPawns == null)
            {
                //Log.Error("allPawns list is null.");
                return false;
            }

            if (!gameComponent.PawnLastAloneTicks.ContainsKey(pawn.thingIDNumber) ||
                Find.TickManager.TicksGame - gameComponent.PawnLastAloneTicks[pawn.thingIDNumber] > 60)
            {
                Region pawnRegion = pawn.GetRegion(RegionType.Set_Passable);

                // Check if our pawn's region is null
                if (pawnRegion == null)
                {
                    Log.Error($"Region for pawn {pawn.Name} is null.");
                    return false;
                }

                foreach (Pawn otherPawn in allPawns)
                {
                    if (otherPawn == null)
                    {
                        Log.Warning("One of the pawns in allPawns list is null. Skipping this pawn.");
                        continue;
                    }

                    if (otherPawn == pawn || otherPawn.Map != pawn.Map || !otherPawn.RaceProps.Humanlike)
                        continue;

                    if (otherPawn.GetRegion(RegionType.Set_Passable) == pawnRegion &&
                        otherPawn.Position.DistanceToSquared(pawn.Position) <= AloneDistanceSquared)
                    {
                        return false;
                    }
                }

                gameComponent.PawnLastAloneTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
            }

            return true;
        }




        public static bool IsPawnInSafeSituation(Pawn p)
        {
            int inSafeSituationThreshhold = 2500; // ticks 
            if (p.Map == null)
            {
                return false;
            }

            Area_Home homeArea = p.Map.areaManager.Home;

            if (homeArea != null && !homeArea.ActiveCells.Contains(p.Position))
            {
                return false;
            }

            int currentTick = Find.TickManager.TicksGame;

            if (currentTick - p.mindState.lastMeleeThreatHarmTick < inSafeSituationThreshhold ||
                currentTick - p.mindState.lastEngageTargetTick < inSafeSituationThreshhold ||
                currentTick - p.mindState.lastAttackTargetTick < inSafeSituationThreshhold)
            {
                return false;
            }

            return true;
        }

        public static void UpdateBipolarPawnTicks(Pawn pawn, Dictionary<int, int> BipolarPawnLastCheckedTicks)
        {
            Hediff bipolar = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Bipolar"));

            if (bipolar != null)
            {
                if (!BipolarPawnLastCheckedTicks.TryGetValue(pawn.thingIDNumber, out int lastCheckedTick))
                {
                    lastCheckedTick = Find.TickManager.TicksGame;
                    BipolarPawnLastCheckedTicks[pawn.thingIDNumber] = lastCheckedTick;
                }

                if (Find.TickManager.TicksGame >= lastCheckedTick + GenDate.TicksPerDay)
                {
                    UpdateBipolarPawn(pawn, bipolar, BipolarPawnLastCheckedTicks);
                }
            }
            else
            {
                if (BipolarPawnLastCheckedTicks.ContainsKey(pawn.thingIDNumber))
                {
                    BipolarPawnLastCheckedTicks.Remove(pawn.thingIDNumber);
                }
            }
        }

        public static void TryGiveRandomInspiration(Pawn pawn)
        {
            if (Rand.Chance(0.5f))
            {
                var validInspirations = DefDatabase<InspirationDef>.AllDefsListForReading
                    .Where(inspiration => pawn.InspirationDef == null)
                    .ToList();

                if (validInspirations.Any())
                {
                    var randomInspiration = validInspirations[Rand.Range(0, validInspirations.Count)];
                    pawn.mindState.inspirationHandler.TryStartInspiration(randomInspiration);
                }
            }
        }

        public static void UpdatePawnMoods(Pawn pawn, Dictionary<Pawn, Mood> pawnMoods, Action<Pawn> onPawnMoodChanged)
        {
            Mood newMood = GetMoodForPawn(pawn);

            if (pawnMoods.TryGetValue(pawn, out var currentMood))
            {
                if (currentMood != newMood)
                {
                    pawnMoods[pawn] = newMood;
                    onPawnMoodChanged?.Invoke(pawn);
                }
            }
            else
            {
                pawnMoods[pawn] = newMood;
            }
        }

        public static void UpdateBipolarPawn(Pawn pawn, Hediff bipolar, Dictionary<int, int> BipolarPawnLastCheckedTicks)
        {
            int newStage = WeightedRandomStageWithInertia(pawn, bipolar.Severity);
            bipolar.Severity = newStage / (float)(bipolar.def.stages.Count - 1);
            BipolarPawnLastCheckedTicks[pawn.thingIDNumber] = Find.TickManager.TicksGame;
        }

        public static Mood GetMoodForPawn(Pawn pawn)
        {
            float pawnMood = pawn.needs?.mood?.CurLevel ?? 0f;
            if (pawnMood > 0.6f) return Mood.Happy;
            if (pawnMood < 0.4f) return Mood.Unhappy;
            return Mood.Neutral;
        }

        public static int WeightedRandomStageWithInertia(Pawn p, float lastSeverity)
        {
            lastSeverity = Mathf.Clamp01(lastSeverity);
            int currentStage = Mathf.RoundToInt(lastSeverity * (StageWeights.Length - 1));

            List<int> possibleStages = Enumerable.Range(0, StageWeights.Length).ToList();

            possibleStages.RemoveAll(stage => Math.Abs(stage - currentStage) > 1);

            possibleStages = possibleStages.Where(stage => stage >= 0 && stage < StageWeights.Length).ToList();

            if (!possibleStages.Any())
            {
                Log.Warning("No valid stages for bipolar thought.");
                return currentStage;
            }

            int totalWeight = possibleStages.Sum(stage => StageWeights[stage]);

            int randomNumber = Rand.RangeInclusive(1, totalWeight);

            int cumulativeWeight = 0;
            foreach (int stage in possibleStages)
            {
                cumulativeWeight += StageWeights[stage];
                if (randomNumber <= cumulativeWeight)
                    return stage;
            }

            return currentStage;
        }
    }
}