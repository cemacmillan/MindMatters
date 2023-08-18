public class TraumaManager : MapComponent
{
    public TraumaManager(Map map) : base(map) { }

    public void OnPawnDowned(Pawn pawn)
    {
        // Check the downed pawn for trauma
        CheckForTrauma(pawn);

        // Check all other pawns on the map for trauma
        foreach (Pawn otherPawn in map.mapPawns.AllPawns)
        {
            if (otherPawn != pawn)
            {
                CheckForTrauma(otherPawn);
            }
        }
    }

    public void OnPawnDied(Pawn pawn)
    {
        // Check all other pawns on the map for trauma
        foreach (Pawn otherPawn in map.mapPawns.AllPawns)
        {
            CheckForTrauma(otherPawn);
        }
    }

    public void CheckForTrauma(Pawn pawn)
    {
        // Don't check for trauma if the pawn is currently having a mental break
        if (pawn.MentalState != null)
        {
            return;
        }

        // Only 50% chance to check for trauma
        if (Rand.Value < 0.5f)
        {
            // Check if the pawn has traits that might make them more susceptible to trauma
            if (pawn.story.traits.HasTrait(TraitDefOf.Wimp))
            {
                ApplyTrauma(pawn);
            }
            // You could add more conditions here to check for other traits or conditions
        }
    }

    public void ApplyTrauma(Pawn pawn)
    {
        // Apply the trauma hediff
        Hediff traumaHediff = HediffMaker.MakeHediff(HediffDef.Named("Trauma"), pawn);
        pawn.health.AddHediff(traumaHediff);

        // Add a hidden hediff to keep track of the trauma state
        Hediff hiddenTraumaState = HediffMaker.MakeHediff(HediffDef.Named("HiddenTraumaState"), pawn);
        pawn.health.AddHediff(hiddenTraumaState);
    }
}