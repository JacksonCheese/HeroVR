# HeroVR Project Guide

This file applies to the entire repository. It is the persistent working agreement for Codex and other coding agents contributing to HeroVR.

## Project Purpose

HeroVR is a private Unity VR superhero arena fighting game for the project owner and friends.

Current product goals:

- Target Meta Quest 2 first.
- Preserve compatibility with Quest 3 as the project develops.
- Support 1v1 play first and expand to 2v2 later.
- Build heroes with distinct, reusable powers.
- Stabilize local gameplay before adding multiplayer.
- Eventually support private multiplayer lobbies.

This is a private game, not currently a public live-service product. Favor a reliable, enjoyable small-group experience over unnecessary platform or backend complexity.

## Current Technology

- Unity 6
- The repository currently uses Unity's built-in render pipeline.
- Universal Render Pipeline remains a possible later migration, but do not migrate or change rendering settings without explicit coordination.
- OpenXR
- XR Interaction Toolkit
- AI Navigation (NavMesh path source for physics-driven AI)
- Unity Input System
- Meta Quest 2 is the performance and interaction baseline.

Do not introduce a second input, XR, rendering, physics, or networking framework without explaining the need and receiving approval.

## Two-Developer Ownership

HeroVR development is split between a gameplay developer and an environment developer working on separate Git branches. Keep their asset ownership boundaries explicit so the branches can merge with minimal Unity scene and prefab conflicts.

The gameplay developer owns:

- gameplay scripts and gameplay tests
- player, character, ability, enemy, match, and other gameplay prefabs
- generic arena/gameplay integration components such as spawn-point metadata
- gameplay-only sandbox scenes
- future XR player, hand, and gameplay prefabs

The environment developer owns:

- production arena scenes and arena prefabs
- environment geometry, buildings, platforms, and props
- environment materials, lighting, and environmental art
- environmental collision layout and arena-specific placement

Production arenas should use small gameplay-owned integration components rather than contain arena-specific implementations of player, combat, ability, AI, or match logic. Gameplay must discover these components without depending on scene names, specifically named GameObjects, or a particular hero controller.

Environment work may place and configure gameplay-owned spawn-point and integration components. Changes to the behavior or serialized contract of those components remain gameplay-owned and should be coordinated before environment scenes depend on them.

The following are shared, conflict-prone areas and require coordination before modification:

- `ProjectSettings`, including tags, layers, build scenes, graphics, quality, Android, and XR settings
- `Packages/manifest.json` and `Packages/packages-lock.json`
- production arena scenes and shared prefabs
- asset moves or renames that change Unity `.meta` paths

Do not require the environment developer to edit this guide on their branch merely to consume the generic gameplay contract.

## Development Roadmap

Work incrementally in this order unless the user explicitly reprioritizes it:

1. Stabilize the desktop prototype v0.1.
2. Improve combat behavior and the training enemy.
3. Add a reusable hero ability system.
4. Add an XR player rig.
5. Map Quest controllers and hands to the same ability system.
6. Test and profile on Quest 2.
7. Create the first complete superhero-style character.
8. Add multiplayer 1v1.
9. Add private lobbies.
10. Expand to 2v2.

Do not pull later milestones forward when doing so would destabilize the current milestone. In particular, do not add networking abstractions or XR-specific behavior throughout the desktop prototype before the local gameplay loop is stable.

## Prototype v0.1 Scope

The desktop prototype should provide:

- WASD movement
- Mouse camera control
- Super jump
- Dash
- Melee punch
- Energy projectile
- Radial super smash
- Health and death
- Enemy AI
- Knockback
- Respawning
- Physics arena objects

Treat this list as the acceptance target for the current milestone. Prefer completing and verifying this loop over adding new powers, presentation systems, or content.

## Architectural Direction

The eventual runtime architecture must keep these responsibilities separate:

1. Character abilities and their gameplay rules
2. Combat, damage, health, death, knockback, and teams
3. Player input and input-device mapping
4. XR rig, tracking, hands, and controller integration
5. Networking, ownership, authority, replication, and lobbies

The same gameplay ability should be activatable by desktop input, Quest controllers, AI, and eventually network commands. An ability must not need to know which physical input device activated it.

