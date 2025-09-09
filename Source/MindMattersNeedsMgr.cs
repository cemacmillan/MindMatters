using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MindMatters;

public class MindMattersNeedsMgr : IExposable
{
    // ----------------------------------------------------------------------
    // DATA FIELDS
    // ----------------------------------------------------------------------

    private static Dictionary<Pawn, Dictionary<DynamicNeedsBitmap, DynamicNeedState>> pawnNeedsMap = new();

    private bool isProcessing = false;
    private int ticksUntilNextProcess = 0;
    private static bool awarenessCheckOK;
    
    // game component instance handle
    protected static MindMattersGameComponent GameComponentInstance { get; private set; }
    
    public MindMattersNeedsMgr()
    {
        // Initialize the reference to the game component instance
        GameComponentInstance = MindMattersGameComponent.Instance;
    }

    // New fancy method to deal with flags for PawnNeedStatus
    private static DynamicNeedState MapPawnNeedStatusToDynamicNeedState(PawnNeedStatus status)
    {
        DynamicNeedState state = DynamicNeedState.None;

        if (status.HasFlag(PawnNeedStatus.Passive))
            state |= DynamicNeedState.Available;
        if (status.HasFlag(PawnNeedStatus.Active))
            state |= DynamicNeedState.Available; // Observed handled separately
        if (status.HasFlag(PawnNeedStatus.Fulminant))
            state |= DynamicNeedState.Triggered;
        if (status.HasFlag(PawnNeedStatus.Deprecated))
            state |= DynamicNeedState.Disabled; // Explicitly disabled by global or external action

        return state;
    }
    
    // ----------------------------------------------------------------------
    // AGGREGATOR FOR THE HARMONY PATCH
    // ----------------------------------------------------------------------
    public static bool CanPawnHaveDynamicNeed(Pawn pawn, NeedDef needDef)
    {
        // Basic null and destroyed checks
        if (pawn == null || pawn.Dead || pawn.Destroyed || needDef == null)
        {
            return false;
        }

        // Check if the instPawn is incomplete or a PawnKind template
        if (pawn.RaceProps == null || pawn.health == null || pawn.story == null)
        {
            return false;
        }

        // Cannot satisfy IDE for the following warning without extraneous tests
        if ((int)pawn.RaceProps.intelligence < (int)needDef.minIntelligence)
        {
            return false;
        }

        // Developmental stage filtering
        if (!needDef.developmentalStageFilter.Has(pawn.DevelopmentalStage))
        {
            return false;
        }

        // Faction-based exclusions
        if (needDef.colonistsOnly && (pawn.Faction == null || !pawn.Faction.IsPlayer))
        {
            return false;
        }

        // Slaves and prisoners
        if (needDef.neverOnPrisoner && pawn.IsPrisoner)
        {
            return false;
        }

        if (needDef.neverOnSlave && pawn.IsSlave)
        {
            return false;
        }

        // Hediff-based exclusions
        if (pawn.health.hediffSet.DisablesNeed(needDef))
        {
            return false;
        }

        // Trait-based exclusions
        if (pawn.story.traits.allTraits.Any(t =>
                !t.Suppressed && t.CurrentData.disablesNeeds.NotNullAndContains(needDef)))
        {
            return false;
        }

        // Gene-based exclusions (Biotech)
        if (ModsConfig.BiotechActive && pawn.genes != null)
        {
            if (pawn.genes.GenesListForReading.Any(g =>
                    g.Active && g.def.disablesNeeds != null && g.def.disablesNeeds.Contains(needDef)))
            {
                return false;
            }
        }


        // Retrieve the bitmap for the need
        DynamicNeedsBitmap needBitmap = DynamicNeedsRegistry.GetBitmapForNeed(needDef.needClass);
        if (needBitmap == DynamicNeedsBitmap.None)
        {
            MMToolkit.GripeOnce($"[MindMatters] Invalid dynamic need bitmap for {needDef.defName}. Skipping.");
            return false;
        }
            
        //MMToolkit.DebugLog($"[MindMatters] CanPawnHaveDynamicNeed: Processing NeedDef={needDef.defName} (Bitmap={needBitmap}) for Pawn={instPawn.LabelShort}");

        // test player settings, etc. to make sure we should go ahead and are not shutting down. Currently, returns true.
        awarenessCheckOK = SituationalAwarenessCheck(pawn, needDef);

        if (awarenessCheckOK == false)
        {
            return false;
        }

        // Check if the need is already known and determine its status
        NeedStatus needStatus = KnownNeedForPawn(needBitmap, pawn);
        //MMToolkit.DebugLog($"[MindMatters] CanPawnHaveDynamicNeed: KnownNeedForPawn returned PawnStatus={needStatus.PawnStatus} for Bitmap={needBitmap}, Pawn={instPawn.LabelShort}");

        // Handle known statuses
        switch (needStatus.PawnStatus)
        {
            case PawnNeedStatus.Active:
                return true; // Already active

            case PawnNeedStatus.Passive:
                // Reactivate if the global state permits
                if (needStatus.GlobalState.HasFlag(DynamicNeedState.Available))
                {
                    MindMattersNeedsMgr.UpdatePawnNeedStatus(needBitmap, pawn, PawnNeedStatus.Active);
                    return true;
                }
                break;

            case PawnNeedStatus.Deprecated:
            case PawnNeedStatus.None:
            default:
                // Proceed to dynamic-specific checks for these statuses
                break;
        }

        // Dynamic-specific check
        if (Activator.CreateInstance(needDef.needClass) is DynamicNeed dynNeed)
        {
            try
            {
                bool shouldHave = dynNeed.ShouldPawnHaveThisNeed(pawn);

                // Map shouldHave to a DynamicNeedState
                DynamicNeedState updatedState = shouldHave
                    ? DynamicNeedState.Available // The need is valid and should be active
                    : DynamicNeedState.Disabled; // The need is not valid for this instPawn

                // Update the state map based on the result
                UpdateNeedStateForPawn(needBitmap, pawn, updatedState);

                return shouldHave; // Return the boolean for higher-level logic
            }
            catch (Exception ex)
            {
                MMToolkit.GripeOnce(
                    $"CanPawnHaveDynamicNeed: Error while determining if {pawn.LabelShort} should have Need {needDef.defName}: {ex.Message}"
                );
                return false;
            }
        }

        MMToolkit.DebugLog($"CanPawnHaveDynamicNeed: Final check returned false for Pawn={pawn.LabelShort}, NeedDef={needDef.defName}");
        return false;
    }
    // ----------------------------------------------------------------------
    // SITUATIONAL AWARENESS CHECK
    // ----------------------------------------------------------------------

