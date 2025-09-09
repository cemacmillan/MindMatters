public sealed class Rotator_Lunar : IRotator
{
    private readonly float periodDays;  // 10f or 9f
    private readonly float evilCenter;  // around shortest day
    private readonly float evilWidth;   // spread of “anything can happen”
    public Rotator_Lunar(float periodDays=10f, float evilCenter=60f, float evilWidth=2.5f)
    { this.periodDays=periodDays; this.evilCenter=evilCenter; this.evilWidth=evilWidth; }

    public void Tick(Pawn p, ref ModeField m, float dt)
    {
        float day = GenDate.DayOfYear(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(p.MapHeld.Tile).x);
        float phase = (day % periodDays) / periodDays;           // 0..1
        float moon = 0.5f + 0.5f * Mathf.Sin(2f*Mathf.PI*phase); // 0..1

        // “Evil days” bump: gaussian around evilCenter
        float evil = Mathf.Exp(-Mathf.Pow((day - evilCenter)/evilWidth, 2f)); // 0..1

        // Shape modes
        m.Scale("Vigilant", 1f + 0.10f*moon + 0.20f*evil);
        m.Scale("Affiliative", 1f - 0.05f*evil);
        m.Scale("Despair", 1f + 0.08f*evil);
        m.AdjustNegHalfLife(1f + 0.25f*evil); // negative impressions linger more on evil days
        m.Normalize();
    }
}