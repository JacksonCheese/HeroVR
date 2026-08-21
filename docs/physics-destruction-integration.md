# Physics, Destruction, Minion, and Boss Integration

This milestone is gameplay-owned. It provides reusable components and placeholder
assets; it does not require environment code or final art. The validation scene is
`Assets/Scenes/Gameplay/PhysicsDestructionSandbox.unity`. Production arenas remain
environment-owned.

## Controls in the gameplay sandbox

- Desktop: hold `E` while aiming at a nearby valid body or prop, then release `E`
  to throw it along the camera aim direction.
- Base XR player: either grip can grab a nearby valid target.
- Thor XR: left grip grabs. Right grip remains reserved for Mjolnir hold/throw.

The input adapters only request grabs and releases. `PhysicsGrabInteractor` owns
the joint and velocity handoff; the target owns whether it may be grabbed.

## Humanoid ragdoll setup

Add these components at the logical character root:

- `Damageable`
- a locomotion `Rigidbody`
- `RagdollController`
- `GrabbableCharacter` when the character may be grabbed
- the AI/navigation driver, preferably implementing `IControlSuspendable`

For a skinned humanoid, create normal Rigidbody/joint/collider ragdoll bones and
assign them to `RagdollController`, or leave the arrays empty to let it discover
child Rigidbodies. Author the normal animated pose with ragdoll bone bodies
kinematic and their collision state as desired. The controller captures that state,
enables bone physics for full ragdoll, and restores the captured state on recovery
or respawn. A one-Rigidbody capsule uses the supported root-body fallback.

Heavy activation is based on `DamageInfo.ImpactStrength`, not hero identity.
Death can independently force ragdoll. While ragdolled, registered control systems
are suspended and NavMesh locomotion stops. Living enemies may recover after the
configured delay. Dead bodies settle after a configured timeout. The static active
ragdoll budget settles the oldest body when the per-controller limit is exceeded.

For explicit torso grabbing, add `CharacterGrabArea` to the torso collider and
optionally assign its preferred Rigidbody. Turn off root-collider grabbing on
`GrabbableCharacter` when only authored areas should be valid. Do not add
`GrabbableCharacter` to giant bosses unless the design explicitly allows it.

## Throwable environment prefab contract

An environment prop that can be picked up and thrown needs this root contract:

```text
PropRoot
├── Rigidbody
├── one or more non-trigger Colliders
├── GrabbableObject
├── ThrowableObject
└── ImpactDamageDealer
```

Configuration responsibilities:

- Set realistic gameplay mass on `Rigidbody`. Mass directly affects momentum and
  therefore damage. A hand-sized prop should not use vehicle-scale mass.
- Use continuous collision detection for fast, important props.
- `GrabbableObject` is the permission/ownership gate. Only one interactor can own
  it at a time.
- `ThrowableObject` scales and caps inherited release velocity and passes the
  player/hero instigator into impact attribution.
- `ImpactDamageDealer` controls minimum damaging speed, minimum momentum, damage
  per momentum, damage cap, knockback scaling/cap, severity scale, and repeat-hit
  cooldown.
- Do not add hero-specific checks. Collision layers may narrow valid targets, but
  team/self rules must continue to use ownership/instigator data.

The current stable approximation is:

```text
momentum = rigidbody mass × relative collision speed
impact strength = max(momentum, collision impulse) × strength scale
damage = clamp((momentum - minimum momentum) × damage scale, 0, damage cap)
```

Both the minimum speed and minimum momentum gates must pass. This is intentionally
tunable rather than a physically exact simulation.

## Breakable structure contract

The environment developer can author visuals while gameplay owns the state logic.
Use this hierarchy:

```text
StructureRoot
├── StructuralDamageReceiver
├── DestructibleStructure
├── blocking collider(s)
├── IntactState
├── DamagedState
├── BrokenState
└── Debris (optional, bounded pieces with DebrisLifecycle)
```

Requirements:

- Put `StructuralDamageReceiver` and `DestructibleStructure` on the same root.
- Assign all colliders that block traversal to `blockingColliders`. They are
  disabled exactly once when the structure reaches `Broken`.
- Intact, damaged, and broken children are visual/state variants. Only the active
  state is enabled. The broken state must leave the intended passage unobstructed.
- Avoid putting an unlisted solid collider across the opening; gameplay cannot
  make the opening traversable if an environment collider remains.
