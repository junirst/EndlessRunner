# EndlessRunner

Unity **2022.3.62f2** URP 2D project, **StandaloneWindows64**, C# 9 / netstandard2.1.

Open the repo root in Unity Editor — there are no CLI build/test/lint commands.

## Project structure — 4 mini-games, one title screen

| Directory | Game | Entry scene | Notes |
|---|---|---|---|
| `Assets/TitleScreen.unity` | Title screen | `TitleScreen` | Loads all game scenes by name |
| `Assets/CubeDash/` | Endless runner (main) | `CubeDash` | JSON save at `Application.persistentDataPath + "/cubesaves/"` |
| `Assets/MiniGolf/` | Mini-golf | `MainMenu` | Multiple levels |
| `Assets/Snake/` | Snake | (under `Scenes/`) | No save system, no audio manager |
| `Assets/TopDownShooter/` | Top-down shooter | `Menu` | Separate JSON save at `.../Save/` |

**Build scenes** (in order): TitleScreen, CubeDash, MainMenu, MiniGolf-Level{1,2,3}, Menu, Game.

## Coding conventions

- **Singleton managers**: `public static <Type> Instance` + `Awake` null-check.
- **Null-safe singleton calls**: `AudioManager.Instance?.PlayBgm()` (use `?.` always).
- **Inspector fields**: `[SerializeField] private` for serialized, `public` for non-serialized.
- **Section grouping**: `#region` blocks.
- **Event-driven UI**: `UnityEvent` callbacks (`onPlay`, `onGameOver`, etc.) wired in `Start`.
- **Audio per game**: each sub-game has its own `AudioManager` (sfx + bgm sources, wrapper methods per clip). `TitleAudioManager` stops all game BGM on init via coroutine with two passes.
- **Scene transitions**: `SceneManager.LoadScene(sceneName)` (string overload).

## Save system quirks

- CubeDash: `Assets/CubeDash/Scripts/SaveSystem.cs` — static class, folder `cubesaves/`.
- TopDownShooter: `ShooterSaveSystem.cs` — `MonoBehaviour`, folder `Save/`. Must call `Initialize()` first. Same folder pattern.
- Both use `JsonUtility.ToJson/FromJson`.

## Gotchas

- `Assets/Title.cs` contains class `TitleScript` (mismatch with filename; refactor carefully).
- Snake's `Snake.cs` uses `transform.Translate` + `FixedUpdate` snapping — **not** `Rigidbody2D`. No audio manager.
- `Time.timeScale` is set to 0 in pause, restored to 1 on resume/game-over — always reset it when loading a new scene directly.
- `.vscode/settings.json` hides `.meta`, `.unity`, `.prefab`, `.asset` etc. from the file explorer.
