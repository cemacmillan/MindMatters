# Mind Matters Experience System Documentation

## Overview

The Mind Matters Experience system bridges the gap between the current MindMatters mod's `ExperienceManager` and the historic Psyche system. This document explains how experiences flow through the system and why you might not see them in the ITab yet.

## Architecture

### Two Experience Types

1. **`MindMatters.Experience`** (Internal)
   - Located in `/Source/Experience/Experience.cs`
   - Used by the current MindMatters mod
   - Contains: `EventType`, `Valency`, `Timestamp`, `Flags`

2. **`MindMattersInterface.Experience`** (External)
   - Located in `/MindMattersInterface/Source/StaticData/ExperienceValency.cs`
   - Used for mod-to-mod communication
   - Same structure but separate namespace

### Experience Flow

```
Game Event → ExperienceManager → Psyche System → ITab Visualization
     ↓              ↓                ↓              ↓
  (Death,      AddExperience()   ProcessExperience()  DrawExperiences()
   Surgery,    (Internal)        (Interface)          (Currently Empty)
   etc.)
```

## Key Components

### 1. ExperienceManager (`/Source/ExperienceManager.cs`)

**Purpose**: Central hub for managing pawn experiences

**Key Methods**:
- `AddExperience(Pawn pawn, Experience experience)` - Adds internal experience
- `ConvertToInterfaceExperience(Experience experience)` - Converts internal to interface
- `InitializePsycheSystem()` - Connects to Psyche system

**Integration Point**:
```csharp
// In AddExperience method (line ~138):
if (psycheSystem != null)
{
    psycheSystem.ProcessExperience(pawn, ConvertToInterfaceExperience(experience));
}
```

### 2. MindMattersPsyche (`/Source/Psyche/MindMattersPsyche.cs`)

**Purpose**: Main Psyche system component

**Key Methods**:
- `ProcessExperience(Pawn pawn, MindMattersInterface.Experience experience)` - Processes interface experiences
- `GetOrCreatePsyche(Pawn pawn)` - Gets or creates pawn psyche data
- `GetStatistics()` - Returns system statistics

**Event Subscription**:
```csharp
// In constructor:
experienceManager.OnExperienceAdded += ProcessExperience;
```

### 3. PawnPsyche (`/Source/Psyche/PawnPsyche.cs`)

**Purpose**: Individual pawn psychological state

**Key Data**:
- `modeWeights[]` - Array of psychological mode weights
- `recentExperiences` - List of recent PsycheExperience objects
- `DominantMode` - Currently dominant psychological mode

**Experience Processing**:
```csharp
public void ProcessExperience(MindMattersInterface.Experience experience)
{
    var psycheExp = ConvertToPsycheExperience(experience);
    recentExperiences.Add(psycheExp);
    // Update mode weights based on experience...
}
```

## Why Experiences Might Not Show in ITab

### 1. Experience Expiration

**Issue**: Experiences expire very quickly by default

**Location**: `PawnPsyche.ProcessExperience()` method
```csharp
// Experiences are added but may expire before visualization
recentExperiences.Add(psycheExp);
```

**Solution**: Check experience expiration logic or extend expiration time for testing

### 2. ITab Implementation Gap

**Current State**: `GetRecentExperiences()` method returns empty list
```csharp
private List<Experience> GetRecentExperiences(Pawn pawn)
{
    // TODO: Implement actual experience retrieval
    return new List<Experience>();
}
```

**Missing Connection**: ITab doesn't yet connect to `PawnPsyche.recentExperiences`

### 3. Experience Sources

**Current Sources**:
- Death events (`SomeoneDiedPatch`)
- Surgery failures (`SurgeryKilledPatient`)
- Grave filling (`Building_Grave_NotifyHauledTo_Patch_1.5`)

**Potential Issues**:
- Events might not be triggering
- Experience valency might be too low
- Pawns might not have psyche data yet

## Debugging Steps

### 1. Check Experience Creation
Add debug logging to `ExperienceManager.AddExperience()`:
```csharp
MMToolkit.DebugLog($"[ExperienceManager] Added experience {experience.EventType} ({experience.Valency}) for {pawn.LabelShort}");
```

### 2. Check Psyche Processing
Add debug logging to `MindMattersPsyche.ProcessExperience()`:
```csharp
MMToolkit.DebugLog($"[MindMattersPsyche] Processing experience {experience.EventType} for {pawn.LabelShort}");
```

### 3. Check ITab Data
Add debug logging to `ITab_PsycheMap.DrawPsycheMap()`:
```csharp
var experiences = GetRecentExperiences(pawn);
MMToolkit.DebugLog($"[ITab_PsycheMap] Found {experiences.Count} experiences for {pawn.LabelShort}");
```

### 4. Verify Psyche Data
Check if pawn has psyche data:
```csharp
var pawnPsyche = psycheSystem.GetOrCreatePsyche(pawn);
MMToolkit.DebugLog($"[ITab_PsycheMap] Pawn psyche has {pawnPsyche.recentExperiences.Count} recent experiences");
```

## Next Steps

### 1. Complete ITab Implementation
- Implement `GetRecentExperiences()` to return `pawnPsyche.recentExperiences`
- Convert `PsycheExperience` to `Experience` for display
- Add experience expiration filtering

### 2. Extend Experience Duration
- Modify experience expiration logic
- Add configuration options for experience duration

### 3. Add More Experience Sources
- Social interactions
- Environmental events
- Mood changes
- Trait gains/losses

### 4. Improve Visualization
- Add experience tooltips
- Show experience timestamps
- Color-code by valency
- Add experience details

## Configuration Notes

### GUI Scaling
- Current size: `1120f x 840f` (2.8x increase)
- Should respect `Prefs.UIScale`
- Future: Add mod settings for size customization

### Experience Settings
- Default expiration: Very fast (needs investigation)
- Valency thresholds: Currently filtering for ≥5 mood impact
- Visualization distance: 120f from pawn center

## Files Modified

- `/Source/ITab_PsycheMap.cs` - Size increased, subtitle font reduced
- `/Source/ExperienceManager.cs` - Psyche system integration
- `/Source/Psyche/MindMattersPsyche.cs` - Main psyche component
- `/Source/Psyche/PawnPsyche.cs` - Individual pawn psyche
- `/Source/Harmony/PawnInspectorTabsPatch.cs` - ITab registration

This system provides a solid foundation for psychological visualization, but the ITab needs completion to show actual experience data.
