I think we've got it. Btw, you added back in the base(Pawn null) or whatever it was in your example. :) I took it back out.

With this correction we no longer see the exception when guests come, so I think we're on track.

I'm feeling like mischief, since my evening has gone the wrong way. Let's make it so these visiting pawns would 0.80 have _some_ DynamicNeed based on their sampling of Traits:

#1
Sasha, House Servant

Childhood Pyromaniac, Adulthood House servant

Traits: Pyromaniac, Trigger-happy, Misandrist

Incapable of Intellectual, Firefighting

#2
Stench, Paramedic

Shelter child, Paramedic

Traits: Self-Centered, Eccentric, Pretty, (genetic) Psychically deaf, (genetic) Abrasive

#3
Marina, Veterinarian

Cult child, Veterinarian

Traits: Lazy

Incapable of Intellectual

#4
Ivy, Drifter

Vatgrown soldier, Drifter

Traits: Iron Stomach

Incapable of Caring, Social

#5
Scarlet, Furniture Builder

Wreckage Explorer, Furniture builder

Traits: Industrious, Kleptomaniac

#6
Vivis, Hunter

Sickly Child, Hunter

Traits: Slow learner, Delicate, Impervious

#7
King (17 years old), teenage bounty hunter

Pyromaniac, (no adult trait or backstory gained yet)

Traits: Pyromaniac, Trigger-happy, Body purist

Incapable of Firefighting

Let's use these 7 pawns as our PawnDataSetA to analyze and treat as representative for the moment.

We also have access to their Ideo:

Rural Way  (style rural)
Memes: Rancher
Slavery: Disapproved
Cannibalism: Horrible
Organ use: No harvest
Diversity of thought: Neutral
Physical love: Spouse only (mild)
Marriage name: Usually man's (Rancher tends to imply male dominance by default clothing requirements being more covering for women and marriage name == man's)

This is about as generic as it gets in terms of auto generated Ideoligions. It is the lambda Ideololigion, by RimWorld terms.

These aren't a particularly "interesting" set of pawns either to consider for designing some new DynamicNeeds except that there are two Pyromaniacs and a Kleptomaniac who is Industrious. Kleptomaniac isn't a Core Trait but comes from Vanilla Expanded as I recall.

Impervious, Self-Centered are Mind Matters Traits. Impervious isn't really implemented yet but should make the pawn immune to situational stressors, Trauma, Anxiety. The Impervious pawn shouldn't care about _minor_ inconveniences, only remarking them.

Self-Centered is a first step on implementing a proper Narcissist Trait. From a Mind Matters optic, Self-Centered is an experiential phenomenon: the pawn notices what affects them, rather than being unconcerned with what affects others, and should prioritize based on their own Needs before anyone elses, because that's what they notice first. Narcissist will remove the concern for the Needs of others.

We've caught something we consider a "Trait Error" from the perspective of Mind Matters: a lazy veterinarian. There are a number of "this shouldn't happen" trait combinations that we mean to add a tool to correct. I've never encountered a lazy veterinarian in my entire life, but because so many traits preclude others this is an alarmingly common combination. About 1/6 of veterianians get the Lazy trait in the game, akin to Doctors, and if they aren't Baseliners it will appproach half due to imposed Needs creating additional filtering, which never filters out Lazy.

This data set and Ideo also reminds me of our two other we need to build as additional MM Tools:

- Pawnalyzer (or some other funny name) - in single argument form, returns what sticks out about a pawn in more or less the same way I showed you to create the data set above from the guest pawns.

In multi argument form: Pawnalyzer(Pawn pawnA, Pawn pawnB) or (Pawn pawn, ThingDef concreteThing), ... 

Pawnalyzer has convenience methods: booleans IsLambda, HasSixOutOfSeven, CanCarePawn(Pawn pawn), CanCareKind(PawnKind kind), MakesTrouble(), MakesTroubleFor(Pawn pawn), MakesTroubleForKind, MakesTroubleWhen(// some condition that can be tested), EnforcesOrder, EnforcesNorms, List returners HasTriggers, HasPhobias, HasDislikes, HasQuirks, HasExperienced, ...

This sounds _big_ but it will avoid _tons_ of code if we can concentrate this in a single class as much as possible. Other modders will definitely use MM for this.

- Ideoscope - in single argument form:

- Return what isn't lambda. We can use our Ideoligion above as a guide for this.

- Two argument form IdeoScope.HasHarmonies(IdeoDef ideoA, IdeoDef ideoB), HasConflicts(IdeoDef ideoA, IdeoDef ideoB), AdustedCompat(IdeoDef ideoA, IdeoDef ideoB), ...

Hardly any mod code delves into what Ideoligions mean in terms of how pawns interact. We'd like to address this in Mind Matters by creating opportunties for deepening relationships between pawns due to commonalities, or, ensuring chaos if there's good reason these groups of people can't hang out in a saloon together while heavily inebriated and generally armed.

Consider two, we'll be adding a critical Precept, or Precept set: Personal arms, being armed, etc.

Do you think we should maybe define the Precepts will use before we get into the meat of the rest of this?