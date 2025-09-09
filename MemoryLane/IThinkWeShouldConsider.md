I think we should consider what you wrote above in terms of code as canon. It's just as I would conceive of it.

A friend asked my news, and here's what came out. Why don't we go ahead and parse how I've explained this to them, and see if we could, using what we've written above, create the base classes for what I'm describing. My description "added" a little because I'm reminding them of a long ago version of this concept for a game.

"Fuck it, I am beginning game development effort.

The not so silly these days title: The Game Of Love, Friendship, And Honor, or, Villainy.

Concept: The last game of the genre of tile-based games in real time. :) Nethack/Rogue-like freedom of action (verbology) in an easily extensible and moddable 2-d for action 3-d for levels of space, world with facing for objects (pawns). 

Technology: Godot as game engine, lightweight Kernel built around it (a la LPC in DGD) to avoid Godot problems such as node sprawl or giving the programmer, ie. modder, too much leeway to modify what is exposed to them (A la Ludeon).

World: per PZ, as to real of objects and much of classification. The "use" of an object isn't focal as it is to RimWorld or Prison Labor. Instead, like PZ, the object (Thing) has competences, which to Player are verbs.

Room-thinking isn't central as it is in RimWorld because of Unity. Instead, things simply have visibility which degrades in reality with distance, and the characteristics of the pawn as object or observer.

Thing-thinking is central, because things themselves determine what they can or cannot do, and thus where they are, where they can occur, etc.

Things are discrete. Pawns don't know everything. What they can do largely depends on what they are aware of and know (with some sacrifices made so that the player can get them to put out fires, stop invaders, thieves and so on...)

There isn't a strict map clutter problem because shit's always happening where you can't see it as the world isn't limited to your map. It's continuous, and is largely made up as you go along (per Nethack)

This is all simply applied philosophy which is missing from the design of all the games I mention, since, they are technically all improvements on each other.
NOUVEAU
[20:48]
Everyone around me is asking to work on a game, or, hinting at it.

I have an endless stream of mods I create which if you put them all together, makes pawns behave as they would in the game I name above.

Everything I create is aimed at taking away some degree of player control of something, while giving them the power to do something else.

When I give pawns Experiences, I'm essentially creating a new category of complex ThoughtMemory that just doesn't exist in the game, because thoughts have a Tracker architecture."

then:

"Trackers for Things give them watchers, essentially and watchers of things are expensive in terms of compute.

RimWorld makes it worse because things have both Trackers and their own Ticks, which aren't coordinated.

Both of these are supposed to be combined into a single thing called Heartbeat.

Anything that exists, must be an organism in the very strict sense: it is self-contained. When it lives (if it lives) this drives its activities, except where Player intervenes. Otherwise its state is one of perpetual decay, this being true even of pieces of rock (true of LPC, Nethack, etc.).

If something isn't there it continues to do its thing subject to the amount of memory allowed for this, and the possible range of interaction of the player.

Beyond this, things begin to decay starting with things on the ground. When things die, collapse or decay they become things on the ground, and eventually disappear.
This is stuff that's true of the world we live in, but, if you choose your concepts very carefully you can do rudimentary physics modeling.

It's how everything was done in MUDs and is it beautifully CPU efficient because things take care of themselves in terms of no longer being there, while doing something while they are.

This ensures that the more distant a player is from point X, the more different it will be when they return to it from Y.

Of course, this doesn't affect map-like distances, something like 1200 cells. Beyond that, if it's the woods, well it's the woods until you go n (distance to X) cells and begin to approach X.

Big immovable stuff is still there. Agents you've encountered there, are still there unless they've moved on or, are dead. Virtually anything else, might change depending on the distance to X and the elapsed time."

To give you background, I write mods like "Mind Matters" which try to compose a psychology (experientially-based) and create behaviors, with non-linear pawn evolution. I also create apparel and weapons, but, I can't resist giving these things qualities which the player might have to imagine.

I introduce _Training Corsets_, create ConstraintNeed, FormalityNeed and two different kinds of fainting spells. Pawns with the right Traits get assigned one or both of these Needs. I quickly realize, that some people are going to force their pawns to wear these anyway, so I add the CompFreedomSuppressor, which satisfies both Constraint and Formality. If we take a world view, upper class garments "upper class" garments restrain and prohibit movement.

This of course, produces the production need for corsets, which are time and resource consuming, or, expensive compared to a leather vest, while the Quality of the corset determines the frequency of fainting.

Naturally, I must consider people _not_ liking to wear this, and the associated mental breaks or behaviors, which feedback with Mind Matters, of course.

So I've introduced the category of Oppressive Apparel, essentially and because it's expensive and labor-intensive, the game naturally picks it up and equips incoming pawns with it, which is precious when they are friendlies and have a low-quality corset. (snickers). And your faction specializes in producing silk corsets. Ahem.

And then since Apparel can Oppress, I had to conceive of the Trait of _Runaway_ which actually, doesn't exist in RimWorld.

This is a different version of the _Give Up Quit_ incident which can affect any pawn except slaves (if not modded to allow this).

A Runaway is always a World Pawn, but wrapped a little differently.