# QUBE HOOPS

QUBE HOOPS is an original basketball game project targeting Windows first, built on Unreal Engine 5 with a realistic-but-optimized visual target.

## Game scope

- Play modes: 1v1, 2v2 and 3v3 only.
- Original athletes, uniforms, arenas, audio and branding.
- Human athlete presentation with skeletal animation, natural locomotion and detailed materials.
- Basketball systems designed around authoritative ball state, possession, dribble, pass, shot, rebound and defensive interactions.
- Scalable rendering with a 60 FPS target on supported PC hardware; actual performance depends on hardware and content.

## Current project status

This repository currently contains the initial Unreal C++ project scaffold and a configurable third-person athlete movement/camera class. It is **not yet a finished or compiled game**. No packaged executable is included.

## Requirements

- Unreal Engine 5.6 (or update the EngineAssociation in QubeHoops.uproject to match your installed version)
- Visual Studio 2022 with the Desktop development with C++ workload and the game development with C++ components
- Windows 10/11

## Open the project

1. Install the matching Unreal Engine version through Epic Games Launcher.
2. Right-click `QubeHoops.uproject` and generate Visual Studio project files.
3. Open the generated solution and build the Development Editor configuration.
4. Open the project in Unreal Editor.

The athlete class expects Enhanced Input assets to be created and assigned in a Blueprint subclass or default configuration. Input assets and art assets are not included yet.

## Planned milestones

1. **Playable foundation:** court blockout, athlete Blueprint, input assets, ball actor and possession state.
2. **Core basketball:** dribble state machine, pass/catch, shot release/timing, rebounds and defensive contests.
3. **Team play:** 1v1/2v2/3v3 rules, AI spacing, on-ball defense, screens and transition logic.
4. **Presentation:** original athlete rigs and animations, uniforms, arena, scoreboard, audio, replay and UI.
5. **Release engineering:** automated builds, performance profiling, QA, Windows packaging and later platform feasibility review.

## Design principles

- Do not ship fake menu controls or simulated features presented as complete systems.
- Keep gameplay systems testable and separate from presentation.
- Use original or properly licensed assets. Do not copy NBA 2K assets, branding or proprietary content.
- Treat console releases as a later project requiring platform-holder approval, development access and certification.
