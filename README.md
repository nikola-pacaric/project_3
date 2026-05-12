# Warblade-style

A 2D arcade space shooter inspired by **Warblade 1.5** and **Galaga**.

The player controls a ship locked to the bottom of the screen, moves left and right, and fights through swooping enemy waves. The long-term goal is a polished WebGL arcade game with hand-tuned levels, bosses, loot drops, a shop loop, mini-games, and online leaderboards.

This is a solo portfolio project focused on clean Unity architecture, responsive arcade gameplay, and steady milestone-based development.

## Current State

The project currently has the core shooter loop in place:

- Player movement and shooting
- Enemy waves, formations, and level progression
- Multiple enemy types
- Object pooling for bullets and enemies
- ScriptableObject-driven enemy, wave, formation, and level data
- Run stats, lives, armour, weapon tiers, and event-channel foundations

The current milestone is **M4: Risk and Reward**, which adds pickups, timed buffs, cash, upgrades, and the shop loop.

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
- **M4:** Pickups, buffs, cash, and shop loop - in progress
- **M5:** Boss fights
- **M6:** Content fill and mini-games
- **M7:** Online leaderboard
- **M8:** Game feel, art, and audio polish
- **M9:** Final tuning and WebGL release pass

More detailed planning lives in `PLANNING.md` and milestone-specific planning files.