Preferred dependency direction:

```text
Desktop input ─┐
XR input ──────┼──> character/ability commands ──> abilities ──> combat
AI input ──────┘

Networking coordinates authoritative commands and replicated results; it should not own hero design rules.
```

Keep Unity-facing adapters thin where practical. Put reusable gameplay rules behind explicit methods and data rather than reading keyboard, mouse, or XR controls directly inside every ability.

Do not over-engineer this architecture before the prototype proves what it needs. Add interfaces, base classes, ScriptableObjects, event channels, or dependency-injection mechanisms only when they solve a concrete reuse, testing, configuration, or ownership problem.

## Existing Systems to Preserve

The current custom code is organized under `Assets/Scripts`:

- `Combat/Damageable.cs` is the working health foundation and has already been tested successfully.
- `Combat/PunchHitbox.cs` contains velocity-based physical punch behavior.
- `Combat/RespawnOnDeath.cs` is the current reusable respawn component.
- `Combat/DamageTester.cs` is temporary diagnostic code.
- `Abilities/EnergyProjectile.cs` contains projectile collision, damage, and knockback behavior.
- `Abilities/ProjectileCaster.cs` is the current action-to-projectile bridge.
- `Abilities/IAimProvider.cs` and `Abilities/TransformAimProvider.cs` provide a
  device-independent world-space origin and direction to aimed abilities.
- `Abilities/DashAbility.cs` performs collision-aware travel through the owning
  `CharacterController` over a configurable duration; do not restore the old
  single-frame displacement.
- `Abilities/LightningAbility.cs` is the reusable aimed ray/beam damage ability.
- `Abilities/HeroAbilityLoadout.cs` is the shared ability command surface for desktop and XR input.
- `Movement/DesktopCharacterMotor.cs` owns desktop locomotion without owning keyboard input.
- `Prototype/DesktopHeroController.cs` is the temporary desktop vertical slice.
- `Prototype/TrainingBot.cs` is the initial enemy implementation.
- `Prototype/PrototypeArenaBootstrap.cs` constructs the current prototype arena at runtime.
- `XR/XRCharacterMotor.cs` owns head-relative XR locomotion and comfort snap turning.
- `XR/XRHeroInputAdapter.cs` maps Quest/OpenXR controls into the shared locomotion and ability APIs.
- `XR/TrackedHandPhysicsFollower.cs` drives kinematic punch proxies and supplies tracked velocity to `PunchHitbox`.
- `XR/XRWeaponInputAdapter.cs` maps grip release and recall input to a reusable
  weapon without implementing weapon or damage rules.
- `Input/DesktopWeaponInputAdapter.cs` maps desktop throw/recall actions and an
  `IAimProvider` direction to that same reusable weapon state machine.
- `Input/DesktopThorFlightDebugAdapter.cs` supplies editor/development-only
  synthetic Mjolnir motion without coupling Thor flight rules to keyboard input.
- `Weapons/RecallableWeapon.cs` owns the Held/Thrown/Recalling state machine,
  physical throw, visible return, hand attachment, and out-of-world failsafe.
- `Weapons/TransformWeaponMotionSource.cs` produces smoothed, device-independent
  held-weapon motion samples from a tracked transform.
- `Gameplay/GameplayMatchBootstrap.cs` spawns either desktop or XR player prefabs from generic arena spawn points.
- `Heroes/HeroDefinition.cs` is the data source for hero identity, combat tuning, ability names, and ultimate-resource rules.
- `Heroes/HeroProfile.cs` applies one definition to the existing reusable ability and combat components.
- `Heroes/HeroUltimateCharge.cs` implements the optional resource gate used by charged ultimate abilities.
- `Heroes/HeroStatusDisplay.cs` is the lightweight XR status presenter; it does not own gameplay state.
- `Combat/RagdollController.cs` reacts to generic impact strength/death and owns bounded ragdoll recovery/settling without owning AI behavior.
- `Interaction/PhysicsGrabInteractor.cs` is the device-independent one-owner grab joint; `GrabbableCharacter` and `GrabbableObject` own target permission and release behavior.
- `Combat/ImpactDamageDealer.cs` converts tunable mass/momentum collisions into attributed `DamageInfo` hits.
- `Destruction/StructuralDamageReceiver.cs` and `DestructibleStructure.cs` own structural health and Intact/Damaged/Broken state transitions.
- `Enemies/GenericEnemyBrain.cs` applies `EnemyDefinition` data while preserving `TrainingBot` as the current melee/navigation driver.
- `Enemies/MinionSpawnController.cs` creates bounded groups from generic `MinionSpawnPoint` metadata.
- `Bosses/BossController.cs`, `BossDefinition.cs`, and `BossHitRegion.cs` provide the placeholder giant-boss phase, region, attack, summon, death, and reset contracts.
- `Heroes/ThorHammerFlight.cs` interprets held Mjolnir spin and directional motion
  into collision-aware launch, momentum, and hover modifiers.