    private static bool SituationalAwarenessCheck(Pawn pawn, NeedDef needDef)
    {
        // Placeholder logic: Expand as needed
        // E.g., Prevent needs from being added during high-priority situations
        // or when the player explicitly opts out.
        return true; // For now, always allow
    }

    public static NeedStatus KnownNeedForPawn(DynamicNeedsBitmap bitmap, Pawn pawn)
    {
        if (!pawnNeedsMap.TryGetValue(pawn, out var needStateMap))
        {
            return new NeedStatus(PawnNeedStatus.None, DynamicNeedState.None,
                message: "Pawn has no registered needs.");
        }

        if (needStateMap.TryGetValue(bitmap, out var state))
        {
            PawnNeedStatus status = PawnNeedStatus.None;

            if (state.HasFlag(DynamicNeedState.Observed))
                status |= PawnNeedStatus.Active;
            if (state.HasFlag(DynamicNeedState.Triggered))
                status |= PawnNeedStatus.Fulminant;
            if (state.HasFlag(DynamicNeedState.Available))
                status |= PawnNeedStatus.Passive;
            if (state.HasFlag(DynamicNeedState.Disabled))
                status |= PawnNeedStatus.Deprecated;

            return new NeedStatus(status, state, message: "Need state successfully retrieved.");
        }

        return new NeedStatus(PawnNeedStatus.None, DynamicNeedState.None,
            message: "No state found for this need.");
    }
    
