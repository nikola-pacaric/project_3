# Warblade-style

A 2D arcade space shooter inspired by **Warblade 1.5** and **Galaga**.

The player controls a ship locked to the bottom of the screen, moves left and right, and fights through swooping enemy waves. The long-term goal is a polished WebGL arcade game with hand-tuned levels, bosses, loot drops, a shop loop, mini-games, and online leaderboards.

This is a solo portfolio project focused on clean Unity architecture, responsive arcade gameplay, and steady milestone-based development.

## Current State

The project currently has the core shooter loop and M4 reward loop in place:

- Player movement and shooting
- Enemy waves, formations, and level progression
- Multiple enemy types
- Object pooling for bullets and enemies
- ScriptableObject-driven enemy, wave, formation, and level data
- Run stats, lives, armour, weapon tiers, and event-channel foundations
- Pickup drops, timed buffs, cash rewards, stat upgrades, and the shop overlay

The current milestone is **M5: Boss Fights**, which adds the first reusable boss architecture and a level-5 test boss encounter.

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
- **M5:** Boss fights - in progress
- **M6:** Content fill and mini-games
- **M7:** Online leaderboard
- **M8:** Game feel, art, and audio polish
- **M9:** Final tuning and WebGL release pass

More detailed planning lives in `PLANNING.md` and milestone-specific planning files.
