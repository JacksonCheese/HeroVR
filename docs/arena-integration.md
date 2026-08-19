# Arena ↔ Gameplay Integration Notes

Notes from the environment side for whoever wires gameplay into `Arena_Graybox_01`.
Written after an integration test of the gameplay branch against the graybox arena.

## What the arena provides

`Assets/Scenes/Arenas/Arena_Graybox_01.unity`

```
Environment      all geometry, colliders, materials (static, environment-owned)
GameplayHooks    bare transforms, no components
Lighting         one directional key light
```

`GameplayHooks` contains four spawn transforms, already positioned and facing the
arena centre:

| Transform       | Position        | Facing |
|-----------------|-----------------|--------|
| `TeamA_Spawn_1` | (-6, 0.1, 24)   | −Z     |
| `TeamA_Spawn_2` | (6, 0.1, 24)    | −Z     |
| `TeamB_Spawn_1` | (-6, 0.1, -24)  | +Z     |
| `TeamB_Spawn_2` | (6, 0.1, -24)   | +Z     |

Plus `ArenaCenter` and `SpectatorCamera_Ref`.

Each team's spawns sit behind their own tower, so opponents have no line of sight
at match start.

## Hooking up spawns

Add `ArenaSpawnPoint` to the four transforms. Positions and rotations are already
set — no geometry changes needed.

For the current 1v1 `GameplayMatchBootstrap`, which looks for exactly one `Player`
and one `TrainingEnemy`:

- `TeamA_Spawn_1` → team `TeamOne`, slot 1, type `Player`
- `TeamB_Spawn_1` → team `TeamTwo`, slot 1, type `TrainingEnemy`

The other two are ready for 2v2 later. Verified working: both prefabs spawn and
run in the arena with no console errors.

## Traversal contract

**This is the important part.** The arena's geometry is tuned against specific
movement values. Two bugs already shipped from these drifting apart, so if the
movement numbers change, the arena needs to change with them.

Current assumptions, read from `DesktopPlayer.prefab` and `DesktopCharacterMotor`:

| Value | Source | Arena depends on it for |
|-------|--------|-------------------------|
| `stepOffset` 0.3 | CharacterController | Plaza steps are 0.25m risers |
| `jumpHeight` 2.6 | DesktopCharacterMotor | Tower climb is 2.0m hops |
| `gravity` -22 | DesktopCharacterMotor | Jump arc / horizontal reach |
| `moveSpeed` 7 | DesktopCharacterMotor | Clearing the 2.5m ledge gap |

Note that `DashAbility` zeroes its Y component, so dash gives no vertical lift.
2.6m is the hard vertical reach.

Traversal layout:

```
ground 0 ──steps 0.25──> plaza 0.5
ground 0 ──ramp────────> wing deck 3.8   (no mobility power required)
wing deck 3.8 ──2.0m hop──> tower ledge 5.8
tower ledge 5.8 ──2.0m hop──> tower roof 7.8
```

`ArenaGrayboxBuilder.ValidateTraversal()` re-checks these on every arena build and
logs an error if a step exceeds the step offset or a hop exceeds the jump height.
If you retune movement, rebuild the arena (`Tools > HeroVR > Environment > Build
Graybox Arena`) and read the console — it will say whether anything became
unreachable.

The two bugs it now guards, both real:

- Plaza risers were 0.4m against a 0.3 step offset, so nothing could walk onto the
  central fight zone.
- Tower roofs needed a 3.15m hop against a 2.6m jump, so the arena's high ground
  was unreachable.

## Known issue: the training bot cannot navigate the arena

`TrainingBot.FixedUpdate` steers with a single force straight at its target:

```csharp
Vector3 desired = to.normalized * moveSpeed;
rb.AddForce((desired - horizontal) * acceleration, ForceMode.Acceleration);
```

There is no pathfinding, obstacle avoidance, or jump, and `to.y` is zeroed.

In an empty box this is fine. In this arena the bot walks into the first thing
between it and the player and stays there. Observed: it wedged against a tower
leg with the 6m passage only 1.7m away and never recovered.

This is not an arena defect — there is open ground all the way around the towers —
but the bot needs some steering or a NavMesh before it can fight here.

Related: the bot is a Rigidbody + CapsuleCollider with no step handling at all, so
it cannot climb anything a CharacterController would step over. It does clear the
0.25m plaza steps under physics alone, which is a useful lower bound: geometry the
bot can climb, the player definitely can.

## Deliberately absent

The scene has **no Camera and no EventSystem**. Those belong to the player rig, so
the environment branch does not ship them. Game view will report no camera until a
gameplay bootstrap spawns a player.

Also, the scene is intentionally **not** in Build Settings, to avoid churn in a
shared settings file.

## Regenerating the arena

The scene is generated from `Assets/Environment/Editor/ArenaGrayboxBuilder.cs`, not
hand-authored. Re-running the menu item rebuilds it from scratch, which **discards
hand edits made in the Editor**, including any components added to the spawn
transforms. Change the layout constants in the script instead, and re-add gameplay
components after a rebuild.
