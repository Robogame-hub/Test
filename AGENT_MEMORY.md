# AGENT_MEMORY

Last updated: 2026-04-10
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
  - Fixed AI steering oscillation ("left-right wobble"):
    - Added corner look-ahead for path following to avoid micro-target jitter near immediate corners.
    - Added desired-direction smoothing and turn-sign stabilization to suppress rapid ±10-15 degree flip behavior.
    - Added rotate-in-place hysteresis (`rotateInPlaceAngle` / `rotateInPlaceExitAngle`) so bots do not rapidly enter/exit turn mode.
  - Switched patrol to dynamic checkpoint progression:
    - `TankAIPatrolPlanner` now picks the next checkpoint only when current is reached (online progression).
    - Next checkpoint selection is strictly from linked `NextNodes` and uses nearest reachable path distance; no prebuilt full-route loop is used.
    - Fallback patrol route remains as reserve when linked checkpoint graph is unavailable.
  - Added stuck detection and recovery in `NavMeshTankAI`:
    - Detects low displacement over time while bot should be moving toward destination.
    - Recovery phases: reverse + pivot turn, then repath.
    - Repeated stuck events on patrol trigger skip to the next linked checkpoint to avoid wall-lock loops.
  - Stabilization pass for dynamic patrol logic:
    - Removed planner-side auto destination override on timeout for checkpoint-graph mode (checkpoint is no longer reselected repeatedly while standing on previous node).
    - Kept checkpoint switches only on true reach/explicit skip, reducing back-forward oscillation.
    - Added stuck-recovery cooldown and heading-error guard to avoid false stuck triggers while bot is legitimately rotating toward path direction.
  - Audited current menu UI architecture before visual redesign (do not break logic):
    - Main menu scene is structured around `MainMenuCanvas` with `LeftPanel` (`PlayButton`, `SandboxButton`, `SettingsButton`, `ExitButton`), sibling panels `SettingsPanel` and `SandboxMatchPanel`, plus `RightPanel -> TankPanel` (`TankSelectionController`, preview, segmented stats).
    - Lobby scene is structured around `LobbyCanvas -> LobbyRoot` with `NicknameInput`, `RoomList/Viewport/Content`, `RoomEntryTemplate`, and controls `RefreshButton`, `CreateButton`, `BackButton`.
    - Core scene pause UI exists as `UIManager -> PauseUIRoot` with `PauseMenuPanel`, `PauseSettingsPanel`, and managed by `BattlePauseMenuController`.
    - Visual colors are currently mostly dark + neon green from builders/config (`MainMenuSceneBuilder`, `CorePauseMenuSceneBuilder`, `MenuButtonFeedbackConfig.asset`), while flow logic is centralized in `MainMenuController`, `LobbyController`, `BattlePauseMenuController`, and `MenuSceneRuntimeBootstrap`.
    - Safety rule for redesign: keep scene object names and controller bindings unchanged; apply styling/layout tuning as a separate visual layer.
  - Implemented non-breaking desert UI/UX visual layer for menu scenes:
    - Added `Assets/Game/Scripts/Menu/MenuDesertTheme.cs` and connected it via `MenuSceneRuntimeBootstrap` plus explicit calls from `MainMenuController`, `LobbyController`, and `BattlePauseMenuController`.
    - Theme now restyles MainMenu/Lobby/Core pause menus at runtime (panels, buttons, sliders, text hierarchy, stat bars, room entries) with a warm desert palette and readable contrast over 3D background scenes.
    - Added responsive anchor tuning for key menu panels (desktop/portrait) so UI keeps playfield/background visibility.
    - Updated feedback/color defaults from neon-green style to desert tones in:
      - `MenuButtonFeedbackConfig.cs`
      - `MenuButtonFeedback.cs`
      - `MenuButtonFeedbackConfig.asset`
      - fallback colors in menu controllers and bootstrap wiring.
    - Updated scene builder color defaults for future scene regeneration:
      - `MainMenuSceneBuilder.cs`
      - `CorePauseMenuSceneBuilder.cs`.
- 2026-04-10:
  - Added `Assets/Game/Scripts/UI/RawImageUvScroll.cs` (`TankGame.UI.RawImageUvScroll`) for CRT/monitor overlays.
  - Component scrolls `RawImage.uvRect` in real time (default speed `0, -0.03`) and supports unscaled time for stable menu/HUD effects.
  - Updated tank preview scan-line behavior to sweep across the full screen height:
    - `Assets/Game/Scripts/Menu/TankSelectionController.cs` now attaches `PreviewScanLineEffect` to the root canvas (instead of only the preview widget host).
    - Added cleanup of duplicate/local preview scan-line effect components when host switching occurs.
  - Investigated Core scene UI vs post-processing composition and applied camera-space canvas fix:
    - In `Assets/Scenes/Core.unity`, `UIManager` canvas was switched from `ScreenSpaceOverlay` to `ScreenSpaceCamera` and bound to `Main Camera`.
    - In `Assets/Game/Scripts/Editor/CorePauseMenuSceneBuilder.cs`, canvas creation/config now enforces `ScreenSpaceCamera` + `worldCamera` assignment so regenerated pause UI does not revert to overlay mode.
  - Migrated localization text storage from hardcoded C# tables to external config:
    - Added `Assets/Resources/Menu/LocalizationConfig.json` (+ `.meta`) as the source of truth for all localized keys and language native names.
    - Refactored `Assets/Game/Scripts/Menu/LocalizationService.cs` to load translations from the JSON config at runtime (`Resources/Menu/LocalizationConfig`), replacing the inline dictionary/switch text tables.
    - Updated language switching logic in `Assets/Game/Scripts/Menu/MainMenuController.cs` and `Assets/Game/Scripts/Menu/BattlePauseMenuController.cs` to use `LocalizationService.GetNextLanguage/GetPreviousLanguage` (removed hardcoded `% 5`/`next=4` assumptions).
