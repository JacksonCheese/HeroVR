# Thor Hammer-Spin Flight

Thor flight is driven by Mjolnir motion rather than a generic fly command.
`ThorHammerFlight` consumes `IWeaponMotionSource` and sends collision-aware flight
impulses/modifiers through `IFlightMovementReceiver`. It does not read XR or desktop
input directly.

## Runtime flow

1. The motion source reports linear velocity, angular velocity, held state, and owner.
2. Spin above `minimumSpinSpeed` accumulates for `requiredSpinDuration`.
3. A distinct rising-edge linear motion above `launchMotionThreshold` consumes that
   charge and applies a capped impulse along the measured motion direction.
4. A small configurable upward impulse is added only for a grounded launch so a
   horizontal swing can leave the floor.
5. While airborne, continued spin above the hover activation threshold reduces
   gravity and adds limited lift. Hover remains stable until spin falls below the
   lower deactivation threshold.
6. When spin stops or Mjolnir is no longer held, lift ends and gravity returns toward
   normal at `gravityRestorationRate` rather than snapping immediately.

`TransformWeaponMotionSource` samples the tracked right-controller transform in
`FixedUpdate` and exponentially smooths its linear and angular velocity. XR held state
comes from `XRWeaponInputAdapter`, so flight requires right grip and cannot activate
while Mjolnir is thrown or recalling. Releasing grip still performs the normal hammer
throw; flight launch occurs while grip remains held.
Motion is measured relative to the owning player before being converted back to a
world-space direction, so Thor's own launch velocity does not count as another hammer
gesture.

Desktop Editor and Development builds use `DesktopThorFlightDebugAdapter`:

- Hold `F` for at least the configured spin duration.
- While still holding `F`, tap `G` to provide a launch pulse along camera aim.
- Continue holding `F` while airborne to exercise hover.
- Aim slightly upward before `G` to test vertical launches.

The debug adapter is inactive in non-development builds. Automated tests use a manual
motion source through the same interface.

## Tuning asset

Both Thor prefabs reference
`Assets/Heroes/Thor/ThorHammerFlightSettings.asset`. The most useful first headset
tuning values are:

1. `minimumSpinSpeed`
2. `requiredSpinDuration`
3. `launchMotionThreshold`
4. `launchImpulse`
5. `groundedLaunchLift`
6. `maximumFlightSpeed`
7. `hoverActivationSpinSpeed` / `hoverDeactivationSpinSpeed`
8. `hoverGravityMultiplier`
9. `hoverLiftAcceleration`
10. `hoverDownwardDamping` / `airSteeringMultiplier`

Motion smoothing is currently configured on `TransformWeaponMotionSource` at 0.12
seconds. Tune it only after checking whether real controller motion is noisy or feels
laggy.

## Lifecycle and integration

Death, disable, and respawn cancel flight and clear flight velocity. Death also returns
Mjolnir to its owner. The shared HeroAbility lifecycle cancels active states on death
and resets cooldowns on respawn. Existing hero input, ability loadout, damage, and
arena spawn APIs were not renamed.

Desktop and XR motors implement the additive `IFlightMovementReceiver` contract.
Other heroes do not receive flight behavior unless they compose a flight interpreter.
Spider-Man code should continue using the existing locomotion and ability APIs; it does
not need to reference Thor flight types.
