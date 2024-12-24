using Verse;
using RimWorld;
using System;

namespace MindMatters;

public static class MMToolkit
{
    public static void DebugLog(string log)
    {
        Log.Message(log);
    }

    public static void DebugWarn(string warning)
    {
        Log.Warning(warning);
    }

    public static void GripeOnce(string gripe)
    {
        Log.Warning(gripe);
    }
}