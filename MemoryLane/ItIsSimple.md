Your proposal—keeping the “official” NeedDef in the game’s database but simultaneously using your own pawnNeedsMap / Registry / NeedsMgr as the canonical source of truth—is a very common and very workable approach. The main difference from the usual “static” approach is that you’re letting RimWorld see the existence of your NeedDef but not letting the game’s default logic fully govern creation or removal. Instead, you watch for (and intercept, or undo) any attempts by vanilla code to add/remove your dynamic need. Below are some considerations and best practices around that design.

1. Why keep NeedDef at all?

- Agreed on all points. The NeedDefs are RWMCS NeedDefs, expressed in XML. Probably no ModExtension trickery required. We can make sure they're impossible by normal means. An effectively inactive NeedDef is never visible to the player except in dev mode.

2. Intercepting or Overriding Removal (“ShouldHaveNeed”)


if (!ShouldHaveNeed(def))
{
    this.allNeeds.RemoveAll(n => n.def == def);
}

We'll try to figure out a clever, and tardive way to patch here. Already, we patch this in Symbiophore so we've a good example already of a 

2.1 Harmony patch ShouldHaveNeed

A very direct approach is to patch Pawn_NeedsTracker.ShouldHaveNeed(NeedDef def). You might do:
	•	Prefix: If def.needClass is one of your dynamic types, consult your pawnNeedsMap or NeedsMgr. If your code says “Yes, we want this need,” return true and skip the original method. If your code says “No,” return false.
	•	Postfix: Alternatively, you can let the base logic run, then override the result if needed.

In other words, *the game’s “official” logic never removes your dynamic need, because you forcibly say “Yes, we do want this” if your registry says so.
---

2.2 Letting the game remove it, then re-add

Another pattern: you let the default logic remove the need if ShouldHaveNeed is false from the game’s perspective, then next tick (or next frame) your code sees “Wait, that pawn still meets the conditions for FreshFruitNeed—the registry says so—so let’s re-add.” That works but can cause spamming add/remove calls if the base logic and your logic disagree. Generally it’s simpler to intercept at ShouldHaveNeed and say “No, we actually do need it, thank you very much.”

3. Dealing with “Impossible to have Need” scenarios

You mentioned wanting to mark the NeedDef so that vanilla’s own code decides “This is never applicable,” e.g. by giving it constraints that are always false (like colonistAndPrisonersOnly=false but minIntelligence=ToolUser, etc.) so the base game never tries to add it automatically. This is effectively zeroing out the “vanilla” path. Then you do your own add in NeedsMgr.

Yes—that’s a great approach to keep it out of the normal loop. The base game won’t see any reason to add or remove it, because from the game’s perspective, it’s a never-qualifying need. Meanwhile you can do:

pawn.needs.AddNeed(new FreshFruitNeed(pawn, myNeedDef));

…and it’s there. If at some point the base logic re-checks ShouldHaveNeed, it sees “No, minIntelligence is too high,” or “This is not a colonist or prisoner,” etc., tries to remove it—but if you patched ShouldHaveNeed to say “Yes, keep it,” then that works. Or you intercept removal in another Harmony patch. Either approach prevents flip-flop.

4. The “Source of Truth” Concept

You’re basically layering an additional state machine on top of RimWorld’s. That’s perfectly fine, especially for “dynamic” or “temporary” needs:
	•	Registry / “NeedsMgr” is the canonical authority about whether a given pawn should have that need right now and at what current level (in case you want to re-add it).
	•	RimWorld’s needs list is the ephemeral actual presence/absence of the Need on the pawn.

Yes, if your registry says “Pawn X has the dynamic need, and CurLevel was 0.42 last time we updated,” but you find that the game’s needs list no longer has it, you can re-insert it. This is basically:
	•	On game load: scan through each pawn, cross-reference with your pawnNeedsMap, re-add your dynamic needs.
	•	Every so often (or every tick if you like), confirm the game’s needs matches your pawnNeedsMap. If something was forcibly removed, re-add it.

Race Conditions?

RimWorld is fundamentally single-threaded in normal gameplay. So you don’t typically have to worry about multi-thread concurrency. The biggest “race” is just in which order do various Harmony patches or mod code run within the same game tick? Usually you can structure your patch to run last in the “AddOrRemoveNeedsAsAppropriate” chain. Or do a small GameComponent that runs after the base logic. The worst that might happen is a single frame where the need is absent, but that typically doesn’t matter unless you do something super time-sensitive.

You likely don’t need a try/catch–style atomic operation, so long as you do a “final pass” after the base game’s logic. For example, a postfix patch or your own “Tick()” method that says:

if (MindMattersRegistry.SaysPawnHasFreshFruitNeed(pawn) && !pawn.needs.TryGetNeed<FreshFruitNeed>())
{
    ReAddDynamicNeed(pawn);
}

Done. No big concurrency issues.

5. Potential Cons or Caveats
	1.	Slight overhead:
	•	You do a check every so often to re-sync your registry with the game’s actual list of needs. However, the game doesn’t do AddOrRemoveNeedsAsAppropriate() all that often (mostly at spawn, load, or major transitions). So overhead is minimal.
	2.	Modders might be confused about how to incorporate your dynamic system if they define NeedDef in XML. They must realize that “the base game’s ShouldHaveNeed might not govern it, MindMatters does.” So you might want to provide doc or a template.
	3.	If you rely heavily on vanilla NeedDef fields (like major, baseLevel, etc.) for some of your logic, be sure to keep them in sync with your “real” data in your registry. Usually that’s straightforward: you set them once, they rarely change dynamically.

6. Summation of the Proposed Flow
	•	Step 1: At load or some central point, you create your dynamic needs’ NeedDefs but set them up so that the base game rarely (or never) qualifies a pawn for them by default. Or if you do want the base game to add them normally, you patch ShouldHaveNeed to keep them from being removed.
	•	Step 2: Your mod keeps a pawnNeedsMap or “NeedsMgr” as the authoritative: which DynamicNeeds should each pawn have right now?
	•	Step 3: Whenever a pawn spawns or the game loads, you do a “sync pass”: for each dynamic need your registry says the pawn should have, check if the game’s needs list is missing it. If it’s missing, you add it with the last known level.
	•	Step 4: If you want to let the game remove it automatically, let it—but then in your “sync pass,” see that your registry says “the pawn should still have it,” so you re-add it. Or intercept the game’s removal logic directly and block it if your registry says “still needed.”

That’s effectively all you need. You end up with a robust system that:
	•	Keeps your dynamic mod logic in a single code location (the registry / NeedsMgr).
	•	Allows any other mod to see your needs in DefDatabase<NeedDef> or on the actual Pawn.needs list, so it’s not “hidden.”
	•	Prevents vanilla from “fighting” your logic, because you either intercept removal or re-add the need anyway.

That’s basically it

You’ve got the right strategy: keep a separate “master record,” let the game do what it does, and simply override or revert any changes that conflict with your intended dynamic system. It’s how many complex RimWorld mods (that introduce ephemeral or advanced mechanics) handle the tension between “We need the base game to be aware of it” and “We don’t want the base game to be in charge.”

In other words, Yes, store your data in the registry plus in Pawn.needs. Patch or sync as needed to ensure correctness. Keep the extra overhead to a minimum by hooking at known points (spawn, load, a single event each in-game day, etc.). You don’t necessarily have to patch every single code path. Just find the main place(s) where RimWorld tries to remove or auto-add a need, and override or handle it. Everything else—UI drawing, saving, etc.—will be satisfied by the normal NeedDef + Need objects in the game’s needs list.