    public static void UpdatePawnNeedStatus(DynamicNeedsBitmap needBitmap, Pawn pawn, PawnNeedStatus newStatus)
    {
        if (pawn == null)
            throw new ArgumentNullException(nameof(pawn));

        if (!pawnNeedsMap.TryGetValue(pawn, out var needStateMap))
        {
            needStateMap = new Dictionary<DynamicNeedsBitmap, DynamicNeedState>();
            pawnNeedsMap[pawn] = needStateMap;
        }

        // Convert PawnNeedStatus to DynamicNeedState for internal consistency
        var newDynamicState = MapPawnNeedStatusToDynamicNeedState(newStatus);

        if (needStateMap.TryGetValue(needBitmap, out var currentState))
        {
            if (currentState.HasFlag(newDynamicState))
            {
                // Avoid redundant updates
                return;
            }

            // Update the current state with the new flags
            needStateMap[needBitmap] |= newDynamicState;
        }
        else
        {
            // Add the new state
            needStateMap[needBitmap] = newDynamicState;
        }

        //MMToolkit.DebugLog(
          //  $"[MindMatters] Updated need status for {instPawn.LabelShort}: {needBitmap} -> {newStatus} ({newDynamicState})"
        //);
    }

    public static void UpdateNeedStateForPawn(DynamicNeedsBitmap needBitmap, Pawn pawn, DynamicNeedState newState)
    {
        // Ensure global states are not assigned at the instPawn level
        if (newState == DynamicNeedState.Observed || newState == DynamicNeedState.Triggered)
        {
            MMToolkit.GripeOnce($"[MindMatters] Invalid use of global state ({newState}) for instPawn-specific data.");
            return;
        }

        // Ensure the instPawn entry exists in the main map
        if (!pawnNeedsMap.TryGetValue(pawn, out var needStateMap))
        {
            needStateMap = new Dictionary<DynamicNeedsBitmap, DynamicNeedState>();
            pawnNeedsMap[pawn] = needStateMap;
        }

        // Get the current state (default to None if not present)
        var currentState = needStateMap.TryGetValue(needBitmap, out var existingState)
            ? existingState
            : DynamicNeedState.None;

        // Avoid redundant updates
        if (currentState == newState)
        {
            return;
        }

        // Update the state
        needStateMap[needBitmap] = newState;

       // MMToolkit.DebugLog($"[MindMatters] Updated need state for {instPawn.LabelShort}: {needBitmap} -> {newState}");
    }
    // ----------------------------------------------------------------------
    // TICKING / UPDATING ALL DYNAMIC NEEDS
    // ----------------------------------------------------------------------

    public void TickAllDynamicNeeds(int specificDelay = 0)
    {
        foreach (var pawn in PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike) continue;

            foreach (IDynamicNeed? need in pawn.needs.AllNeeds.OfType<IDynamicNeed>())
            {
                try
                {
                    need.NeedInterval();
                }
                catch (Exception ex)
                {
                    MMToolkit.GripeOnce(
                        $"[MindMatters] Error ticking dynamic need '{need}' for '{pawn.LabelShort}': {ex.Message}"
                    );
                }
            }
        }

