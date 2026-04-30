# CLAUDE.md

This file is the durable context for the **Warblade-style** project. Read it before answering any question about this codebase.

> Working title: **Warblade-style** (placeholder — will be renamed later).
> Planning, milestones, and task breakdowns live in a separate planning file — not here.

---

## Project Overview

A 2D arcade space shooter inspired by **Warblade 1.5** and *Galaga*. The player controls a ship locked to the bottom of the screen, moving only on the X-axis, firing at swooping waves of enemies. Features 100 hand-tuned levels with bosses every 25 levels, then infinite scaling cycles afterward. Loot drops, an upgrade shop, mini-games, and a global leaderboard round out the loop.

This is a **solo portfolio project** (third major project) intended to demonstrate clean architecture, polished arcade gameplay, and use of modern Unity ecosystem tools.

---

## Target Platform

**WebGL only.** Desktop browser. Single platform commitment — no mobile, no native desktop, no console.

- **Reference resolution:** 1920×1080 (16:9 widescreen, horizontal)
- **Canvas behavior:** Fit-to-screen with letterboxing if the player's browser window has a different aspect ratio
- **Orientation:** Landscape (no rotation handling needed)
- **Browsers:** Modern evergreen (Chrome, Firefox, Edge, Safari)

Mobile / touch input is explicitly **out of scope** — do not suggest touch-friendly UI, virtual buttons, `Input.touches`, or mobile-specific architecture.

---

## Tech Stack

- **Engine:** Unity 6.3 LTS
- **Render Pipeline:** Universal Render Pipeline (URP) 2D Renderer with 2D Lights
- **Input:** New Input System (`com.unity.inputsystem`) — do not suggest legacy `Input.GetKey` patterns
- **Backend:** Unity Gaming Services (UGS)
  - Anonymous Authentication
  - Leaderboards
  - *(Cloud Save and Analytics are not in scope yet — ask before adding.)*
- **Language:** C# only, English-only UI (no localization layer needed)

---

## Controls

Faithful to original Warblade:

- **Move Left:** Left Arrow (and `A` as alternate)
- **Move Right:** Right Arrow (and `D` as alternate)
- **Fire:** Left Ctrl (and `Space` as alternate)
- **Pause:** `P` or `Escape`

### Firing Behavior

- **Default:** One key press = one shot. Holding the fire key does **not** auto-fire.
- **Autofire (bonus drop):** A collectible power-up grants temporary autofire — while active, holding the fire key produces continuous shots. Duration is timed (not permanent like the original, which lost it on death/shop). Specific duration TBD in tuning.

The fire input must be implemented so that the same code path handles "tap to shoot" and "hold to autofire when buff is active" without branching the input layer.

---

## Architecture

### Code Organization (strict separation)

```
Assets/
  Scripts/
    Managers/      // GameManager, LevelManager, AudioManager, etc. (singletons)
    Systems/       // Pooling, Spawning, Scoring, Input, etc.
    Data/          // ScriptableObject definitions (EnemyWaveData, WeaponData, etc.)
    Entities/      // Player, Enemy, Bullet, Pickup MonoBehaviours
    UI/            // HUD, Shop, Menu, Leaderboard views
  Prefabs/
  ScriptableObjects/
  Art/
  Audio/
  Scenes/
```

Keep folders matching this structure. Don't mix entity scripts into Managers, etc.

### Data + Behavior Split

- **ScriptableObjects for data:** enemy stats, wave definitions, weapon configs, boss patterns, shop items, level metadata, drop tables. Anything that's "tuning data" is a SO.
- **MonoBehaviours for runtime behavior:** the things that actually move, collide, and tick each frame.
- Entities read from SOs; they do not hardcode values.

### System Communication

- **Default:** ScriptableObject-based event channels (e.g., `GameEventChannel` SO with a `Raise()` method and `OnEventRaised` action). This keeps systems decoupled — the player doesn't know about the score UI, it just raises an event.
- **Singletons (sparingly):** only for genuinely global, single-instance services — `GameManager`, `LevelManager`, `AudioManager`, `PoolManager`. Don't reach for a singleton when an event channel works.
- Avoid `FindObjectOfType` and `GameObject.Find` in runtime code.

### Object Pooling (strict)

Use Unity's built-in `UnityEngine.Pool.ObjectPool<T>` for **anything that spawns more than ~10 times per session**:
- Bullets (player + enemy)
- Enemies (all types)
- Particle/VFX one-shots
- Pickups, cash drops, damage numbers

Never `Instantiate`/`Destroy` in hot paths. WebGL has tighter memory constraints and weaker JIT than native — pooling matters here too.

### Player Movement

- X-axis only. Y is locked at the bottom of the screen.
- Read horizontal input via the New Input System.
- Use `Mathf.Clamp` for hard left/right boundaries — no screen-wrap.
- Movement is direct/responsive, not physics-driven (no Rigidbody acceleration curves).

### Level Progression

A single `currentLevel` integer drives everything:
- `currentLevel % 25 == 0` → boss level
- `currentLevel / 3` → indexes into bug-set array
- `cycle = (currentLevel - 1) / 100` → difficulty multiplier and visual tinting after level 100

The whole game state should be reconstructible from `currentLevel` + player upgrades + active timed buffs.

---

## Coding Conventions

Microsoft / Unity standard:

- `PascalCase` for classes, methods, properties, public fields, constants
- `_camelCase` for private fields (with leading underscore)
- `camelCase` for local variables and parameters
- `[SerializeField] private` over `public` for inspector-exposed fields
- One class per file, file name matches class name
- Namespaces: `Warblade.Managers`, `Warblade.Systems`, `Warblade.Data`, etc. (folder-aligned)
- XML doc comments (`///`) on public APIs of Systems and Managers; not required on simple entity scripts

---

## Dependencies Policy

**Always ask before adding any dependency** — paid Asset Store packages, free Asset Store packages, NuGet, third-party plugins, or even non-default Unity packages.

Prefer built-in Unity systems:
- Particle System (Shuriken) over VFX Graph for now
- Cinemachine for screen shake (built-in)
- TextMeshPro for all text (built-in)
- URP 2D Lights instead of custom shaders where possible

If a built-in solution exists, use it. If not, surface the dependency request explicitly with the reason.

---

## Working Style with Claude

- I'm **intermediate** with Unity/C# — comfortable but still learning. Skip the "what is a MonoBehaviour" basics, but don't skip the "why" behind a pattern choice.
- **Explain the reasoning** behind suggestions — pattern choices, performance tradeoffs, why one approach beats another for this project. The learning is part of the goal.
- Code samples should be production-ready for this project's conventions, not generic Unity tutorial code.
- When I ask "how do I do X," default to showing the approach that fits this architecture, even if it's slightly more setup than a quick hack.
- Flag when something is a hack vs. the right way, so I can choose knowingly.

---

## Git Workflow (mandatory)

I switch between desktop and laptop frequently — **the working tree must never carry uncommitted work between sessions.** When asked to commit:

- **Always `git add .`** — stage everything in the working tree, never selective adds. The `.gitignore` is the source of truth for what gets excluded; if something shouldn't be tracked, fix the `.gitignore`, don't skip files at the staging step.
- **Always push to `main`** immediately after committing — no local-only commits, no feature branches without explicit instruction.
- **Detailed commit messages** — multi-line, written so reading the log on the other machine is enough to catch up. Lead with a one-line summary, then a body that lists what changed and why. Don't be terse just to save keystrokes.
- **Never `--no-verify`, never `--force`, never amend pushed commits.**

---