- `Heroes/ThorHammerFlightSettings.cs` is Thor's shared desktop/XR flight tuning
  asset contract.

The gameplay-owned validation scenes are `Assets/Scenes/Gameplay/GameplaySandbox.unity`
and `Assets/Scenes/Gameplay/XRGameplaySandbox.unity`. The gameplay-owned physics
and boss validation scene is `Assets/Scenes/Gameplay/PhysicsDestructionSandbox.unity`.
They are not production arenas.

The first configured hero is **Kinetic Vanguard**, defined by
`Assets/Heroes/KineticVanguard/KineticVanguard.asset`. Both desktop and XR player
prefabs reference that definition. Kinetic Vanguard gains Momentum from actual
damage dealt and damage taken, then consumes a full meter to activate Kinetic Nova.
Future heroes should use their own definition and composed loadout rather than fork
`Damageable`, the input adapters, or the locomotion components.

The first substantial XR hero is **Thor**, defined by
`Assets/Heroes/Thor/Thor.asset` and assembled in
`Assets/Prefabs/Characters/ThorXRPlayer.prefab`. Thor composes the shared movement,
damage, loadout, lightning, velocity-melee, throwable-weapon, and recall systems.
`Assets/Prefabs/Characters/ThorDesktopPlayer.prefab` uses the same Thor definition,
abilities, weapon, damage rules, and match bootstrap contract with keyboard/mouse
input adapters. `DesktopHeroController` must preserve a prefab's configured loadout
rather than replacing non-projectile secondary abilities during `Awake`.
`Assets/Scenes/Arenas/Arena_ThorVRTest.unity` is the gameplay-owned integrated
VR playtest; `Assets/Scenes/Arenas/Arena_ThorDesktopTest.unity` is its desktop
counterpart. Both derive from the production graybox; the environment-owned
`Arena_Graybox_01.unity` remains unchanged.

Thor hammer flight consumes `IWeaponMotionSource`; it must not poll a Quest or
desktop device directly. XR motion comes from the right controller transform while
right grip remains held. Desktop Editor/Development testing holds F for synthetic
spin and taps G for a launch pulse. Both player motors implement the additive
`IFlightMovementReceiver` contract, but other heroes receive no flight behavior
unless they explicitly compose a flight interpreter.

`TrainingBot` uses `NavMeshAgent` only to obtain a throttled steering target.
`NavMeshAgent.updatePosition`, `updateRotation`, and `updateUpAxis` remain disabled;
the bot's Rigidbody and `AddForce` continue to own movement so physics impulses are
not overwritten. When no baked NavMesh is present, the bot deliberately falls back
to its original direct steering so `GameplaySandbox` remains usable. The prefab's
agent starts disabled and `TrainingBot` enables it only after finding compatible
baked data beneath the spawn; preserve this to avoid unbound-agent warnings. Production
arenas must provide compatible baked NavMesh data before obstacle routing will work.

Do not rebuild, delete, rename, or replace a working system merely to impose a preferred pattern. Before materially replacing one:

1. Explain the concrete limitation or bug.
2. State what behavior must be preserved.
3. Prefer a small extraction, adapter, or migration.
4. Verify the replacement before removing the old path.
5. Ask for direction when the tradeoff changes gameplay or project scope.

Temporary prototype code may remain intentionally coupled while v0.1 is being stabilized. Separate it gradually at tested seams.

## Combat and Ability Conventions

