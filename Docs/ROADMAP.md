# QUBE HOOPS development roadmap

## Vertical slice acceptance criteria

A milestone is not complete until it can be built and exercised in Unreal Editor.

### Milestone 1 - playable court
- One half-court blockout with measured court markings and two hoops.
- One controllable athlete with working keyboard/controller input.
- Follow camera with tunable distance and sensitivity.
- A physics-enabled basketball with collision and reset behavior.
- A simple test map and repeatable test checklist.

### Milestone 2 - basketball interaction
- Explicit ball states: Free, Held, Dribbling, InFlight, Dead.
- Server-ready possession ownership interface.
- Dribble timing and hand selection tied to athlete movement.
- Pass target selection, ball flight, catch windows and turnovers.
- Shot gather, release timing, arc, rim/backboard collision and rebound outcomes.

### Milestone 3 - competitive modes
- Rulesets for 1v1, 2v2 and 3v3.
- Possession, check-ball, scoring, shot clock and game clock.
- AI roles for ball handler, off-ball spacing, defender and rebounder.
- Deterministic gameplay tests for scoring and possession transitions.

### Milestone 4 - presentation and quality
- Original human athlete character assets and retargeted animation set.
- Foot planting, locomotion blending, upper-body actions and contact reactions.
- Original arena, crowd, lighting, sound and broadcast-style UI.
- Scalability profiles and performance budgets.

### Milestone 5 - release readiness
- Automated Windows Development and Shipping builds.
- Crash reporting, save validation, input remapping and accessibility checks.
- Multiplayer security and latency testing before any online release.
- Platform feasibility review for any future console port.

## Non-goals for the first slice
- No copied NBA teams, player likenesses, logos, broadcast audio or proprietary animations.
- No fake online matchmaking or placeholder network buttons.
- No promise of PlayStation release before approved platform access and certification.
