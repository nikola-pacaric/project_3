# PLANNING.md

Roadmap and progress tracker for the **Warblade-style** project.
For project context (architecture, conventions, scope), see `AGENTS.md`.

---

## How This Plan Works

Vertical-slice milestones, not a phased waterfall. Three rules:

1. **Every milestone ends with a working WebGL build**, played start to finish.
2. **No abstractions without two concrete use cases.** Architecture earns its place by being needed twice.
3. **Placeholders are sacred through M6.** Colored boxes and beeps until the M7 polish pass.

Completed milestones are kept as short summaries. Active and future milestones keep detailed sections: **Build** (what gets made), **Refactor** (cleanup of earlier work now that we understand it better), **Cut / Defer** (explicitly *not* this milestone), **Acceptance** (what the build at the end demonstrates).

---

## Current Milestone

**M7** *(current: Game Feel)*

M6 Content Fill is complete. M7 Game Feel is active: sprites, background presentation, animation polish, the Phase 4 VFX pass, Phase 5 lighting/post-processing, the Phase 6 background integration check, Phase 7 screen feedback, Phase 8 audio foundation, Phase 9 SFX pass, and Phase 10 music pass are complete. The remaining major work is UI polish and integration checks before final tuning.

---

## M0 — It Builds

M0 established the project foundation: a Unity 6.3 LTS URP 2D project, Git repository, Unity `.gitignore`, 1920x1080 reference Game view, and WebGL build target. A minimal scene was built and loaded in a browser to prove the project could compile and run on the intended platform before any architecture or gameplay work was added.

---

## M1 — I Can Shoot

M1 proved the basic player feel with placeholder visuals: a bottom-locked ship, horizontal movement, clamped screen boundaries, single-press shooting, and bullets that clean themselves up off-screen. Values were intentionally hardcoded so movement and firing could be judged before introducing data assets, pooling, input abstraction, or broader architecture.

---

## M2 — Things Shoot Back

M2 turned the prototype into a playable combat loop. One enemy type could enter, hold formation, dive, shoot, and return; the player could take damage, lose lives, respawn, reach game over, and restart. Scoring, collision layers, the New Input System, the project script folder structure, and bullet pooling with `UnityEngine.Pool.ObjectPool<T>` were introduced, while tunable values moved into serialized fields.

---

## M3 — It's a Game

M3 added the first real level structure. The game gained three enemy types, Bezier entry and dive paths, formation breathing, reusable formation shapes, a wave system, five hand-tuned levels, level-complete transitions, persistent score across levels, and level display in the HUD. Enemy, formation, and level tuning moved into ScriptableObjects, spawning became the responsibility of `LevelManager`, and enemy pooling was added.

---

## M4 — Risk and Reward

M4 built the reward loop. The game gained run state, ScriptableObject event channels, stat-driven shooting and movement, armour and shield hooks, pooled pickups, drop tables, timed buffs, cash rewards, HUD support for stats and buffs, and an in-scene shop after every fourth level. Pickup coverage included cash, weapon tiers, stat upgrades, timed autofire/rapid-fire/shield effects, armour, extra life, and sucker downgrades. The `GameManager` state machine was formalized around Playing, Paused, Shop, and GameOver states, while pickup, buff, HUD, and shop systems were connected through event channels.

Bosses, mini-games, UGS, final economy balance, and art/audio polish were deferred. Deep original-Warblade profile and secret-unlock mechanics remain permanently out of scope for this project version.

---

## M5 — Boss Fights

M5 added boss architecture and the first spectacle layer. Bosses use a `Boss` MonoBehaviour with a multi-phase FSM, `BossData` ScriptableObjects for health/phases/patterns/drops, and data-driven radial, aimed, and sweep bullet patterns. The milestone proved the flow with a test boss, then expanded to four distinct boss prefabs and data sets. Boss intro, boss HUD/name display, boss death cleanup, score/drop rewards, collision-layer auditing, and bullet-pool prewarming were also added; final boss placement and polish were left for later milestones.

---

## M6 — Content Fill

M6 scaled the game from the early level set to the full campaign route. `LevelManager` now handles chapter logic, boss levels at 25/50/75/100, six 4-level enemy-set blocks before each boss, and infinite cycle scaling after level 100 using health/speed multipliers and visual tinting. The campaign route contains 96 normal `LevelData` assets plus 4 boss encounters, with first-pass tuning only.

The content model was refactored for safer authoring: reusable formation shape data was separated from enemy composition, final formation placement was separated from entry spawn placement where needed, and validation/editor safeguards were added for missing data, slot/loadout mismatches, empty waves, missing boss levels, and duplicate or missing level numbers. Final balance moved to M8, real art and presentation polish moved to M7, and mini-games moved to v1.1.

