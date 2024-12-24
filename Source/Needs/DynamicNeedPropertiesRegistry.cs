using System;
using System.Collections.Generic;
using RimWorld;

namespace MindMatters;

public static class DynamicNeedPropertiesRegistry
{
    private static readonly Dictionary<Type, NeedDef> NeedPropertiesMap = new();

    static DynamicNeedPropertiesRegistry()
    {
        // Register default properties for dynamic needs
        RegisterDefaultProperties<FreshFruitNeed>(new NeedDef
        {
            defName = "FreshFruit",
            label = "Fresh Fruit",
            description = "This pawn craves fresh fruit to stay happy.",
            needClass = typeof(FreshFruitNeed),
            colonistAndPrisonersOnly = true,
            listPriority = 50
        });

        RegisterDefaultProperties<FormalityNeed>(new NeedDef
        {
            defName = "Formality",
            label = "Formality",
            description = "This pawn gains satisfaction from wearing formal or restrictive attire.",
            needClass = typeof(FormalityNeed),
            colonistAndPrisonersOnly = true,
            listPriority = 51
        });

        RegisterDefaultProperties<ConstraintNeed>(new NeedDef
        {
            defName = "Constraint",
            label = "Constraint",
            description = "This pawn feels more comfortable in restrictive environments.",
            needClass = typeof(ConstraintNeed),
            colonistAndPrisonersOnly = true,
            listPriority = 52
        });
    }

    public static void RegisterDefaultProperties<TNeed>(NeedDef properties) where TNeed : IDynamicNeed
    {
        var needType = typeof(TNeed);
        if (!NeedPropertiesMap.ContainsKey(needType))
        {
            NeedPropertiesMap.Add(needType, properties);
        }
    }

    public static NeedDef GetPropertiesFor(Type needType)
    {
        return NeedPropertiesMap.TryGetValue(needType, out var props) ? props : null;
    }

    public static bool HasProperties(Type needType)
    {
        return NeedPropertiesMap.ContainsKey(needType);
    }
}