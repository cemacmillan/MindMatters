if (painSpikeFromCorset)
  bus.Emit(new Experience {
    subject = pawn, kind = "TightCorset",
    valency = Valency.Negative(0.25f, 0.9f),
    tags = Tags.Pain | Tags.Restriction | Tags.Breath,
    intensity = painLevel
  });

  /* GPT trying to map this out in mod terms using existing (then) parts.
  Core mapping:
	•	Tags map to Impressions: Despair += 0.15*I (halfLife 40k), Vigilant += 0.1*I (halfLife 25k)
	•	If current oxygen debt + pain + Vigilant > threshold → Break_FaintingCorset.TryStart(pawn).

No job-specific Harmony necessary; you’re coupling via existing mechanics and a small break registry.

⸻

Migration path from your current stack
	1.	Introduce Bus + Experience DTO
Keep your PositiveConnections calls; just pivot to ExperienceBus.Emit(...).
	2.	Add ModeField + ImpressionBuffer (per pawn comp)
Wire in DynamicNeedsBridge with a couple of well-chosen influences (≤ 3).
	3.	Wrap existing “experience stack”
Keep it for now but have it also emit Impressions (dual path). Measure if you can delete it later.
	4.	Lift trait logic into Rotators
Start with Bipolar + Anxious. Give them only multipliers & decay shaping, nothing prescriptive.
	5.	Register breaks via a simple IBreak registry
FaintingCorset, Meltdown, CleaningFit—all discoverable, toggleable, and mode-gated.
	6.	UI
Tiny chroma strip + 1–2 recent influence icons. No diaries.
*/