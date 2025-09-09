using Verse;
using RimWorld;
using System;

namespace MindMatters;

public static class MMToolkit
{
    private static readonly System.Collections.Generic.HashSet<int> LoggedWarnings = new();
    
    // Debug levels for more granular control
    public enum DebugLevel
    {
        None = 0,
        Basic = 1,      // Essential system messages
        Detailed = 2,   // Detailed debugging (default for most systems)
        Verbose = 3     // Very verbose output (DynamicNeeds, etc.)
    }
    
    public static void DebugLog(string log, DebugLevel level = DebugLevel.Detailed)
    {
        if(MindMattersMod.settings.enableLogging && (int)level <= MindMattersMod.settings.debugLevel) { 
            Log.Message(log);
        }
    }

    public static void DebugLogVerbose(string log)
    {
        if(MindMattersMod.settings.enableLogging) { 
            Log.Message(log);
        }
    }

    public static void DebugWarn(string warning)
    {
        if(MindMattersMod.settings.enableLogging) { 
            Log.Warning(warning);
        }
    }

    public static void GripeOnce(string message)
    {
        int hash = UnityEngine.Animator.StringToHash(message);
        if (!LoggedWarnings.Contains(hash))
        {
            LoggedWarnings.Add(hash);
            Log.WarningOnce($"[MindMatters] {message}", hash);
        }
    }
}