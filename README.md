# Warblade-style

A 2D arcade space shooter inspired by **Warblade 1.5** and **Galaga**.

The player controls a ship locked to the bottom of the screen, moves left and right, and fights through swooping enemy waves. The long-term goal is a polished WebGL arcade game with hand-tuned levels, bosses, loot drops, a shop loop, mini-games, and online leaderboards.

This is a solo portfolio project focused on clean Unity architecture, responsive arcade gameplay, and steady milestone-based development.

## Current State

The project currently has the core shooter loop, M4 reward loop, M5 boss architecture, completed M6 content-fill foundation, and completed M7 presentation polish in place:

- Player movement and shooting
- Enemy waves, formations, and level progression
- Multiple enemy types
- Object pooling for bullets and enemies
- ScriptableObject-driven enemy, wave, formation, and level data
- Run stats, lives, armour, weapon tiers, and event-channel foundations
- Pickup drops, timed buffs, cash rewards, stat upgrades, and the shop overlay
- First-pass 100-level campaign route: 96 normal levels plus boss routes at 25, 50, 75, and 100
- M6 wave-authoring safeguards and content validation tools
- Cycle scaling code for levels 101+ using repeated campaign content, runtime health/speed multipliers, and placeholder tinting
- M7 visual/audio polish: generated gameplay sprites, parallax/chapter backgrounds, combat VFX, 2D lighting, screen feedback, first-pass SFX, and menu/gameplay/boss/game-over music
- M7 UI polish: shop presentation and transition, level start/complete messaging, main/pause/game-over menu flow, keyboard/controller selection handling, final score display, first-run controls hint, UI highlight/click SFX, font cleanup, and HUD rail art
- M7 integration validation: a 10-level playtest confirmed the current polished loop feels stable and presentable for this stage

The current milestone is **M8: Tune and Ship Core**. Opening difficulty, reward/drop/buff tuning, and shop economy balance are complete. The next work is the midgame, boss, and full-campaign balance pass, followed by optimization, release packaging, and broader WebGL/browser validation.

## Target Platform

- **Platform:** WebGL
- **Input:** Keyboard
- **Resolution target:** 1920x1080, 16:9
- **Scope:** Desktop browser only

## Tech Stack

- Unity 6.3 LTS
- Universal Render Pipeline 2D
- Unity New Input System
- C#
- ScriptableObjects for tuning data
- Unity `ObjectPool<T>` for frequently spawned gameplay objects

## Development Approach

The game is being built as a set of playable milestones. Each milestone should end with a working build and a small, visible improvement to the game loop.

The game is now past the placeholder-only and first presentation-pass phase. M8 is focused on making the full 100-level core fair, stable, performant, and release-ready.

## Roadmap

- **M3:** Core wave shooter loop - complete
- **M4:** Pickups, buffs, cash, and shop loop - complete
- **M5:** Boss fights - complete
- **M6:** Content fill - complete
- **M7:** Game feel, art, audio, and UI polish - complete
- **M8:** Tune and ship core - current
- **M9:** Online leaderboard
- **v1.1:** Mini-games

More detailed planning lives in `PLANNING.md` and milestone-specific planning files.
