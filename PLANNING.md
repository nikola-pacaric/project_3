# PLANNING.md

Roadmap and progress tracker for the **Warblade-style** project.
For project context (architecture, conventions, scope), see `AGENTS.md`.

---

## How This Plan Works

Vertical-slice milestones, not a phased waterfall. Three rules:

1. **Every milestone ends with a working WebGL build**, played start to finish.
2. **No abstractions without two concrete use cases.** Architecture earns its place by being needed twice.
3. **Placeholders are sacred.** Colored boxes and beeps until M8.

Each milestone has four sections: **Build** (what gets made), **Refactor** (cleanup of earlier work now that we understand it better), **Cut / Defer** (explicitly *not* this milestone), **Acceptance** (what the build at the end demonstrates).

---

## Current Milestone

**M5** *(in progress)*

Detailed M4 phase tracking lives in `PLANNING_M4.md`. That file is the source of truth for the current Risk and Reward implementation plan.

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
- [x] Folder structure under `Assets/Scripts/` per `AGENTS.md`

**Refactor**
- [x] **Object pooling for bullets** — both player and enemy bullets use `UnityEngine.Pool.ObjectPool<T>`
- [x] Move magic numbers into `[SerializeField]` fields (still no SOs)

**Cut / Defer:** multiple enemy types, fancy Bezier paths, SOs, event channels, formations of many enemies, audio.

**Acceptance:** an enemy flies in, dives at you, you shoot it down. If it kills you, game over. Restart works.

---

## M3 — It's a Game

**Goal:** waves, levels, multiple enemy types. The shape of a real game emerges.

**Build**
- [x] 2 more enemy types (3 total) — distinct sprites and movement
- [x] Bezier path utility for entries and dives
- [x] Sin/Cos formation breathing
- [x] At least 4 reusable formations (V, line sweep, snake, dual flank)
- [x] Wave system — a level is a sequence of formations spawning over time
- [x] 5 hand-tuned levels with progression
- [x] "Level Complete" → next level transition
- [x] Score persists across levels; game over → level 1
- [x] Level number on HUD

**Refactor**
- [x] **Extract data into ScriptableObjects:** `EnemyData`, `FormationData`, `LevelData`
- [x] Promote ad-hoc spawning into a real `LevelManager` singleton
- [x] Pooling extended to enemies

**Cut / Defer:** all 100 levels, boss/modulo logic, cycle scaling, loot, shop, event channels.

**Acceptance:** play through 5 levels of escalating waves with 3 enemy types in 4+ formations.

---

## M4 — Risk and Reward

**Goal:** the loot loop. The thing that makes Warblade Warblade.

**Status**
- [x] Phase 1: Run state and GameManager state foundation
- [x] Phase 2: ScriptableObject event channels
- [x] Phase 3: Stat-driven player shooting
- [x] Phase 4: Stat-driven movement, damage, armour, and shield hooks
- [x] Phase 5: Enemy drop pickups
- [x] Phase 6: BuffManager and timed buff HUD foundation
- [x] Phase 7: Drop table tuning pass
- [x] Phase 8: Cash, stat, lives, armour, weapon, and buff HUD
- [x] Phase 9: Shop items and shop overlay
- [x] Phase 10: Four-level shop cadence
- [x] Phase 11: M4 content pass

**Build**
- [x] Pickup base class (pooled), `PickupData` SO, shared basic-alien `DropTable` SO, enemy-death drop path
- [x] Pickups: $10/$50/$100/$200 cash, exact weapon pickups (Single/Double/Triple/Quad), Speed/Bullets/Time upgrades, autofire (timed), rapid fire (timed), shield (timed), armour, extra life, red/green/blue sucker downgrades
- [x] `BuffManager` for active timed effects
- [x] Currency system + cash/stat HUD
- [x] In-scene shop overlay after every 4th level
- [x] `ShopItem` SO, shop UI (grid, cash, buy/leave)
- [x] Run-only shop upgrades: Speed, Bullets, Time, armour, extra life, weapon tier, timed Autofire
- [x] Weapon tier purchases only change shot pattern; duplicate active-tier weapon pickups still convert into +1 Bullets

**Refactor**
- [x] **Event channels** — managers now broadcast state changes through SO event channels for HUD/shop/pickup/buff wiring.
- [x] **GameManager state machine** — formalized Playing / Paused / Shop / GameOver states.
- [x] Connect pickup, buff, HUD, and shop systems to the completed run-state/event-channel foundation.

**Cut / Defer:** bosses, mini-games, UGS, final economy balance, art/audio polish.

**Permanently Out of Scope:** Warblade-style shop profile saves, Clear Shields / God Badge reset economy, purchasable secrets, rank-marker secret chains, Alien Lock, Rocket Pack, Super Autofire, and other deep original-Warblade profile/secret unlock mechanics.

**Acceptance:** play several levels, collect pickups, dodge suckers, accumulate cash, hit the shop after level 4, buy upgrades, and feel them in level 5.

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

**Goal:** scale from 5 levels to 100 + infinite. Mostly authoring, after the content model is made scalable.

**Build**
- [ ] `LevelManager` modulo logic: `level % 25 == 0` → boss, `level / 4` → enemy set rotation
- [ ] All 100 `LevelData` SOs authored — first-pass tuning
- [ ] Bosses placed at levels 25, 50, 75, 100
- [ ] Cycle scaling: `cycle = (currentLevel - 1) / 100` → health/speed multipliers + sprite color tint
- [ ] Mini-games: Meteor Storm (dodge round, gem pickups), Memory Match (flip cards)
- [ ] Mini-game trigger logic in `LevelManager`
- [ ] Mini-game scene flow (enter → play → reward → return)
- [ ] Dev tool: level skip / cash grant / kill-all (you'll need this; build it now)

**Refactor**
- [ ] **Split formation shape from enemy composition before authoring all 100 levels.** Current M3 `FormationData` stores both slot layout/path data and `EnemyData` per slot, which makes reusable shapes unsafe: changing a V formation for Level 31 would also change Level 1. Refactor so reusable formation data owns the shape only — slot positions, entry control offsets, breathing — while each wave or a separate loadout/composition asset owns the enemy types for those slots.
- [ ] **Separate final formation placement from entry spawn placement if needed for Warblade-style paths.** Current `WaveData` anchor affects both final slot positions and entry start center. For hand-authored side dives, the designer should be able to place "where enemies start" and "where they settle" independently instead of compensating with local slot offsets.
- [ ] Add validation/editor safeguards for scalable level authoring: warn on missing enemy data, slot/loadout count mismatches, empty wave lists, missing boss levels, and duplicate/missing `LevelData` numbers.

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
