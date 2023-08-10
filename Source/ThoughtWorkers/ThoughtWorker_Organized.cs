using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MindMatters
{
    public class ThoughtWorker_Organized : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.Map == null || !p.story.traits.HasTrait(MindMattersTraits.Organized))
            {
                return ThoughtState.Inactive;
            }

            if (p.Position.Roofed(p.Map))
            {
                Room room = p.GetRoom();
                if (room != null)
                {
                    float cleanliness = GetRoomCleanliness(room);

                    // Now use cleanliness score in your thought logic.
                    // You may want to adjust the threshold values according to your game logic.
                    if (cleanliness < 0.0f)
                    {
                        return ThoughtState.ActiveAtStage(1);
                    }
                    else
                    {
                        return ThoughtState.ActiveAtStage(0);
                    }
                }
            }

            return ThoughtState.Inactive;
        }

        private float GetRoomCleanliness(Room room)
        {
            float cleanliness = 0f;
            if (room?.ContainedAndAdjacentThings != null)
            {
                List<Thing> things = room.ContainedAndAdjacentThings;
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing?.def != null && (thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Filth || thing.def.category == ThingCategory.Plant))
                    {
                        cleanliness += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Cleanliness);
                    }
                }
            }

            if (room?.Cells != null && room.Map != null)
            {
                foreach (IntVec3 cell in room.Cells)
                {
                    TerrainDef terrain = cell.GetTerrain(room.Map);
                    if (terrain != null)
                    {
                        cleanliness += terrain.GetStatValueAbstract(StatDefOf.Cleanliness);
                    }
                }
            }

            return (room?.CellCount > 0) ? cleanliness / (float)room.CellCount : 0;
        }
    }
}