- `Damageable` (or its deliberately evolved successor) remains the single source of truth for health and death state on a combatant.
- Do not create parallel health implementations for the desktop player, XR player, AI, or network player.
- Route damage through a consistent combat API. As requirements grow, include source/owner, instigator, team, hit point, direction, damage type, and knockback data in a shared hit description rather than expanding unrelated one-off methods.
- `DamageInfo.ImpactStrength` and `Damageable.ImpactReceived` are lightweight,
  presentation-agnostic feedback hooks. Sound, haptics, VFX, and reactions may
  listen to them but must not become responsible for authoritative damage.
- An area attack must damage each logical target at most once per activation, even when the target has multiple colliders.
- Projectiles and attacks must track their owner or instigator so they can apply self-hit and team rules consistently.
- Decide self-damage and friendly-fire behavior explicitly; do not let collider layout determine it accidentally.
- Cooldowns and activation state belong to reusable ability gameplay, not exclusively to a particular keyboard or controller binding.
- Avoid making visual effects, sound, haptics, or UI responsible for applying authoritative gameplay results.
- Keep physics work in the appropriate Unity loop. Apply Rigidbody motion/forces consistently and avoid frame-rate-dependent physics.

Design local combat so it can later become server-authoritative, but do not add a networking package or speculative replication code until the multiplayer milestone is approved.

## Input and XR Conventions

- Use the Unity Input System for new input code. Do not add new uses of legacy `UnityEngine.Input`.
- Input components translate device state into character or ability commands; they should not implement damage rules.
- Desktop and XR input should call the same public locomotion and ability APIs.
- Do not simulate tracked hands by moving authoritative combat state from render-only transforms without accounting for physics timing and velocity.
- Keep desktop prototype play possible while XR support is added, unless the user explicitly ends desktop support.
- XR initialization should be deliberate per build target. Desktop prototype testing must not require an active headset or desktop OpenXR runtime.
- Quest controller profiles and hand tracking are separate interaction paths; do not assume hand tracking is required for the first Quest milestone.
- The shared XR player bindings use left stick movement, right stick snap turn,
  right A jump, left stick-click dash, right trigger projectile, and right
  stick-click ultimate. The right controller's OpenXR pointer pose—not its grip
  pose—is the aimed-ability direction. Physical hand velocity activates punch
  damage through `PunchHitbox` rather than a button binding.
- Thor uses right trigger for lightning, right grip press/release to throw
  Mjolnir, right B to recall it, and retains the shared movement/jump/dash/
  ultimate bindings. The held hammer follows the grip pose while lightning uses
  the independent pointer pose.
- Desktop Thor retains standard WASD/mouse, Space jump, Shift dash, left-click
  melee, right-click lightning, and E ultimate controls. Q throws Mjolnir along
  the camera aim provider and R recalls it. In Editor/Development builds, hold F
  to simulate hammer spin and tap G after charging to launch along camera aim.
- Android OpenXR has Oculus Touch Controller Profile and Meta Quest Support
  enabled. Preserve both settings as well as the enabled Standalone Oculus Touch
  profile and the Windows D3D11 PCVR configuration.

## Quest 2 Performance Baseline

Quest 2 is the minimum supported device and performance budget. Quest 3 improvements must not silently make Quest 2 unusable.

- Profile on device; Editor performance is not representative.
- Favor stable frame timing and comfort over visual complexity.
- Use URP configurations intended for mobile VR.
- Avoid per-frame managed allocations in gameplay loops.
- Cache component references used frequently.
- Pool frequently spawned projectiles, effects, and temporary combat objects once prototype behavior is stable.
- Avoid creating materials or primitives repeatedly during normal gameplay in production paths.
- Keep shaders, realtime lights, transparent effects, particles, shadows, physics bodies, and draw calls conservative.
- Treat render scale, anti-aliasing, foveated rendering, batching, and shader variants as measured configuration choices.
- Avoid expensive global searches during gameplay. Startup-only prototype searches are acceptable when documented and small.
- Test thermal behavior and sustained performance, not only short sessions.

Do not prematurely micro-optimize clear prototype code. Fix measured problems and obvious high-frequency allocations first.

## Unity Coding Conventions

