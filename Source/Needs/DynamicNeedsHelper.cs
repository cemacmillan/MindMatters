using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MindMatters;

public static class DynamicNeedHelper
{
    private static readonly Dictionary<Type, DynamicNeedsBitmap> NeedBitmapMap = new();

    static DynamicNeedHelper()
    {
        /*
         *     None = 0,
           SeeArtwork = 1 << 0,
           VerifyStockLevels = 1 << 1,
           NothingOnFloor = 1 << 2,
           FreshFruit = 1 << 3,
           Formality = 1 << 4,
           Constraint = 1 << 5,

         */
     
        //RegisterNeed<FormalityNeed>(DynamicNeedsBitmap.Formality);
        //RegisterNeed<ConstraintNeed>(DynamicNeedsBitmap.Constraint);
        // RegisterNeed<FreshFruitNeed>(DynamicNeedsBitmap.FreshFruit);
       
    }

    /*
    public static void RegisterNeed<TNeed>(DynamicNeedsBitmap bitmap) where TNeed : IDynamicNeed
    {
        NeedBitmapMap[typeof(TNeed)] = bitmap;
    }

    public static DynamicNeedsBitmap GetBitmapForNeed<TNeed>() where TNeed : IDynamicNeed
    {
        return NeedBitmapMap.TryGetValue(typeof(TNeed), out var bitmap) ? bitmap : DynamicNeedsBitmap.None;
    }

    public static IEnumerable<IDynamicNeed> GetActiveNeedsFromBitmap(Pawn pawn, DynamicNeedsBitmap bitmap)
    {
        foreach (var entry in NeedBitmapMap)
        {
            if (IsNeedActive(bitmap, entry.Value))
            {
                yield return (IDynamicNeed)Activator.CreateInstance(entry.Key, pawn);
            }
        }
    }
    */

    public static DynamicNeedsBitmap AddNeed(DynamicNeedsBitmap current, DynamicNeedsBitmap toAdd)
    {
        return current | toAdd;
    }

    public static DynamicNeedsBitmap RemoveNeed(DynamicNeedsBitmap current, DynamicNeedsBitmap toRemove)
    {
        return current & ~toRemove;
    }

    public static bool IsNeedActive(DynamicNeedsBitmap current, DynamicNeedsBitmap toCheck)
    {
        return (current & toCheck) == toCheck;
    }
}