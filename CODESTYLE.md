# Code Style

Follow [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with the following project-specific rules:

## Naming

- Private member variables: `_camelCase` (leading underscore)
- Public variables: `PascalCase`
- Function names: `PascalCase`

## Manager Structure

- Each scene has its own dedicated `*SceneManager` (e.g. `TilingSceneManager`, `MenuSceneManager`).
- All state transitions and scene-specific logic are handled inside that scene's manager. It is acceptable for these files to be long.
- Logic shared across scenes is extracted into a separate manager component (e.g. `PersistentManager`, `AudioManager`). Scene managers obtain references to shared managers exactly once in `Awake()` via `GetComponent<>()`.

## Language

- **All code, comments, and commit messages must be written in English.**

## Formatting

- **No blank lines inside function bodies.** (Functions are edited as vim paragraph units; blank lines break paragraph navigation.)

## Unity-specific

- **Do not use `?.`, `??`, or `??=` in MonoBehaviour classes.** Unity overloads `==` for `UnityEngine.Object`, so destroyed objects compare equal to `null` via `==`. The null-conditional/coalescing operators bypass this overload and use the raw CLR null check, causing destroyed objects to pass as non-null.
