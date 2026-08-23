# Bomber Legends

Grid-based tactical action game — a modernised Bomberman with an afro-futurist neon identity.
Mobile-first, Unity 6.3 LTS.

Design and engineering documentation lives in [`docs/`](docs/README.md). Start with
[`docs/01-ANALYSIS.md`](docs/01-ANALYSIS.md).

---

## Requirements

| | |
|---|---|
| **Unity** | **6000.3.10f1** (Unity 6.3 LTS) — exact version, see `ProjectSettings/ProjectVersion.txt` |
| **Modules** | Android Build Support, WebGL Build Support, Windows Build Support |
| **Git LFS** | Required before committing any binary asset — see below |

Open the project folder in Unity Hub. Do not upgrade the editor version without a team decision;
`ProjectVersion.txt` is the contract.

---

## Project state

**Milestone 0 complete** (T-001 → T-009), with one item needing a physical device — see below.
The app boots, composes its services, loads the save and moves between hub and match. There is no
gameplay yet; that starts at Milestone 1.
See [`docs/05-BACKLOG.md`](docs/05-BACKLOG.md) for what comes next.

### Running it

Press Play from any scene — `Play From Bootstrap` (under the **Bomber Legends** menu) starts the app
from `Bootstrap` and then continues into whichever scene you had open. Toggle it off from the same
menu if you ever need raw play mode.

### Tests

```bash
./tools/unity test
```

`test -p` runs the PlayMode suite, `test --all` runs both, and a bare argument filters:
`./tools/unity test EnemyThreatTests`. Results and the full editor log land in `Logs/`.

The script exists for one reason above the others: **a compile error aborts the run before the
test runner writes anything**, so a results file left over from last time reports a confident pass
over broken code. It deletes the file first, every time, and refuses to run at all while the Editor
holds the project lock.

### Android build

```bash
./tools/unity build android
```

`build webgl` and `build windows` do the same for the other targets; `--release` switches from the
development build to the release one.

Output lands in `Builds/Android/`. IL2CPP, ARM64, ASTC, landscape-only, min SDK 26. The development
build is signed with Unity's debug keystore; a release keystore is required before any store upload.

### ⚠️ Needs a physical device

No Android device or `adb` is available on this machine, so two things remain unverified:

1. The APK installs and launches to the hub on a real device.
2. **The save survives a force-stop** — the outstanding acceptance criterion from T-005. Earn or
   change some state, force-stop the app from Android settings, relaunch, and confirm it reloads.

### Assemblies

Twelve assemblies, strict acyclic graph — see [`docs/03-ARCHITECTURE.md`](docs/03-ARCHITECTURE.md) §2.
`BomberLegends.Core` and `BomberLegends.Simulation` are compiled with **`noEngineReferences`**: they
cannot call into Unity at all, which is what makes the simulation testable headlessly, deterministic,
and reusable as a server simulation later.

These rules are enforced by `AssemblyGraphTests` (EditMode). Breaking the graph fails the build, not
a code review. Run the tests with:

```bash
./tools/unity test AssemblyGraphTests
```

C# nullable reference types are enabled project-wide through `Assets/csc.rsp`.

### Configuration locked at T-001

| Setting | Value | Why |
|---|---|---|
| Render pipeline | URP 2D (`Assets/_Project/Settings/Rendering/`) | 2D game; `Renderer2DData` renderer |
| Colour space | **Linear** | Required for correct bloom — the neon aesthetic depends on it |
| Active input handler | **Input System** (new) | Architecture §9; legacy input is off |
| Asset serialisation | **ForceText** | Readable diffs, mergeable YAML |
| Meta files | Visible | Version-control friendly |

---

## Repository layout

```
Assets/_Project/     all authored content — Art, Audio, Data, Prefabs, Scenes,
                     Scripts, Settings, Tests, Vfx
LocalPackages/       vendored package tarballs (see "Known environment issue")
Packages/            UPM manifest
ProjectSettings/     project configuration (committed)
docs/                design and engineering documentation
tools/               batchmode wrappers — `tools/unity test`, `tools/unity build`
```

Scripts are organised **by feature**, not by type. See
[`docs/03-ARCHITECTURE.md`](docs/03-ARCHITECTURE.md) §1.

---

## Setup

### 1. Git LFS — required

Not yet installed on the current machine. Binary assets **must not** be committed until it is:

```bash
sudo apt-get update && sudo apt-get install -y git-lfs && git lfs install
```

`.gitattributes` already declares the tracked binary types (images, audio, models, fonts, video).
Committing a `.png` before running the above will fail.

### 2. Unity YAML merge — recommended

Register Unity's smart merge tool so scene and prefab conflicts are resolvable:

```bash
git config merge.unityyamlmerge.driver '"/mnt/c/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

---

## Known environment issue — vendored packages

Three packages are referenced from `LocalPackages/` as local tarballs rather than from the Unity
registry:

- `com.unity.inputsystem` 1.20.0
- `com.unity.ide.rider` 3.0.34
- `com.unity.ide.visualstudio` 2.0.22

**Cause.** These packages are served from `cdn.packages.unity.com`, which returns an incomplete TLS
certificate chain on this machine's network path. Windows `curl` compensates by fetching the missing
intermediate via AIA; UPM (Node-based) does not, and fails with
`unable to verify the first certificate`. Packages served from `packages.unity.com` (URP, uGUI, Test
Framework, ShaderGraph, 2D) resolve normally.

**Why this workaround.** UPM's `file:` dependency is a documented, portable feature. It needs no
administrator rights, works on any machine, and is reproducible from a clean clone. Adding the
missing intermediate to the editor's CA bundle
(`Editor/Data/Resources/PackageManager/Server/app/cacerts.pem`) would require elevation and would be
lost on every editor upgrade.

**Retirement condition.** When UPM can reach the CDN — a Unity patch, a network change, or a fixed
CDN chain — replace the three `file:` entries with plain version strings and delete `LocalPackages/`.
Verify with a clean `Library/` reimport before removing the tarballs.

> Note: `com.unity.inputsystem` **1.12.0** (the version in the editor's bundled 6.1-era 2D template)
> does not compile on 6000.3 — `BuildTarget.ReservedCFE` is missing. 1.20.0 is the correct version.

---

## Packages deliberately not installed

| Package | Reason | Revisit |
|---|---|---|
| Cinemachine | Levels are single-screen with a fixed camera (decision Q3) — no use for it | If scrolling levels are ever adopted |
| Addressables | Not needed until `IAssetService` is implemented | T-006 |
| UniTask | **Not needed.** Unity 6's built-in `UnityEngine.Awaitable` covers the project's async surface — first-party, player-loop integrated, pooled, WebGL-safe. Supersedes the UniTask preference in `CLAUDE.md`. Revisit only if we need UniTask's richer combinators. | — |
| Localization | English-only until production polish | M8 |
| Visual Scripting, Timeline, Collab | Not used; compile time and project noise | — |
| 2D Animation, PSD Importer, Aseprite, SpriteShape | Art pipeline packages | M8 / T-053 |

Unity engine **modules** were left at their defaults. Trimming unused ones (terrain, vehicles, cloth,
VR/XR, umbra) is a build-size task for **T-009**, to be done with measurement rather than guesswork.
