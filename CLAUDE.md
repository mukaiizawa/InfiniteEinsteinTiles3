# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Infinite Einstein Tiles3** is a Unity game (version 6000.0.35f1) about placing aperiodic "einstein" monotiles (hat-shaped 14-sided polygons) on an infinite canvas. It is published on Steam. The game has two modes: Creative (free placement) and Puzzle (complete a target tiling pattern within constraints).

## Building

Open the project in Unity 6000.0.35f1. There is no CLI build command — use the Unity Editor. The solution files (`InfiniteEinsteinTiles3.sln`) can be opened in an IDE for code editing.

A `DEMO` scripting define symbol exists for demo builds (see `#if DEMO` guards in code).

## Branching Strategy

- `main`: production-ready code
- Feature/bugfix branches: `issues/999` format, branched off `main`

## Code Style

See [CODESTYLE.md](./CODESTYLE.md).

## Architecture

### Scene Flow

Four scenes, loaded via `LoadingManager.LoadAsync(Scene)`:
- `Title` → `Menu` → `PuzzleMenu` → `Tiling`
- Creative mode stays in `Menu` → `Tiling`

Each scene has a corresponding `*SceneManager` MonoBehaviour that drives its state machine via a private `State` enum and a `ChangeState()` method.

### MonoBehaviours (`Assets/Scripts/MonoBehaviours/`)

All manager components live on a single root GameObject per scene via `GetComponent<>()` in `Awake()`.

- **`TilingSceneManager`** — the core gameplay loop. Manages tile placement, selection, blueprint mode, paint mode, puzzle validation, undo/redo, and input handling. Uses `[DefaultExecutionOrder(100)]`.
- **`PersistentManager`** — all file I/O and `PlayerPrefs`. Saves/loads `Solution` and `Progress` as JSON via `JsonUtility`. Directory layout under `Application.persistentDataPath`:
  ```
  Creative/          ← one .json per saved layout
  Puzzle/
    Slot1/
      Puzzle1/       ← one .json per saved solution attempt
      ...
      Progress.json
    Slot2/  Slot3/
  ```
- **`LoadingManager`** — async scene transitions with fade mask.
- **`AssetManager`** — loads textures, materials, audio clips.
- **`AudioManager`** — BGM playlist and SE playback.
- **`SolutionManager`** — manages the Solutions panel UI (list, rename, delete).
- **`SteamManager`** — wraps Facepunch.Steamworks for achievements and overlay.
- **`SettingManager`** — wraps settings sliders.
- **`Tile`** — MonoBehaviour on each tile prefab; holds a `TileMemory` and a `SpriteRenderer`. Supports highlight/unhighlight, color change, blueprint visual, and `ImportMemory`/`ExportMemory`.

### Vanilla (pure C#, no Unity dependency except serialization) (`Assets/Scripts/Vanilla/`)

- **`TileMemory`** — serializable struct for a tile: `Position` (Vector2), `Rotation` (int 0–11, each step = 30°), `Color`. Owns the `VerticesTable` (12 pre-rotated vertex sets for the 14-sided hat polygon). Computes `Edge[]` arrays for placement validation.
- **`Edge`** — a directed segment between two Vector2 points, canonicalized so P.x ≤ Q.x. Used for adjacency checking via `NearlyEqual()`.
- **`Board`** — serializable container: `TileMemory[]` + `Color[]` (30-color palette).
- **`Solution`** — serializable save file unit. Fields: `Name`, `CreatedAt`, `UpdatedAt`, `Board`. Non-serialized fields (`GameMode`, `Slot`, `Level`, `PhysicalName`) are set at load time.
- **`Progress`** — serializable: `CurrentLevel` int. In editor, Slot 2 = level 0 (debug), Slot 3 = all levels complete.
- **`GlobalData`** — static globals: `GameMode`, `Slot`, `Level`, `TotalLevel` (28), `Solution`, `IsRestart`, `IsHardcoreMode`, `Tolerance` (0.31f).
- **`Colors`** — static color constants and helpers (`Parse`, `Format`, `ChangeAlpha`, `ChangeSaturation`). Default 30-color palette.
- **`Tags`** — Unity tag string constants: `"Tile"`, `"LevelTile"`, `"SelectedTile"`.
- **`GameMode`** — enum: `Nil`, `Creative`, `Puzzle`.

### Tile Geometry

The einstein tile is a 14-vertex polygon. `TileMemory.VerticesTable` stores 12 pre-rotated vertex arrays (one per 30° rotation). Rotations are stored as int `[0, 11]`. Edge matching tolerance is `GlobalData.Tolerance = 0.31f` (half of the shortest segment length 0.64).

### Tile Placement & Collision Detection

See [ARCHITECTURE.md](./ARCHITECTURE.md) for the full algorithm description of edge snapping (FindAlignment) and multi-stage collision detection (HasCollision). When multiple tiles are placed at once (grab or blueprint), placement is partial: each tile is individually checked for collision, and only non-colliding tiles are placed. Colliding tiles are discarded (grab) or skipped (blueprint).

### Key Patterns

- Scene managers use internal `State` enums and `ChangeState(State)` for all UI transitions — do not toggle UI directly, always go through `ChangeState`.
- All file operations go through `PersistentManager`; never write files elsewhere.
- `[System.NonSerialized]` fields on `Solution` are populated by `PersistentManager` after `JsonUtility.FromJson`.
- Steam integration via `Facepunch.Steamworks` (see `Assets/Scripts/Facepunch.Steamworks.2.4.1/`).
- Localization uses Unity Localization package with a `"default"` string table; supported locales: en, it, fr, de, ru, pl, pt, es, ja, zh-Hans, zh-Hant, ko.