---

## M7 — Game Feel

**Goal:** juice. Stops looking like a prototype.

**Build — Visual**
- [x] Parallax starfield (Particle System)
- [x] Player thruster particles
- [x] Explosion and impact variants (player, enemies, bullets, bosses)
- [x] URP 2D lights on bullets and explosions
- [x] Screen flash for player death, Mother death, and boss death
- [x] Restrained Cinemachine Impulse feedback for major death cues
- [x] Bloom + color grading in Global Volume
- [x] Replace placeholder sprites with generated sprite sheets

**Build — Audio**
- [x] `AudioManager` singleton, separate SFX/music mixer channels
- [x] SFX library: shoot, hit, explosion, pickup, shop, UI, boss roar
- [x] Music: menu, gameplay, boss, game over

Audio status:
- Phase 8 audio foundation is complete.
- Phase 9 SFX pass is complete: player, enemy, Mother enemy, boss, pickup, shop, pause, game-over, restart-button, sector-warp, kamikaze spawn, and bonus-special spawn cues are wired and assigned.
- Kamikaze return and bonus-special despawn/death sounds were intentionally removed because the level mix became too busy.
- Phase 10 music pass is complete: menu, gameplay, boss, and game-over loop tracks are assigned, loop-processed, and routed through the existing `AudioManager` with runtime fades.

**Build — UI / UX**
- [ ] Main menu (Start, Settings)
- [ ] Settings (master / SFX / music volume)
- [ ] First-run controls hint overlay
- [ ] Pause menu, animated screen transitions

**Refactor**
- [ ] Final under-the-hood cleanup pass before tuning

**Cut / Defer:** difficulty tuning (M8), leaderboard integration (M9), mini-game audio/music (v1.1), localization, control rebinding.

**Acceptance:** show a friend without telling them you made it. They should not assume it's a prototype.

---

## M8 — Tune and Ship Core

**Goal:** make the 100-level core game playable, stable, and worth scoring before online services are connected.

**Tuning**
- [ ] First 10 levels difficulty curve (most important for retention)
- [ ] Drop rate balancing
- [ ] Buff duration tuning
- [ ] Shop pricing economy
- [ ] 5+ external playtesters, iterate

**Build Optimization**
- [ ] WebGL Brotli compression
- [ ] Code stripping audit
- [ ] Texture atlas audit
- [ ] Audio compression audit (music streamed, SFX in memory)
- [ ] First-load size measured (target <30 MB)

**Cross-Browser**
- [ ] Chrome, Firefox, Edge, Safari (Safari is the one that breaks)

**Portfolio Package**
- [ ] Hosted build (itch.io or personal site)
- [ ] README with screenshots
- [ ] 30-second gameplay GIF
- [ ] Short postmortem — what worked, what didn't, what you'd change
- [ ] Link added to portfolio site / CV / LinkedIn

**Acceptance:** public URL, starts within 5s, 60fps, plays end-to-end locally, no crashes. Linkable in a job application without flinching.

---

## M9 — Online Leaderboard

**Goal:** add online scoring after the core game is playable, polished, and tuned enough for submitted scores to mean something.

**Build**
- [ ] Install UGS Authentication and Leaderboards packages
- [ ] Link Unity project to Unity Cloud dashboard
- [ ] Initialize UGS on game boot
- [ ] Anonymous Authentication — silent sign-in, persisted player ID
- [ ] `LeaderboardService` wrapper (submit, fetch top N, fetch player rank)
- [ ] Submit score on game over
- [ ] Leaderboard view UI — top entries, player's rank highlighted
- [ ] Offline / network failure handling — cache pending score, retry on next launch
- [ ] Connecting / failed / no-scores UI states

**Refactor**
- [ ] Game over screen — Submit Score / View Leaderboard / Restart
- [ ] Main menu — add Leaderboard entry after the service is working

**Cut / Defer:** Cloud Save, Analytics, social features.

**Acceptance:** play, die, submit, see your score on the board, and confirm in a different browser session.

---

## v1.1 — Mini-Games

**Goal:** add optional bonus rounds after the core 100-level game is playable, polished, tuned, and released.

**Scope**
- [ ] Mini-game flow foundation: trigger, enter, reward, and return to campaign progression
- [ ] Meteor Storm: dodge falling meteors and collect gem rewards
- [ ] Memory Match: flip cards, match pairs, and award a completion reward
- [ ] Mini-game SFX and music hooks

**Cut / Defer:** v1.0 release polish, core tuning, leaderboard integration, and WebGL stability are higher priority.

---

## Change Log

- *YYYY-MM-DD* — Plan created.
