# Destruction & Boss Environment

Environment-side assets for destruction, throwables, and boss encounters. Everything here is
geometry, colliders, materials and bare placeholder transforms. No gameplay components are
included — attachment points are listed under [Integration](#integration).

## Breakable structures

`Assets/Prefabs/Environment/Breakable/`

| Prefab | Thickness | Debris chunks |
|---|---|---|
| `Wall_Breakable_Concrete.prefab` | 0.55m | 4 |
| `Wall_Breakable_Brick.prefab` | 0.45m | 5 |
| `Wall_Breakable_Interior.prefab` | 0.28m | 3 |

All three are 8m wide × 5m tall, with a 4.2m × 3.4m opening in the broken state.

### Hierarchy

```
Wall_Breakable_*            <- attach DestructibleStructure here
├── IntactState             (active)
│   ├── Visual
│   └── Collider
├── DamagedState            (inactive)
│   ├── Visual
│   ├── CrackDetail
│   └── Collider
└── BrokenState             (inactive)
    ├── Visual
    │   ├── Side_L, Side_R, Lintel
    │   └── BrokenEdges
    ├── Collider
    │   └── Side_L, Side_R, Lintel
    └── Debris
        └── Chunk_1 … Chunk_N
```

State roots are direct children of the prefab root with fixed names, so serialized references can
be assigned without searching for arbitrary child paths.

### Collider strategy

- **One state active at a time.** Each state owns its own collider, so nothing invisible is left
  behind after a break.
- **Broken collision is three boxes** — `Side_L`, `Side_R`, `Lintel` — not one box with a visual
  hole. A `BoxCollider` cannot have a hole; a single box is how a "broken" wall ends up still
  blocking the passage it just opened.
- **Primitives only.** No `MeshCollider` anywhere. Fast thrown objects tunnel through them and
  ragdolls snag on them.
- `BrokenEdges` are visual only, so ragged geometry around the gap cannot catch anything passing
  through.
- Wall thickness is deliberately generous for the speeds involved — a thin wall is what a hammer or
  thrown enemy tunnels through between physics ticks.

### Debris strategy

A few large chunks, not dozens of fragments. Dozens of rigidbody shards is the usual reason
destruction becomes unshippable on Quest.

Chunks ship **without Rigidbodies** — gameplay decides whether they ever simulate. Each has a
simple `BoxCollider`.

**Debris is placed outside the opening, not piled in it.** Rubble in the gap reads as realistic but
physically blocks the passage the break just created. This was caught by
`BrokenState_LeavesAPhysicallyTraversableOpening`, which sweeps a player-sized capsule through the
gap — it failed on the first build because chunks sat in the middle of the hole.

## Props

`Assets/Prefabs/Environment/Props/`

| Prefab | Tier | Collider | Suggested mass |
|---|---|---|---|
| `Prop_Light_SmallCrate` | Light | Box 0.45³ | 8–12 kg |
| `Prop_Light_TrashCan` | Light | Capsule r0.28 h0.85 | 10–15 kg |
| `Prop_Medium_Barrel` | Medium | Capsule r0.32 h1.05 | 40–60 kg |
| `Prop_Medium_Bench` | Medium | Box 1.85×0.85×0.62 | 60–90 kg |
| `Prop_Heavy_ConcreteChunk` | Heavy | Box 1.15×0.85×0.95 | 300–450 kg |
| `Prop_Heavy_Car` | Heavy | Box 2.05×1.65×4.35 | 1200–1600 kg |

Masses are **recommendations only** — nothing is hard-coded. Gameplay owns physics tuning.

- **No Rigidbody shipped.** Adding one here would pre-empt gameplay's mass and drag choices.
- **Pivots are at centre of mass**, not the base. A base pivot makes a thrown prop spin around its
  feet like a hammer.
- **Bench and car use one box collider** across the whole silhouette rather than separate
  seat/leg or body/cabin boxes. The gaps underneath are exactly where a ragdoll limb wedges.
- No grab points are authored. If gameplay wants specific hand anchors, say where and they can be
  added as empty transforms.

## Destruction test area

`Assets/Scenes/Arenas/Arena_DestructionTest.unity`

A 46×34m walled courtyard, separate from the production arena.

- **Breakable exterior walls**: `Exterior_Concrete`, `Exterior_Brick` — free space on both sides so
  a thrown enemy reaches impact speed and lands somewhere after breaking through.
- **Breakable interior wall**: `Interior_Divider`, running across the space so breaking it opens a
  route between halves.
- **Six props**, one of each type.
- **Hooks**: `PlayerStart`, `ThrowTarget_Concrete`, `ThrowTarget_Brick`, `EnemySpawn_1`,
  `EnemySpawn_2`.

## Boss arena

`Assets/Scenes/Arenas/BossArena_Graybox_01.unity`

92×92m floor, 28m perimeter walls, open sky.

### Scale

Built for a boss of roughly **3×–6× human height** (up to ~11m). No exact boss size is encoded —
the space is built with clearance across that range.

- **Boss zone: 22m radius kept clear** of columns and structures. A giant boss clipping geometry on
  every step is the main failure mode for this kind of arena, so the central floor is empty.
  `BossArena_HasSpawnHooksAndClearBossZone` sweeps 8 directions at boss chest height to enforce it.
- **28m wall height** gives headroom above an 11m boss for flight.
- **Minion alcoves stay human-scale** (2.6m doorways). Players read them as their own routes, and a
  giant boss cannot follow a minion out through one.

### Verticality

Two walkway rings plus four corner towers, all **outside** the boss zone radius:

| Element | Height |
|---|---|
| Lower ring | 7m |
| Upper ring | 14m |
| Tower decks | 17m |
| Tower crowns | 20.5m |

Rings are four straight spans rather than a curved ring — simple box colliders, no seams for a
dashing player or ragdoll to catch on.

### Spawn hooks

```
GameplayHooks
├── ArenaCenter
├── BossSpawnPoint          (0, 0.1, -18) facing the player side
├── PlayerStart             (0, 0.1, 36) facing the boss
├── SpectatorCamera_Ref
└── MinionSpawns
    ├── MinionSpawn_A1, A2
    ├── MinionSpawn_B1, B2
    └── MinionSpawn_C1, C2
```

Minion spawns sit at 60° intervals at the arena edge, facing inward, so minions enter from several
directions and never appear on top of the player.

### Breakable structures in the boss arena

| Name | Location | Purpose |
|---|---|---|
| `BreakWall_North` / `BreakWall_South` | ±28m on Z | Enemy thrown outward breaks through into the outer ring |
| `Barricade_East` / `Barricade_West` | ±28m on X | Destructible cover at the boss zone edge |
| `BreakPillar_1…4` | Diagonals | Breakable pillars outside the clear floor |

Deliberately limited. Not every surface is breakable — this is a performance-conscious prototype,
and readable destruction beats ubiquitous destruction.

### Navigation

Both new scenes are baked via `Tools > HeroVR > Environment > Bake Arena NavMesh`.
`BossArena_IsNavigableForMinions` verifies minion spawns are on the NavMesh and can path to the boss
zone.

## Quest 2 budget

- Breakables use **authored states**, never runtime fracture — no impact-time spikes, predictable
  collider counts.
- Debris counts are 3–5 per wall, large chunks only.
- Materials are shared across all assets; no transparency.
- One realtime directional light per scene.
- Fixed geometry is marked static for batching. **Breakables and props are deliberately excluded** —
  their states toggle at runtime, and Unity warns when static objects are enabled or moved.
- All colliders are primitives.

## Integration

Attach these once the gameplay components exist:

| Component | Attach to | Notes |
|---|---|---|
| `DestructibleStructure` | `Wall_Breakable_*` prefab **root** | Assign `IntactState`, `DamagedState`, `BrokenState` (direct children, fixed names). Toggling state GameObjects swaps both visual and collision together. |
| `GrabbableObject` | `Prop_*` prefab **root** | Collider already present. Add `Rigidbody` with mass from the table above. |
| `ThrowableObject` | `Prop_*` prefab **root** | Same object as the grabbable. Pivot is already at centre of mass. |
| `BossSpawnPoint` | `GameplayHooks/BossSpawnPoint` | Position and facing set; no geometry changes needed. |
| `MinionSpawnPoint` | `GameplayHooks/MinionSpawns/MinionSpawn_*` | All six, each already facing inward. |

Debris chunks under `BrokenState/Debris` have colliders but no Rigidbody — add one only if you want
them to simulate after a break.

## Known limitations

- **Not playtested in VR.** Geometry, collision and navigation are verified by automated tests
  only; scale and feel need a headset pass.
- **No damage-state transition logic.** States are authored and toggled by gameplay; nothing here
  decides when a wall breaks.
- **Boss arena has no NavMesh links** for the walkways. Minions can path the ground floor; reaching
  the 7m/14m rings would need off-mesh links or flying minions.
- **Prop masses are untested** — recommendations from real-world scale, not tuned against actual
  knockback or impact feel.
- **Breakable pillars reuse the interior wall prefab** rather than being purpose-built pillar
  geometry. Functional but visually a wall segment.