        if (specificDelay > 0)
        {
            ticksUntilNextProcess = specificDelay;
        }
    }

    // ----------------------------------------------------------------------
    // MASTER PROCESS METHOD
    // ----------------------------------------------------------------------

    public void ProcessNeeds(
        Pawn pawn = null,
        DynamicNeedCategory? category = null,
        bool exclusive = false,
        string failures = "warning",
        int specificDelay = 0
    )
    {
        if (exclusive && isProcessing)
        {
            if (failures == "exception")
            {
                throw new InvalidOperationException("[MindMatters] Exclusive processing in progress.");
            }

            MMToolkit.GripeOnce("[MindMatters] Exclusive processing in progress. Skipping.");
            return;
        }

        if (specificDelay > 0 && ticksUntilNextProcess > 0)
        {
            ticksUntilNextProcess--;
            if (ticksUntilNextProcess > 0) return;
        }

        isProcessing = true;

        try
        {
            IEnumerable<Pawn> pawns = pawn != null
                ? new List<Pawn> { pawn }
                : PawnsFinder.AllMaps_FreeColonistsAndPrisonersSpawned;

            foreach (var p in pawns)
            {
                if (p == null || !p.RaceProps.Humanlike) continue;

                foreach (DynamicNeedProperties? needProps in DynamicNeedsRegistry.GetNeedsForCategory(category))
                {
                    var bitmap = DynamicNeedsRegistry.GetBitmapForNeed(needProps.NeedClass);
                    bool alreadyHasIt = HasNeed(p, bitmap);

                    //MMToolkit.DebugLog($"[MindMatters] Processing Need for {p.LabelShort}: Bitmap={bitmap}, AlreadyHasIt={alreadyHasIt}");

                    var needDef = DynamicNeedsRegistry.GetNeedDef(needProps.NeedClass);
                    if (needDef == null)
                    {
                        MMToolkit.DebugLog($"[MindMatters] NeedDef is null for bitmap: {bitmap}");
                        continue;
                    }

                    bool shouldHave = CanPawnHaveDynamicNeed(p, needDef);

                    // MMToolkit.DebugLog($"[MindMatters] CanPawnHaveDynamicNeed={shouldHave} for {needDef.defName} (Bitmap={bitmap})");

                    // Handle Needs already present in map
                    if (alreadyHasIt)
                    {
                        NeedStatus needStatus = KnownNeedForPawn(bitmap, p);
                        // MMToolkit.DebugLog($"[MindMatters] KnownNeedForPawn: Status={needStatus.PawnStatus}, GlobalState={needStatus.GlobalState}");

                        if (shouldHave)
                        {
                            if (needStatus.PawnStatus != PawnNeedStatus.Active)
                            {
                                // Ensure the instPawn-specific status is updated
                                UpdatePawnNeedStatus(bitmap, p, PawnNeedStatus.Active);
                            }

                            // No need to update `DynamicNeedState.Observed` here; it’s a global state
                        }
                        else
                        {
                            // Remove the need if it shouldn't be there
                            RemoveNeed(p, bitmap);
                        }

                        continue;
                    }

                    // Handle new Needs
                    if (shouldHave && !alreadyHasIt)
                    {
                        try
                        {
                            //MMToolkit.DebugLog($"[MindMatters] Adding Need: {needDef.defName} to {p.LabelShort}");
                            TryAddNeed(p, needDef);
                        }
                        catch (Exception ex)
                        {
                            HandleProcessingFailure(failures, ex, needProps.NeedClass, p);
                        }
                    }
                }
            }
        }
        finally
        {
            isProcessing = false;
            if (specificDelay > 0) ticksUntilNextProcess = specificDelay;
        }
    }

    private void TryAddNeed(Pawn pawn, NeedDef needDef)
    {
        // Check if the need already exists in AllNeeds
        if (pawn.needs.TryGetNeed(needDef) != null)
        {
            // MMToolkit.DebugLog(
            //  $"[MindMatters] Need '{needDef.defName}' already exists for {instPawn.LabelShort}.");
            return;
        }

        // Create and add the new need safely
        Need? newNeed = (Need)Activator.CreateInstance(needDef.needClass, pawn);
        if (newNeed == null)
        {
            MMToolkit.GripeOnce(
                $"[MindMatters] Failed to create instance for NeedDef '{needDef.defName}'.");
            return;
        }

        newNeed.def = needDef;
        newNeed.SetInitialLevel();

        pawn.needs.AllNeeds.Add(newNeed);
        pawn.needs.BindDirectNeedFields(); // Ensures proper bindings

        // MMToolkit.DebugLog(
        //  $"[MindMatters] Successfully added need '{needDef.defName}' to {instPawn.LabelShort}.");
    }

    public void InitializePawnNeeds(Pawn pawn)
    {
        ProcessNeeds(pawn: pawn);
    }

    // ----------------------------------------------------------------------
    // ADD / REMOVE / HAS
    // ----------------------------------------------------------------------

    public void AddNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
    {
        if (pawn == null)
            throw new ArgumentNullException(nameof(pawn));

        // Ensure the instPawn entry exists in the main map
        if (!pawnNeedsMap.TryGetValue(pawn, out Dictionary<DynamicNeedsBitmap, DynamicNeedState> needStateMap))
        {
            needStateMap = new Dictionary<DynamicNeedsBitmap, DynamicNeedState>();
            pawnNeedsMap[pawn] = needStateMap;
        }

        // Add or update the specific need's state in the inner map
        if (!needStateMap.ContainsKey(needBitmap))
        {
            needStateMap[needBitmap] = DynamicNeedState.Available; // Default state when adding
        }
        else
        {
            needStateMap[needBitmap] |= DynamicNeedState.Observed; // Optionally upgrade state
        }

        // Retrieve properties for the bitmap
        DynamicNeedProperties needProps = DynamicNeedsRegistry.GetPropertiesForBitmap(needBitmap);
        if (needProps == null)
        {
            MMToolkit.GripeOnce($"[MindMatters] No properties found for bitmap: {needBitmap}");
            return;
        }

        // Create a new dynamic need instance
        DynamicNeed newNeed = DynamicNeedFactory.CreateNeedInstance(needProps.NeedClass, pawn);
        if (newNeed != null)
        {
            // Add the need to the instPawn's needs if allowed by settings
            if (!MindMattersMod.settings.DoNotAddNeeds)
            {
                pawn.needs.AllNeeds.Add(newNeed);
            }

            // Debug logging can be re-enabled if needed
            // MMToolkit.DebugLog(
            //   $"[MindMatters] Successfully added need '{newNeed.def.defName}' to {instPawn.LabelShort}."
            // );
        }
    }

    public void RemoveNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
    {
        if (pawn == null)
            throw new ArgumentNullException(nameof(pawn));

        // Check if the instPawn has a needs map
        if (!pawnNeedsMap.TryGetValue(pawn, out Dictionary<DynamicNeedsBitmap, DynamicNeedState> needStateMap))
        {
            MMToolkit.GripeOnce(
                $"[MindMatters] Attempted to remove a need for a instPawn with no registered needs: {pawn.LabelShort}");
            return;
        }

        // Remove the specific need's state from the inner map
        if (needStateMap.ContainsKey(needBitmap))
        {
            needStateMap.Remove(needBitmap);
        }
        else
        {
            MMToolkit.GripeOnce(
                $"[MindMatters] Attempted to remove a non-existent need from {pawn.LabelShort}: {needBitmap}");
            return;
        }

        // Retrieve properties for the bitmap
        DynamicNeedProperties needProps = DynamicNeedsRegistry.GetPropertiesForBitmap(needBitmap);
        if (needProps == null)
        {
            MMToolkit.GripeOnce(
                $"[MindMatters] No properties found for bitmap: {needBitmap}. Cannot proceed with removal.");
            return;
        }

        // Retrieve the need definition
        NeedDef needDef = DynamicNeedsRegistry.GetNeedDef(needProps.NeedClass);
        if (needDef == null)
        {
            MMToolkit.GripeOnce(
                $"[MindMatters] No NeedDef found for {needProps.NeedClass}. Cannot proceed with removal.");
            return;
        }

        // Attempt to remove the need from the instPawn's needs list
        Need need = pawn.needs.TryGetNeed(needDef);
        if (need != null)
        {
            if (!MindMattersMod.settings.DoNotAddNeeds)
            {
                pawn.needs.AllNeeds.Remove(need);
            }

            // Debug logging can be re-enabled if needed
            // MMToolkit.DebugLog(
            //   $"[MindMatters] Successfully removed need '{needDef.defName}' from {instPawn.LabelShort}."
            // );
        }
    }

    public bool HasNeed(Pawn pawn, DynamicNeedsBitmap needBitmap)
    {
        if (pawn == null)
            throw new ArgumentNullException(nameof(pawn));

        // Check if the instPawn has a registered needs map
        if (pawnNeedsMap.TryGetValue(pawn, out Dictionary<DynamicNeedsBitmap, DynamicNeedState> needStateMap))
        {
            // Check if the specific need bitmap exists in the state map and is active
            if (needStateMap.TryGetValue(needBitmap, out DynamicNeedState state))
            {
                return state.HasFlag(DynamicNeedState.Observed);
            }
        }

        return false; // Need is not active or not registered
    }

    private void HandleProcessingFailure(string failures, Exception ex, Type needClass, Pawn pawn)
    {
        string message =
            $"[MindMatters] Failed to process need '{needClass.Name}' for {pawn.LabelShort}: {ex.Message}";
        if (failures == "exception")
        {
            throw new InvalidOperationException(message, ex);
        }
        else if (failures == "warning")
        {
            MMToolkit.GripeOnce(message);
        }
    }

    public void ExposeData()
    {
        // Reload pawnNeedsMap
        Scribe_Collections.Look(ref pawnNeedsMap, "pawnNeedsMap", LookMode.Reference, LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            foreach (var kvp in pawnNeedsMap)
            {
                Pawn pawn = kvp.Key;
                if (pawn != null && pawn.Spawned && pawn.needs != null)
                {
                    // Use the static reference to add the instPawn to the pending queue
                    GameComponentInstance?.pendingPawnQueue.Add(pawn);
                }
            }
        }
        // !snip
    }

    /*
    public void ExposeData()
    {
        Scribe_Collections.Look(ref pawnNeedsMap, "pawnNeedsMap", LookMode.Reference, LookMode.Value);
    }
    */
}