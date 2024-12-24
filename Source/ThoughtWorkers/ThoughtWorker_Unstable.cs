using System;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MindMatters
{
    public class ThoughtWorker_Unstable : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            try
            {
                if (Current.Game == null)
                {
                    return ThoughtState.Inactive;
                }

               // MindMattersUtilities.DebugLog($"Checking ThoughtState for pawn {p.LabelShort}");

                if (!p.story.traits.HasTrait(MindMattersTraitDef.Unstable))
                {
                   // MindMattersUtilities.DebugLog($"Pawn {p.LabelShort} does not have Unstable trait.");
                    return ThoughtState.Inactive;
                }

                // MindMattersUtilities.DebugLog($"Pawn {p.LabelShort} has Unstable trait.");

                var gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();

                if (gameComponent == null)
                {
                    Log.Error("MindMattersGameComponent is null.");
                    return ThoughtState.Inactive;
                }

                if (gameComponent.UnstablePawnLastMoodSwitchTicks == null)
                {
                   // MindMattersUtilities.DebugLog("UnstablePawnLastMoodSwitchTicks is null, initializing it to an empty dictionary.");
                    gameComponent.UnstablePawnLastMoodSwitchTicks = new Dictionary<int, int>();
                    return ThoughtState.Inactive;
                }

               // MindMattersUtilities.DebugLog($"Checking mood switch ticks for pawn {p.LabelShort}.");

                if (gameComponent.UnstablePawnLastMoodSwitchTicks.TryGetValue(p.thingIDNumber, out var lastMoodSwitchTick))
                {
                   // MindMattersUtilities.DebugLog($"Last mood switch for pawn {p.LabelShort} was at tick {lastMoodSwitchTick}.");

                    // Mood shift every 4 hours
                    if (Find.TickManager.TicksGame - lastMoodSwitchTick > 10000)
                    {
                        //MindMattersUtilities.DebugLog($"More than 4 hours have passed since last mood switch for pawn {p.LabelShort}.");

                        gameComponent.UnstablePawnLastMoodSwitchTicks[p.thingIDNumber] = Find.TickManager.TicksGame;

                        // Random mood shift
                        int randomMoodShift = Rand.RangeInclusive(0, 2);
                        gameComponent.UnstablePawnLastMoodState[p.thingIDNumber] = randomMoodShift;
                       // MindMattersUtilities.DebugLog($"Random mood shift for pawn {p.LabelShort} is {randomMoodShift}.");

                        switch (randomMoodShift)
                        {
                            case 0:
                                //MindMattersUtilities.DebugLog($"Pawn {p.LabelShort} is switching to Neutral mood.");
                                return ThoughtState.ActiveAtStage(0); // Neutral mood
                            case 1:
                                //MindMattersUtilities.DebugLog($"Pawn {p.LabelShort} is switching to Positive mood.");
                                return ThoughtState.ActiveAtStage(1); // Positive mood
                            case 2:
                                //MindMattersUtilities.DebugLog($"Pawn {p.LabelShort} is switching to Negative mood.");
                                return ThoughtState.ActiveAtStage(2); // Negative mood
                            default:
                                //MindMattersUtilities.DebugLog($"Unexpected mood shift value for pawn {p.LabelShort}.");
                                return ThoughtState.Inactive;
                        }
                    }
                   else
                    {
                        //MindMattersUtilities.DebugLog($"Less than 4 hours have passed since last mood switch for pawn {p.LabelShort}.");

                        if (gameComponent.UnstablePawnLastMoodState.TryGetValue(p.thingIDNumber, out var lastMoodState))
                        {
                            return ThoughtState.ActiveAtStage(lastMoodState);
                        }
                        else
                        {
                            //MindMattersUtilities.DebugLog($"No recorded last mood state for pawn {p.LabelShort}.");
                            return ThoughtState.Inactive;
                        }
                    }
                }
                else
                {
                    //MindMattersUtilities.DebugLog($"No recorded last mood switch tick for pawn {p.LabelShort}.");
                }

                //MindMattersUtilities.DebugLog($"Returning Inactive ThoughtState for pawn {p.LabelShort}.");
                return ThoughtState.Inactive;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in ThoughtWorker_Unstable.CurrentStateInternal for pawn {p.LabelShort}: {ex}");
                return ThoughtState.Inactive;
            }
        }
    }
}
