# Basketball foundation: setup and validation

This milestone adds a replicated rigid-body ball actor and a server-authoritative starter possession component.

## Add it to the Unreal project

1. Open the project with Unreal Engine 5.6 and allow the C++ modules to compile.
2. Create a Blueprint child of `AQHBasketball`.
3. Assign a basketball Static Mesh to `BallMesh`. Keep its mesh collision disabled; the root sphere is the physics collider.
4. Tune the root sphere radius to match the chosen mesh (the C++ default is 12 cm).
5. Place an instance in a test level. Confirm that the floor has collision and that the ball settles under gravity.
6. Add `QHBallPossessionComponent` to the athlete Blueprint derived from `AQHPlayerCharacter`.
7. Ensure the athlete has a skeletal mesh with a hand socket named `hand_r`. If it does not, the component uses a simple root-relative fallback position.
8. In a Blueprint input event, call `RequestPickup` with the nearby ball. Call `RequestRelease` with a launch velocity to release it.

## Current behavior

- The ball is a replicated actor with replicated movement and a physics-enabled sphere collider.
- Pickup is processed on the server and rejected if the player is too far away, already holds a ball, or the ball is unavailable.
- A held ball is attached to the player's hand socket and its physics/collision are disabled.
- Release detaches the ball, restores physics/collision, and applies the requested linear velocity.
- Ball and possession state are replicated for networked play.

## Validation checklist

- [ ] C++ project compiles in the target Unreal Engine 5.6 installation.
- [ ] Loose ball collides with the floor and rolls/bounces as configured.
- [ ] A player outside PickupRange cannot acquire the ball.
- [ ] Two players cannot acquire the same ball at the same time on the server.
- [ ] Pickup attaches to the hand socket and stops rigid-body simulation.
- [ ] Release restores collision and physics and produces the expected trajectory.
- [ ] Run a two-client PIE session and verify pickup/release replication.
- [ ] Test ball reset after a score or out-of-bounds event.

## Not implemented yet

This is a foundation, not a complete basketball simulation. It does not yet implement dribble cadence/hand contact, ball-hand animation synchronization, shooting input and aim, passes, steals, rebounds, scoring, court geometry, hoop/rim collision, fouls, AI, or a finished player model. Those require additional systems, animation assets, tuning, and in-editor testing. No Unreal build has been run in this environment.
