# Warblade-style

A 2D arcade space shooter inspired by **Warblade 1.5** and **Galaga**.

The player controls a ship locked to the bottom of the screen, moves left and right, and fights through swooping enemy waves. The long-term goal is a polished WebGL arcade game with hand-tuned levels, bosses, loot drops, a shop loop, mini-games, and online leaderboards.

This is a solo portfolio project focused on clean Unity architecture, responsive arcade gameplay, and steady milestone-based development.

## Current State

The project currently has the core shooter loop, M4 reward loop, M5 boss architecture, and early M6 content-fill foundations in place:

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

The current milestone is **M7: Game Feel**. M6 Content Fill is complete; detailed M6 phase history lives in `PLANNING_M6.md`.

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

Placeholder art and simple effects are intentional for now. Visual polish, audio polish, and final balance come later, after the systems are stable.

## Roadmap

- **M3:** Core wave shooter loop - complete
- **M4:** Pickups, buffs, cash, and shop loop - complete
- **M5:** Boss fights - complete
- **M6:** Content fill and mini-games - in progress
- **M7:** Online leaderboard
- **M8:** Game feel, art, and audio polish
- **M9:** Final tuning and WebGL release pass

More detailed planning lives in `PLANNING.md` and milestone-specific planning files.
