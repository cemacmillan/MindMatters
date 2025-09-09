
Development Plan: Mind Matters - Dynamic Needs

Stage 1: Core Functionality - “Basic Implementation”

(Status: Largely Complete)
Goal: Establish a system for managing dynamic needs and activating them for relevant pawns.
1.	Dynamic Needs Registry
•	Implement a registry to store and retrieve DynamicNeedProperties and associated metadata (DynamicNeedsBitmap).
•	Ensure the registry is populated from DefDatabase during game initialization.
•	Resolve any issues related to malformed or unexpected NeedDef names.
•	Graceful handling: Return DynamicNeedsBitmap.None with appropriate logging.
2.	Needs Manager (NeedsMgr)
•	Manage activation and deactivation of dynamic needs based on conditions.
•	Process dynamic needs for individual or all pawns (ProcessNeeds).
•	Implement proper handling for “queued” pawns to add needs upon colony entry or game load.
3.	Harmony Integration (ShouldHaveNeed Patch)
•	Implement a patch to manage when and how dynamic needs are applied.
•	Ensure compatibility with RimWorld’s existing AddOrRemoveNeedsAsAppropriate logic.
•	Add robust debugging to handle unexpected input (e.g., incomplete pawns or pawn kinds).
4.	Dynamic Need Filters
•	Implement individual filters (ShouldPawnHaveThisNeed) for dynamic needs.
•	Ensure proper handling for edge cases like guests, incomplete pawns, or KindDefs.

Stage 2: Refinement and Expanded Functionality - “Polish and Extend”

(Next Steps)
Goal: Improve robustness, polish debugging, and extend functionality to support all intended use cases.
1.	Guest Exclusions and Special Handling
•	Investigate and resolve issues with guests, prisoners, and other non-colonist pawns.
•	Ensure proper filtering for special cases (e.g., incomplete pawn data during generation).
•	Use debugging outputs to trace and refine these behaviors.
•	Implement optional settings to allow or exclude dynamic needs for guests.
2.	Pawn Integration Testing
•	Verify dynamic needs are correctly added, activated, and tracked for:
•	Colonists
•	Prisoners
•	Slaves
•	Temporary pawns (e.g., refugees, quest-related pawns)
•	Address quirks related to hidden or modded pawn data (e.g., Simple Personalities).
3.	Reflection Edge Cases
•	Identify and test edge cases for reflection-based need instantiation.
•	Ensure clean handling of modded needs or unexpected changes to NeedDef structures.
4.	Mod Settings Integration
•	Ensure dynamic needs respond to user-configured mod settings, including:
•	Enabling/disabling random needs.
•	Controlling prevalence of specific needs.
•	Options for player preference in surprise/random behaviors.

Stage 3: Final API and Modular Extensions - “Feature Ready”

(Dependent on API Fixes)
Goal: Ensure compatibility with future planned features, external mods, and modular extensions.
1.	Finalize API for Dynamic Needs
•	Test and confirm external mods can safely interact with dynamic needs via exposed API.
•	Validate external queries for prevalence, activation, and suppression of needs.
2.	Expanded Need Logic
•	Add modular or contextual logic for needs based on:
•	Environmental factors (e.g., scarcity of certain items).
•	Social or factional influences (e.g., trends, peer behaviors).
3.	Customizable Need Responses
•	Allow users to define custom conditions or responses for dynamic needs.
•	Add more nuanced state transitions (e.g., “latent,” “fulfilled,” “urgent”).

Stage 4: Player Feedback and Quality Assurance - “Final Polish”

Goal: Optimize the system for usability and player enjoyment.
1.	Debugging Outputs and Logs
•	Reduce or streamline debugging outputs for player builds.
•	Offer a developer mode for detailed logs and diagnostics.
2.	Visual Feedback in Game
•	Add tooltips or overlays to indicate when and why a dynamic need is active.
•	Include clear messaging for unexpected or hidden behaviors (e.g., a guest suddenly joining and gaining needs).
3.	Balance and Player Control
•	Balance prevalence and impact of dynamic needs to align with gameplay goals.
•	Offer settings to customize the “intensity” of dynamic needs for different playstyles.

Immediate Next Steps
1.	Finalize debugging cleanup for guest/pawn-kind distinction.
2.	Test the interaction between queued pawns and late-stage need activation.
3.	Begin implementing Stage 2 refinements, focusing on guest handling and edge-case testing.
