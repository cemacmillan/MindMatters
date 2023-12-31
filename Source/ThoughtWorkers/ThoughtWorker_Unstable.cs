﻿using System;
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

               // Log.Message($"Checking ThoughtState for pawn {p.LabelShort}");

                if (!p.story.traits.HasTrait(MindMattersTraits.Unstable))
                {
                   // Log.Message($"Pawn {p.LabelShort} does not have Unstable trait.");
                    return ThoughtState.Inactive;
                }

                // Log.Message($"Pawn {p.LabelShort} has Unstable trait.");

                var gameComponent = Current.Game.GetComponent<MindMattersGameComponent>();

                if (gameComponent == null)
                {
                    Log.Error("MindMattersGameComponent is null.");
                    return ThoughtState.Inactive;
                }

                if (gameComponent.UnstablePawnLastMoodSwitchTicks == null)
                {
                   // Log.Message("UnstablePawnLastMoodSwitchTicks is null, initializing it to an empty dictionary.");
                    gameComponent.UnstablePawnLastMoodSwitchTicks = new Dictionary<int, int>();
                    return ThoughtState.Inactive;
                }

               // Log.Message($"Checking mood switch ticks for pawn {p.LabelShort}.");

                if (gameComponent.UnstablePawnLastMoodSwitchTicks.TryGetValue(p.thingIDNumber, out var lastMoodSwitchTick))
                {
                   // Log.Message($"Last mood switch for pawn {p.LabelShort} was at tick {lastMoodSwitchTick}.");

                    // Mood shift every 4 hours
                    if (Find.TickManager.TicksGame - lastMoodSwitchTick > 10000)
                    {
                        //Log.Message($"More than 4 hours have passed since last mood switch for pawn {p.LabelShort}.");

                        gameComponent.UnstablePawnLastMoodSwitchTicks[p.thingIDNumber] = Find.TickManager.TicksGame;

                        // Random mood shift
                        int randomMoodShift = Rand.RangeInclusive(0, 2);
                        gameComponent.UnstablePawnLastMoodState[p.thingIDNumber] = randomMoodShift;
                       // Log.Message($"Random mood shift for pawn {p.LabelShort} is {randomMoodShift}.");

                        switch (randomMoodShift)
                        {
                            case 0:
                                //Log.Message($"Pawn {p.LabelShort} is switching to Neutral mood.");
                                return ThoughtState.ActiveAtStage(0); // Neutral mood
                            case 1:
                                //Log.Message($"Pawn {p.LabelShort} is switching to Positive mood.");
                                return ThoughtState.ActiveAtStage(1); // Positive mood
                            case 2:
                                //Log.Message($"Pawn {p.LabelShort} is switching to Negative mood.");
                                return ThoughtState.ActiveAtStage(2); // Negative mood
                            default:
                                //Log.Message($"Unexpected mood shift value for pawn {p.LabelShort}.");
                                return ThoughtState.Inactive;
                        }
                    }
                   else
                    {
                        //Log.Message($"Less than 4 hours have passed since last mood switch for pawn {p.LabelShort}.");

                        if (gameComponent.UnstablePawnLastMoodState.TryGetValue(p.thingIDNumber, out var lastMoodState))
                        {
                            return ThoughtState.ActiveAtStage(lastMoodState);
                        }
                        else
                        {
                            //Log.Message($"No recorded last mood state for pawn {p.LabelShort}.");
                            return ThoughtState.Inactive;
                        }
                    }
                }
                else
                {
                    //Log.Message($"No recorded last mood switch tick for pawn {p.LabelShort}.");
                }

                //Log.Message($"Returning Inactive ThoughtState for pawn {p.LabelShort}.");
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
