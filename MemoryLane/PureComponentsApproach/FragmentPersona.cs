public sealed class Persona {
  public Pawn target;
  public float trust;      // 0..1
  public float salience;   // recency/importance 0..1
  public Valency last;     // last net valency
  public int lastTick;
}