using Verse;
using RimWorld;
using System;

namespace MindMatters;

public static class MMToolkit
{
    private static readonly System.Collections.Generic.HashSet<int> LoggedWarnings = new();
    
    public static void DebugLog(string log)
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