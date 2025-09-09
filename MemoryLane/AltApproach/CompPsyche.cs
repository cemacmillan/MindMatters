public class CompPsyche : ThingComp
{
    public IPsyche Psyche { get; private set; }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        Psyche = new PsycheImpl(new PsycheHost(this));
        MindMattersExperienceComponent.GetOrCreateInstance()
            ?.OnExperienceAdded += OnRouted; // optional; or call via router directly
    }

    private void OnRouted(Pawn p, MindMattersInterface.Experience xp)
    {
        if (p == this.parent as Pawn)
            Psyche.Ingest(ExperienceMapper.Map(xp));
    }

    public override void CompTickRare()
    {
        Psyche.Tick(GenTicks.TickLongInterval);
        DynamicNeedsBridge.Apply((Pawn)this.parent, Psyche);
    }

    public override string CompInspectStringExtra()
    {
        var w = Psyche.Weights;
        int i = Psyche.DominantModeIndex();
        return $"Chroma: {PsycheHost.Modes[i]} ({w[i]:P0})";
    }
}