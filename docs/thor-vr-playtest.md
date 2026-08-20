# Thor VR Playtest

Open `Assets/Scenes/Arenas/Arena_ThorVRTest.unity` to test Thor in the production
graybox. This gameplay-owned scene adds generic `ArenaSpawnPoint` components and a
Thor match bootstrap to the environment scene without changing
`Arena_Graybox_01.unity`.

For keyboard/mouse testing, open
`Assets/Scenes/Arenas/Arena_ThorDesktopTest.unity`. It uses the same Thor definition,
combat values, lightning ability, Mjolnir prefab, throw/recall state machine, enemy,
and generic arena spawn contract as the XR scene.

## Controls

- Left stick: move
- Right stick: snap turn
- Right A: jump
- Left stick click: Thunder Dash
- Right trigger: aimed lightning
- Right grip press, then release with a throwing motion: throw Mjolnir
- Right B: recall Mjolnir
- Right stick click: God of Thunder ultimate when charged
- Physical left-hand swing: unarmed punch
- Physical Mjolnir swing: hammer melee

## Desktop Controls

- WASD: move
- Mouse: aim/look
- Space: jump
- Left Shift: Thunder Dash
- Left click: Mjolnir melee strike
- Right click: aimed lightning
- Q: throw Mjolnir along the camera aim
- R: recall Mjolnir
- E: God of Thunder ultimate when charged
- Escape: release the mouse cursor; click the Game view to capture it again

The energy-projectile XR aim fix is available on the shared `XRPlayer` prefab and
uses the right controller pointer/aim pose. Thor replaces that ability slot with
lightning, which uses the same aim provider.

## Quest 2 Check

1. Confirm both controllers track and provide input.
2. Confirm right A jumps once per press and the former left primary binding does not jump.
3. Aim forward, left, right, up, and down; verify the projectile or Thor lightning follows.
4. Dash into open space and a wall; verify visible travel and collision.
5. Confirm Thor and the training enemy spawn in the arena.
6. Verify hammer melee, physical throw, visible recall, and lightning damage.
7. Verify Thor cannot damage himself with Mjolnir, projectiles, or lightning.
8. Recheck plaza and tower traversal, then death and respawn.

For an Android headset build, include `Arena_ThorVRTest.unity` as the launch scene
in the active Build Profile. The repository enables Android Oculus Touch Controller
Profile and Meta Quest Support while preserving the working Standalone Oculus
Touch profile. Quest 2 device validation and sustained performance profiling are
still required.
