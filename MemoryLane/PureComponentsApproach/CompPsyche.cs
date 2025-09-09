public class CompPsyche : ThingComp
{
    public static readonly List<string> Modes = new() { "Calm","Vigilant","Affiliative","Acquisitive","Despair","Defiant" };

    // Mode weights sum to ~1
    public float[] weights = new float[Modes.Count];  // initialize to uniform
    public float[,] M = new float[6,6];               // interaction matrix

    // Impressions (small ring buffer)
    private struct Impression { public int channel; public float mag; public float halfLifeTicks; public int tick0; }
    private readonly Queue<Impression> buffer = new();  // cap e.g. 64

    public override void CompTickRare()
    {
        DecayImpressions(250);
        ApplyDynamicNeedsCoupling();
        NormalizeWeights();
    }

    public void PushEvent(PsyEvent e)
    {
        // Map event -> channels
        foreach (var (modeIdx, delta, halfLife) in e.ToImpressionDeltas(this))
            EnqueueImpression(modeIdx, delta, halfLife);
    }

    private void EnqueueImpression(int ch, float mag, float hl)
    {
        if (buffer.Count > 64) buffer.Dequeue();
        buffer.Enqueue(new Impression { channel=ch, mag=mag, halfLifeTicks=hl, tick0=Find.TickManager.TicksGame });
    }

    private void DecayImpressions(int dt)
    {
        // apply decayed contributions, drop tiny remnants
        int n = buffer.Count;
        for (int i = 0; i < n; i++)
        {
            var imp = buffer.Dequeue();
            float age = Find.TickManager.TicksGame - imp.tick0;
            float factor = Mathf.Exp(-age / imp.halfLifeTicks);
            if (factor > 0.02f)
            {
                weights[imp.channel] += imp.mag * factor * 0.1f; // small nudges
                buffer.Enqueue(imp); // keep until smaller
            }
        }
        // interactions
        float[] w2 = (float[])weights.Clone();
        for (int i=0;i<weights.Length;i++)
            for (int j=0;j<weights.Length;j++)
                w2[i] += M[i,j] * weights[j];
        weights = w2;
    }

    private void ApplyDynamicNeedsCoupling()
    {
        var pawn = (Pawn)this.parent;
        // Example: hunger pushes Acquisitive target up a bit
        float hunger = 1f - pawn.needs.food.CurLevel; // 0..1
        int acquisIdx = Modes.IndexOf("Acquisitive");
        weights[acquisIdx] += 0.15f * hunger;

        // Social isolation pushes Affiliative
        float solitude = 1f - pawn.needs.joy.CurLevel;
        int affIdx = Modes.IndexOf("Affiliative");
        weights[affIdx] += 0.10f * solitude;

        // High Despair slightly slows Joy recovery (handled via Hediff/Need offset elsewhere)
    }

    private void NormalizeWeights()
    {
        for (int i=0;i<weights.Length;i++) weights[i] = Mathf.Max(0f, weights[i]);
        float sum = weights.Sum();
        if (sum <= 1e-5f) { for (int i=0;i<weights.Length;i++) weights[i]=1f/weights.Length; return; }
        for (int i=0;i<weights.Length;i++) weights[i] /= sum;
    }
}

// Event mapping hook (no job-specific Harmony required)
public class PsyEvent
{
    public string defName;
    public float intensity;

    public IEnumerable<(int modeIdx, float delta, float halfLife)> ToImpressionDeltas(CompPsyche comp)
    {
        // e.g., BetrayedByAlly
        if (defName == "BetrayedByAlly")
        {
            yield return (comp.ModeIndex("Defiant"), +0.25f*intensity, 60000f);   // day
            yield return (comp.ModeIndex("Despair"), +0.15f*intensity, 90000f);   // 1.5 day
            yield return (comp.ModeIndex("Affiliative"), -0.10f*intensity, 30000f);
        }
        // Map other Tale/Thought/Letter/Signal types similarly
    }
}