public void AddExperience(Pawn pawn, Experience experience)
{
    if (pawn == null || experience == null) { /* warn */ return; }

    // Old behavior (compat)
    var experiences = GetOrCreateExperiences(pawn);
    experiences.Add(experience);
    OnExperienceAdded?.Invoke(pawn, ConvertToInterfaceExperience(experience));

    // New path: push into the pawn psyche
    ForwardToPsyche(pawn, experience);

    // Optional: coalesce logs to avoid spam
    // MMToolkit.DebugLog($"[MMXP] + {experience.EventType} {experience.Valency} → {pawn.LabelShort}");
}