- Optional debris should be a small authored set, not runtime mesh fracturing.
  Put `DebrisLifecycle` on each debris group, start it inactive/kinematic, and keep
  its lifetime and global active limit conservative for Quest 2.
- Tune structure armor, minimum impact, type multipliers, and impact scaling on
  `StructuralDamageReceiver`. Ordinary `Physical` damage is deliberately weak;
  `HeavyPhysical` and `Structural` damage are effective. `Energy` is separately
  configurable.

Attacks route through `CombatHitResolver`. The nearest receiver level owns a hit,
which also lets boss hit-region colliders modify damage before forwarding it to
shared boss health.

`Assets/Prefabs/Gameplay/Physics/BreakableTestWall.prefab` is the reference. Its
root blocking collider is disabled at `Broken`, so the central opening becomes
traversable. A thrown ragdoll uses the same `ImpactDamageDealer` path as a thrown
prop; there is no throw-through-wall special case.

## Minion spawn contract

Place gameplay-owned `MinionSpawnPoint` components in the production arena where
waves may appear. Each point may specify:

- enemy prefab override
- enemy definition override
- arena team metadata
- spawn radius
- wave group index

Then reference those points from one `MinionSpawnController`. Configure a default
enemy prefab/definition, maximum active count, and defeated cleanup delay. The
controller performs no scene-name or GameObject-name lookup. Boss phases request a
count and wave group from this same controller.

The reusable minion composition is:

```text
EnemyDefinition
+ GenericEnemyBrain (configuration/composition)
+ TrainingBot (preserved melee/navigation driver for now)
+ Damageable
+ Rigidbody/NavMeshAgent
+ RagdollController
+ GrabbableCharacter
+ ImpactDamageDealer
```

`EnemyDefinition.AttackRole` is melee/ranged-ready. This milestone preserves the
working TrainingBot melee implementation; a later ranged driver can consume the
same definition without forking health, ragdoll, grabbing, or spawning.

## Boss spawn and encounter contract

Place a `BossSpawnPoint` at the intended boss root position and rotation. Assign a
boss prefab and `BossDefinition`. Reference it from a `BossEncounterController`
along with the arena's `MinionSpawnController`. Do not hard-code coordinates into
the boss prefab or controller.

Boss prefab composition:

```text
BossRoot
├── Damageable
├── BossController
├── optional kinematic Rigidbody
├── Head collider + BossHitRegion
├── Torso collider + BossHitRegion
└── Limb collider(s) + BossHitRegion
```

Each region has a configurable multiplier and forwards to the boss's single
`Damageable`. `BossDefinition` supplies maximum health, physical scale, attack
slots, telegraph/cooldown data, and phase thresholds. Phase entries may request a
minion count and wave group. The placeholder boss proves a telegraphed stomp/AOE;
projectile slots are data-ready but need a future boss-specific projectile prefab.

Bosses do not receive `GrabbableCharacter` or normal minion ragdoll by default.
That keeps ordinary heavy hits from toppling or grabbing a giant. A separate death
ragdoll may be composed later if its physics and Quest budget are validated.

## Quest 2 limits

- Default maximum active minions: configurable, reference sandbox uses 6.
- Default maximum active ragdolls: 8; the oldest is settled first.
- Dead ragdolls settle after their configured corpse delay.
- Defeated spawned minions are destroyed after the configured cleanup delay.
- Debris uses authored pieces, an active lifetime, and a global count limit.
- Grab overlap queries use fixed non-allocating buffers and occur on grip press,
  not every frame.
- Impact calculation occurs on collisions only. No runtime fracturing, voxel work,
  or per-frame global searches are used.

These are sane prototype bounds, not final budgets. Profile sustained encounters
on Quest 2 before increasing them.

## Current limitations

- The placeholder minion uses the root-Rigidbody ragdoll fallback. Production
  humanoid art still needs an authored bone ragdoll and recovery pose blending.
- Desktop throw velocity is an explicit forward debug throw, not mouse-motion
  estimation.
- Boss navigation and final animation are not part of this milestone. The current
  boss proves targeting, regions, phases, summons, telegraph, AOE damage, death,
  and reset.
- Runtime pooling is not yet used for minions/debris. Counts and cleanup are
  bounded; pooling should follow measured Quest allocations and spawn hitches.
