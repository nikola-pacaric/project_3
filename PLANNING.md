# PLANNING.md

Roadmap and progress tracker for the **Warblade-style** project.
For project context (architecture, conventions, scope), see `CLAUDE.md`.

---

## How This Plan Works

Vertical-slice milestones, not a phased waterfall. Three rules:

1. **Every milestone ends with a working WebGL build**, played start to finish.
2. **No abstractions without two concrete use cases.** Architecture earns its place by being needed twice.
3. **Placeholders are sacred.** Colored boxes and beeps until M8.

Each milestone has four sections: **Build** (what gets made), **Refactor** (cleanup of earlier work now that we understand it better), **Cut / Defer** (explicitly *not* this milestone), **Acceptance** (what the build at the end demonstrates).

---

## Current Milestone

**M3 — It's a Game** *(not started)*

---

## Open Questions

- [ ] Final game name *(by M9)*
- [ ] Autofire + Rapid Fire interaction: stack, refresh, or replace? *(by M4)*
- [ ] Buff durations — autofire, rapid fire, shield *(by M8)*
- [ ] Starting player stats — speed, fire rate, max bullets, lives *(by M1)*
- [ ] Cycle scaling formula — is +50% health per cycle right? *(by M6)*
- [ ] Boss behavior at level 101+ — repeat with scaling, or true final boss? *(by M5)*
- [ ] Mini-game trigger frequency *(by M6)*
- [ ] Leaderboard scope — single global, or per-cycle bands? *(by M7)*

---

## M0 — It Builds

**Goal:** clean Unity project that compiles to WebGL and runs in a browser.

**Build**
- [x] Install Unity 6.3 LTS, create project from URP 2D template
- [x] Git repo with Unity `.gitignore`, first commit
- [x] Game view set to 1920×1080 reference resolution
- [x] Build target: WebGL
- [x] Drop a sprite into a test scene, run a WebGL build, confirm it loads in the browser

**Cut / Defer:** folder structure, namespaces, asmdefs, extra packages.

**Acceptance:** blank scene with one sprite, opened in a browser via WebGL build output.

---

## M1 — I Can Shoot

**Goal:** prove the core feel. If shooting doesn't feel right with squares, it won't feel right with art.

**Build**
- [x] Player sprite (placeholder), bottom of screen
- [x] Horizontal movement with arrow keys (legacy `Input.GetAxis` is fine — refactored in M2)
- [x] Hard left/right `Mathf.Clamp` boundaries
- [x] Bullet prefab, Ctrl to fire, single-press = single shot
- [x] Bullets self-destruct off-screen
- [x] Hardcoded values everywhere

**Cut / Defer:** pooling, SOs, New Input System, event channels, GameManager, audio, folder structure, namespaces.

**Acceptance:** drive the ship side to side and fire bullets into the void. Decide if it feels good. If not, fix the feel before M2.

---

## M2 — Things Shoot Back

**Goal:** an actual fight loop. Enemies attack, you can die, you can restart.

**Build**
- [x] One enemy type: flies in along a path, sits in formation, occasionally dives, returns
- [x] Enemy bullets + firing logic
- [x] Layer setup: Player, PlayerBullet, Enemy, EnemyBullet (collision matrix configured)
- [x] Player damage, lives counter, death + respawn
- [x] Game Over screen with Restart
- [x] Score counter on enemy kill
- [x] Replace `Input.GetAxis` with **New Input System** — Input Actions asset, `InputReader` SO
- [x] Folder structure under `Assets/Scripts/` per `CLAUDE.md`

**Refactor**
- [x] **Object pooling for bullets** — both player and enemy bullets use `UnityEngine.Pool.ObjectPool<T>`
- [x] Move magic numbers into `[SerializeField]` fields (still no SOs)

**Cut / Defer:** multiple enemy types, fancy Bezier paths, SOs, event channels, formations of many enemies, audio.

**Acceptance:** an enemy flies in, dives at you, you shoot it down. If it kills you, game over. Restart works.

---

## M3 — It's a Game

**Goal:** waves, levels, multiple enemy types. The shape of a real game emerges.

**Build**
- [ ] 2 more enemy types (3 total) — distinct sprites and movement
- [ ] Bezier path utility for entries and dives
- [ ] Sin/Cos formation breathing
- [ ] At least 4 reusable formations (V, line sweep, snake, dual flank)
- [ ] Wave system — a level is a sequence of formations spawning over time
- [ ] 5 hand-tuned levels with progression
- [ ] "Level Complete" → next level transition
- [ ] Score persists across levels; game over → level 1
- [ ] Level number on HUD

**Refactor**
- [ ] **Extract data into ScriptableObjects:** `EnemyData`, `FormationData`, `LevelData`
- [ ] Promote ad-hoc spawning into a real `LevelManager` singleton
- [ ] Pooling extended to enemies

**Cut / Defer:** all 100 levels, boss/modulo logic, cycle scaling, loot, shop, event channels.

**Acceptance:** play through 5 levels of escalating waves with 3 enemy types in 4+ formations.

