# AGENT_MEMORY

Last updated: 2026-04-06
Workspace: `D:\PROJECTS\Test\Test`

## Purpose
This file is a persistent project context for future tasks.
I should read this first before making changes.

## Project Snapshot
- Engine: Unity `6000.2.12f1`
- Main scenes in build:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/Lobby.unity`
  - `Assets/Scenes/Core.unity`
- Disabled in build: `Assets/Scenes/Main.unity`
- Stack highlights: URP, Input System, AI Navigation.

## High-Level Flow
1. `MainMenu` scene:
   - Start mode choice (single/multiplayer)
   - Tank selection
   - Session settings saved in `PlayerPrefs`
2. `Lobby` scene:
   - Player nickname/team/ready flow
   - Start battle
3. `Core` scene:
   - Spawn manager + player/bots
   - Tank runtime, combat, UI, pause

## Core Runtime Entry Points
- Game bootstrap:
  - `Assets/Game/Scripts/Game/GameInitializer.cs`
- Session state:
  - `Assets/Game/Scripts/Session/GameSessionSettings.cs`
- Menu controllers:
  - `Assets/Game/Scripts/Menu/MainMenuController.cs`
  - `Assets/Game/Scripts/Menu/LobbyController.cs`
  - `Assets/Game/Scripts/Menu/TankSelectionController.cs`
  - `Assets/Game/Scripts/Menu/MenuSceneRuntimeBootstrap.cs`
  - `Assets/Game/Scripts/Menu/BattlePauseMenuController.cs`

## Tank Gameplay Architecture
- Main orchestrator:
  - `Assets/Game/Scripts/Tank/TankController.cs`
- Input contract:
  - `Assets/Game/Scripts/Commands/TankInputCommand.cs`
- Input adapter:
  - `Assets/Game/Scripts/Tank/TankInputHandler.cs`
- Runtime helpers:
  - `Assets/Game/Scripts/Tank/TankRuntime.cs`
  - `Assets/Game/Scripts/Tank/TankMatchContext.cs`
  - `Assets/Game/Scripts/Tank/TankRegistry.cs`
- Components:
  - `Assets/Game/Scripts/Tank/Components/TankMovement.cs`
  - `Assets/Game/Scripts/Tank/Components/TankTurret.cs`
  - `Assets/Game/Scripts/Tank/Components/TankWeapon.cs`
  - `Assets/Game/Scripts/Tank/Components/TankHealth.cs`
  - `Assets/Game/Scripts/Tank/Components/TankEngine.cs`
- Projectile:
  - `Assets/Game/Scripts/Weapons/Bullet.cs`
- AI:
  - `Assets/Game/Scripts/Tank/AI/NavMeshTankAI.cs`

## Networking Status (Important)
- Current real transport is not fully integrated.
- Present abstraction:
  - `Assets/Game/Scripts/Network/INetworkAdapter.cs`
  - `Assets/Game/Scripts/Network/LocalNetworkAdapter.cs` (loopback/stub behavior)
- There are many networking docs, but code-level implementation is currently local-first.

## UI Systems
- `Assets/Game/Scripts/UI/HealthUI.cs`
- `Assets/Game/Scripts/UI/AmmoUI.cs`
- `Assets/Game/Scripts/UI/StaminaUI.cs`
- `Assets/Game/Scripts/UI/CrosshairUI.cs`
- `Assets/Game/Scripts/UI/WeaponUIStateSwitcher.cs`

## Spawn and Match Control
- `Assets/Game/Scripts/Game/SpawnManager.cs`
- `Assets/Game/Scripts/Game/SpawnPoint.cs`
- `Assets/Game/Scripts/Game/BotPoolManager.cs`

## Editor Tools
- `Assets/Game/Scripts/Editor/MainMenuSceneBuilder.cs`
- `Assets/Game/Scripts/Editor/CorePauseMenuSceneBuilder.cs`
- `Assets/Game/Scripts/Editor/PluginCleanupWindow.cs`

## Known Documentation Drift
- Some old docs mention `TankController_New` / `Bullet_New`.
- Actual live scripts are `TankController.cs` and `Weapons/Bullet.cs`.
- Rule: trust current C# scripts over older markdown when they conflict.

## Working Tree Safety Notes
- Repository already has a very large set of unrelated modified assets/scenes/lightmaps.
- Rule: do not revert unrelated changes.
- Rule: keep edits minimal and scoped to user request.

## Operational Rules For Future Tasks
1. Start from this file and then open only relevant scripts.
2. Prefer code truth over historical docs.
3. Avoid broad scene/lightmap/material changes unless user asks explicitly.
4. When changing gameplay, check:
   - `TankController` authority mode behavior
   - `TankInputCommand` compatibility
   - bot AI (`NavMeshTankAI`) assumptions
5. Preserve scene flow: `MainMenu -> Lobby -> Core`.
6. Always update this memory file after each substantive implementation task (what changed + why).
7. When AI behavior is modified, record updated assumptions about patrol, chase, and coordination logic.

## Session Log
- 2026-04-06:
  - Read project structure, key docs, gameplay/menu/network scripts, and editor builders.
  - Built this memory file to keep persistent context for future tasks.
  - Refactored tank bot AI architecture and added modular brain/planner flow.
  - Updated patrol routing to respect checkpoint graph links and nearest-start behavior.
  - Added strict turning/point-following tuning, squad anti-clumping combat coordination, and patrol rebuild on returning from chase/search.
  - Added memory maintenance rule: always update `AGENT_MEMORY.md` after substantive tasks.
  - Tightened patrol precision:
    - Route start node now selected by shortest complete NavMesh path length (with distance fallback), improving "nearest point" start behavior in real topology.
    - Added strict checkpoint snapping limit (`checkpointMaxSnapDistance`) so patrol points stay close to authored checkpoint nodes.
    - Tightened patrol reach/stop clamps in AI (`checkpointReachDistance`, `patrolStoppingDistance`) to reduce wide waypoint passes.