- Place runtime C# under `Assets/Scripts` in a responsibility-based folder.
- Use the root namespace `HeroVR`, with responsibility namespaces such as `HeroVR.Combat`, `HeroVR.Abilities`, `HeroVR.Input`, `HeroVR.XR`, and `HeroVR.Networking`.
- Use one primary public type per C# file and match the file name to the type name.
- Prefer `private` serialized fields (`[SerializeField] private`) over mutable public fields for production code.
- Expose read-only state through properties where other systems need it.
- Use explicit access modifiers in production code.
- Validate required components with `RequireComponent`, `Awake`, or `OnValidate` as appropriate.
- Subscribe and unsubscribe event handlers symmetrically, normally in `OnEnable` and `OnDisable`.
- Avoid repeated `GetComponent`, scene searches, LINQ allocations, or collection creation inside `Update`/`FixedUpdate`.
- Use `Update` for input sampling and non-physics presentation; use `FixedUpdate` for Rigidbody-driven simulation.
- Use `Time.deltaTime` or `Time.fixedDeltaTime` for time-based behavior as appropriate.
- Prefer understandable names and focused components over abbreviated or overly generic abstractions.
- Add comments for intent, invariants, non-obvious physics choices, or platform constraints—not for restating the code.
- Keep diagnostic scripts and prototype-only UI clearly labeled and easy to exclude from production builds.
- Do not edit imported package/sample files unless the task specifically requires it.

Follow the APIs supported by the installed Unity version. For Unity 6 Rigidbody code, use the current velocity/damping APIs already present in the project unless package or platform compatibility requires otherwise.

## Assets, Scenes, and Repository Hygiene

- Preserve `.meta` files and their GUIDs when moving Unity assets.
- Move or rename assets together with their `.meta` files.
- Do not commit generated Unity directories such as `Library`, `Temp`, `Logs`, `obj`, or build output.
- Inspect `git status` before editing. Existing changes belong to the user and must not be discarded or overwritten.
- Never use destructive Git commands to clean the project without explicit approval.
- Prefer small, reviewable changes aligned with one milestone.
- Avoid hand-editing scene or prefab YAML when a safer Unity Editor workflow is available. If serialized YAML must be edited, make a minimal change and validate it in Unity.
- Do not silently change Project Settings, packages, render pipeline, XR providers, Android settings, or build scenes. Explain why each configuration change is necessary.
- Do not add third-party assets or packages unless they provide clear value for the active milestone and the user approves the dependency.

## Verification Expectations

For every gameplay change:

1. Confirm scripts compile without new errors.
2. Run the most relevant Edit Mode or Play Mode tests when available.
3. Exercise the affected behavior in the appropriate scene when practical.
4. Check the Unity Console/log for new exceptions and important warnings.
5. Report what was verified and what still requires manual Editor or headset testing.

Add focused tests as stable seams emerge. High-value early tests include:

- damage, healing, death, and reset behavior
- one death event per life
- ability cooldown behavior
- one area-hit result per target
- projectile owner/self-hit rules
- respawn state and position reset
- attack rejection while dead

For Quest changes, a successful Editor test is not sufficient. Clearly identify when an Android build, on-device check, controller check, tracking check, or performance capture remains outstanding.

Quest 2 on-device validation for the initial XR foundation was explicitly deferred by
the project owner. Continue development without treating it as a milestone blocker,
but keep the lack of device validation visible and fix regressions retroactively when
hardware testing becomes available.

## Working Style for Codex

- Inspect relevant code, scenes, packages, settings, logs, and repository state before proposing broad changes.
- Lead with the observed problem and the smallest useful next step.
- Preserve verified behavior while improving its implementation.
- Explain architectural migrations before performing them.
- If a request is diagnostic, report findings without implementing unrelated fixes.
- If a request authorizes implementation, complete and verify the requested increment without expanding into later roadmap stages.
- Call out assumptions, especially around combat rules, comfort, input mapping, multiplayer authority, and Quest performance.
- Leave concise handoff notes: files changed, behavior changed, verification performed, and remaining manual steps.

When uncertain, prioritize a stable and playable desktop v0.1, then evolve it toward shared desktop/XR abilities in measured steps.