---

## M4 — Risk and Reward

**Goal:** the loot loop. The thing that makes Warblade Warblade.

**Build**
- [ ] Pickup base class (pooled), `PickupData` SO, `DropTable` SO per enemy
- [ ] Pickups: cash, weapon upgrades (single→double→triple→quad), autofire (timed), rapid fire (timed), shield (timed), extra life, sucker downgrade
- [ ] `BuffManager` for active timed effects
- [ ] Currency system + cash on HUD
- [ ] Shop scene loaded between level chunks
- [ ] `ShopItem` SO, shop UI (grid, cash, buy/leave)
- [ ] Permanent upgrades: extra bullets, ship speed, fire rate, score multiplier, extra life

**Refactor**
- [ ] **Event channels** — too many systems broadcasting state (cash, score, lives, buffs, level). Build SO event channel base classes and migrate direct references.
- [ ] **GameManager state machine** — formalize Playing / Paused / Shop / GameOver. Pause respects state.

**Cut / Defer:** bosses, mini-games, UGS, audio polish.

**Acceptance:** play several levels, dodge suckers, accumulate cash, hit the shop, buy upgrades, feel them next level.

---

## M5 — Boss Fights

**Goal:** the spectacle. One good boss, then duplicate to four.

**Build**
- [ ] `Boss` MonoBehaviour with multi-phase FSM
- [ ] `BossData` SO — health, phases, attack patterns, drops
- [ ] Bullet pattern systems — radial, aimed, sweep (data-driven)
- [ ] First boss prefab + data — get the architecture right here
- [ ] Boss intro flow (warning, entry, name banner)
- [ ] Boss death sequence (long explosion, screen clear, big drops)
- [ ] Test at level 5 (renumber to 25 in M6)
- [ ] Three more boss prefabs + data once architecture is proven

**Refactor**
- [ ] Formalize the enemy FSM if it was ad-hoc — bosses share the pattern
- [ ] Audit collision layers and pool capacities for bullet-storm scenarios

**Cut / Defer:** real level placement (M6), boss-rush modes.

**Acceptance:** play to the test boss. Multi-phase fight. Bullet patterns readable but threatening. Verify all 4 bosses fight differently.

---

## M6 — Content Fill

**Goal:** scale from 5 levels to 100 + infinite. Authoring, not engineering.

**Build**
- [ ] `LevelManager` modulo logic: `level % 25 == 0` → boss, `level / 3` → enemy set rotation
- [ ] All 100 `LevelData` SOs authored — first-pass tuning
- [ ] Bosses placed at levels 25, 50, 75, 100
- [ ] Cycle scaling: `cycle = (currentLevel - 1) / 100` → health/speed multipliers + sprite color tint
- [ ] Mini-games: Meteor Storm (dodge round, gem pickups), Memory Match (flip cards)
- [ ] Mini-game trigger logic in `LevelManager`
- [ ] Mini-game scene flow (enter → play → reward → return)
- [ ] Dev tool: level skip / cash grant / kill-all (you'll need this; build it now)

**Cut / Defer:** final balance pass (M9), real art (M8).

**Acceptance:** reach level 100 via dev tool, play into cycle 2, confirm tinting and increased difficulty. Mini-games trigger and reward correctly.

---

## M7 — Online

**Goal:** leaderboards. The game persists past the game over screen.

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

**Cut / Defer:** Cloud Save, Analytics, social features.

**Acceptance:** play, die, submit, see your score on the board. Confirm in a different browser session.

---

## M8 — Game Feel

**Goal:** juice. Stops looking like a prototype.

**Build — Visual**
- [ ] Parallax starfield (Particle System)
- [ ] Player thruster particles
- [ ] Explosion variants (grunts, bullets, bosses)
- [ ] URP 2D lights on bullets and explosions
- [ ] Player hit flash (UI overlay coroutine)
- [ ] Screen shake via Cinemachine Impulse
- [ ] Bloom + color grading in Global Volume
- [ ] Replace placeholder sprites with generated sprite sheets

**Build — Audio**
- [ ] `AudioManager` singleton, separate SFX/music mixer channels
- [ ] SFX library: shoot, hit, explosion, pickup, shop, UI, boss roar, mini-game, level start
- [ ] Music: menu, gameplay, boss, mini-game

**Build — UI / UX**
- [ ] Main menu (Start, Leaderboard, Settings)
- [ ] Settings (master / SFX / music volume)
- [ ] First-run controls hint overlay
- [ ] Pause menu, animated screen transitions

**Refactor**
- [ ] Final under-the-hood cleanup pass before M9

**Cut / Defer:** difficulty tuning (M9), localization, control rebinding.

**Acceptance:** show a friend without telling them you made it. They should not assume it's a prototype.

---

## M9 — Ship It

**Goal:** the build that goes on a portfolio.

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

**Acceptance:** public URL, starts within 5s, 60fps, plays end-to-end, live leaderboard, no crashes. Linkable in a job application without flinching.

---

## Change Log

- *YYYY-MM-DD* — Plan created.
