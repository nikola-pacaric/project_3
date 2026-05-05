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

**M4** *(not started)*

---

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

# M4 — Risk and Reward Phase Plan

  ## Summary

  M4 turns the M3 five-level shooter into a run-based loot/economy loop: enemies drop pickups, the player collects cash and upgrades, timed buffs change
  combat feel, suckers create risk, and a shop opens after every 4th level. This follows the Warblade reference: shop every 4th level, S/B/T bars for Speed/
  Bullets/Bonus Time, armour as hit protection, and timed shield/autofire-style bonuses.

  References used:

  - Warblade manual/shop cadence and S/B/T bars: https://warblade.fandom.com/wiki/Game_Manual
  - Warblade bonuses/stat modifiers: https://warblade.fandom.com/wiki/Bonuses
  - Warblade weapons/suckers/life-loss behavior: https://warblade.fandom.com/wiki/Weapons

  Key decisions locked:

  - Shop cadence: after every 4th level.
  - M4 shop form: in-scene overlay/state, not a separate scene, for smoother WebGL and less persistence complexity.
  - Boss/bonus-wave pattern: cadence only in M4; real bosses stay M5, mini-games stay M6.
  - Starting stats: Warblade-like prototype values.
  - Weapon loss: hit without protection loses one life and downgrades weapon one tier, never below single.
  - Buffs: independent timers; picking the same buff refreshes that buff only.
  - Shield: timed invulnerability pickup.
  - Armour: separate max-2 hit buffer, bought/dropped.
  - Sucker: weapon tier down one for the run, plus one random temporary stat debuff for the current level among Speed/Bullets/Time.
  - Shop upgrades: run-only, reset on game over.
  - Drop tables: per enemy type.

  ———

  ## Phase 1 — Run State + Game State Foundation

  Code

  - Add a GameManager singleton with states: Playing, Paused, Shop, GameOver.
  - Add a RunStatsManager singleton for current-run state:
      - cash
      - lives
      - armour count, max 2
      - weapon tier: Single, Double, Triple, Quad
      - Speed level
      - Bullets level
      - Time level
      - temporary current-level stat debuffs from suckers
  - Starting defaults:
      - lives: 3
      - weapon: Single
      - max bullets on screen: 5 + BulletsLevel - temporaryBulletDebuff
      - movement speed: current M3 feel as baseline, then stat-adjusted
      - timed buff duration: base duration plus Time-level bonus
  - Add methods for:
      - AddCash, TrySpendCash
      - AddLife
      - AddArmour
      - SetWeaponTier, UpgradeWeaponTier, DowngradeWeaponTier
      - IncreaseSpeed, IncreaseBullets, IncreaseTime
      - ApplySuckerPenalty
      - ClearCurrentLevelDebuffs
      - ResetRun

  Refactor

  - Move “run reset on game over” responsibility out of scattered UI/level code and into RunStatsManager.ResetRun.
  - Keep ScoreManager separate for now, but stop duplicating score reset logic where practical.

  Editor

  - Add GameManager and RunStatsManager objects to SampleScene.
  - Wire starting values in inspector so they are easy to tune.

  Acceptance

  - Press Play and current M3 gameplay still works.
  - Game over resets cash/upgrades/lives/weapon state.
  - No shop or pickups yet.

  ———

  ## Phase 2 — Event Channels Before More UI Wiring

  Code

  - Add ScriptableObject event channels under Assets/Scripts/Systems/ or Assets/Scripts/Data/Events/:
      - void event channel
      - int event channel
      - game-state event channel
      - buff-state event channel if needed for HUD
  - Use channels for:
      - cash changed
      - lives changed
      - armour changed
      - weapon tier changed
      - Speed/Bullets/Time changed
      - game state changed
      - level started/completed
  - Keep singletons for true global services, but UI should listen to events instead of polling singletons directly where reasonable.

  Refactor

  - Migrate LevelHud, score/cash/lives HUD additions, and future shop UI toward event-channel updates.
  - Do not over-migrate enemy movement/spawning yet; keep M3 gameplay stable.

  Editor

  - Create event channel assets in Assets/ScriptableObjects/EventChannels/.

  Acceptance

  - Existing score and level HUD still update.
  - Changing run stats raises events and can be observed with temporary debug logs or inspector-wired listeners.

  ———

  ## Phase 3 — Stat-Driven Player Shooting

  Code

  - Refactor PlayerShooting so all firing goes through one TryFire() path.
  - Add max active player bullet counting.
  - Bullet cap counts individual projectile instances, not trigger pulls.
  - Weapon patterns:
      - Single: 1 center bullet
      - Double: 2 bullets, slight left/right offset
      - Triple: 3 bullets, left/center/right
      - Quad: 4 bullets, two inner and two outer offsets
  - Fire only if there is enough bullet capacity for the full volley.
  - Add base fire cooldown.
  - Manual fire:
      - one key press attempts one volley.
      - holding does not repeat unless autofire is active.
  - Autofire-active fire:
      - holding fire repeatedly calls the same TryFire() path when cooldown allows.
  - Rapid-fire-active:
      - reduces fire cooldown, still respecting bullet cap.

  Refactor

  - Keep InputReader as-is: it already exposes FireHeld, which is correct for tap and autofire.
  - Player bullet pooling remains in PlayerShooting.

  Editor

  - Tune bullet offsets and cooldown in inspector.
  - Confirm existing bullet prefab still works.

  Acceptance

  - Single-shot tap behavior remains faithful.
  - Double/triple/quad visibly change projectile count.
  - Bullet cap prevents infinite bullet spam.
  - Autofire and rapid fire can be enabled via inspector/debug and work together.

  ———

  ## Phase 4 — Stat-Driven Movement, Damage, Armour, and Shield Hooks

  Code

  - Refactor PlayerMovement to read effective movement speed from RunStatsManager.
  - Refactor PlayerHealth so hit resolution order is:
      1. If timed shield is active, ignore hit.
      2. Else if armour > 0, consume one armour and ignore life loss.
      3. Else lose one life and downgrade weapon tier by one.
      4. If lives reach zero, raise game over and reset run through GameManager.
  - Add current-level stat debuff clearing when a new level starts.

  Refactor

  - PlayerHealth should no longer own an isolated _currentLives that UI cannot share.
  - Keep IDamageable unchanged.

  Editor

  - Add armour/lives HUD placeholders if not already present.

  Acceptance

  - Armour absorbs hits before lives.
  - Shield blocks hits while active.
  - Losing a life downgrades weapon one tier.
  - Sucker-style temporary debuffs clear at next level start.

  ———

  ## Phase 5 — Pickup Data, Pickup Entity, and Pickup Pooling

  Code

  - Add PickupData ScriptableObject.
  - Add pickup effect types:
      - CashSmall
      - CashLarge
      - WeaponUpgrade
      - SpeedUp
      - BulletsUp
      - TimeUp
      - Autofire
      - RapidFire
      - Shield
      - Armour
      - ExtraLife
      - Sucker
  - Add pooled Pickup MonoBehaviour:
      - falls downward
      - despawns below play area
      - applies effect on player trigger
  - Add PickupSpawner system using UnityEngine.Pool.ObjectPool<Pickup>.
  - Use placeholder colored square/icon sprites for now.

  Refactor

  - No enemy drops yet; first validate pickups by placing them manually in the scene.

  Editor

  - Create PickupData assets in Assets/ScriptableObjects/Pickups/.
  - Create a single generic Pickup.prefab.

  Acceptance

  - Manually placed pickups can be collected.
  - Cash, stat upgrades, weapon upgrade, armour, life, and sucker effects all change run state correctly.
  - Pickup instances are pooled.

  ———

  ## Phase 6 — BuffManager and Timed Buff UI

  Code

  - Add BuffManager singleton.
  - Timed buffs:
      - Autofire
      - RapidFire
      - Shield
  - Buff rules:
      - independent timers
      - collecting the same buff refreshes its timer
      - collecting a different buff does not cancel existing buffs
  - Duration rule:
      - base duration comes from serialized buff tuning
      - Time stat adds fixed seconds per Time level
      - temporary Time debuff reduces effective duration while active
  - Expose read APIs:
      - IsAutofireActive
      - IsRapidFireActive
      - IsShieldActive
      - remaining duration per buff

  Refactor

  - PlayerShooting reads autofire/rapid-fire state from BuffManager.
  - PlayerHealth reads shield state from BuffManager.

  Editor

  - Add simple HUD buff indicators with shrinking timer text or bars.
  - Use placeholder labels/icons.

  Acceptance

  - Autofire allows hold-to-fire.
  - Rapid fire increases firing cadence.
  - Shield makes the player invulnerable for its timer.
  - Time stat visibly changes new buff durations.

  ———

  ## Phase 7 — Drop Tables on Enemy Death

  Code

  - Add DropTable ScriptableObject with weighted entries referencing PickupData.
  - Add drop chance and weighted selection logic.
  - Add a DropTable reference to EnemyData.
  - On Enemy.Die, after score award, roll the enemy’s drop table and spawn a pickup at enemy position.
  - Prototype drop generosity:
      - frequent enough that several pickups appear every level.
      - cash drops are common.
      - suckers are uncommon but visible during testing.

  Refactor

  - Keep score award in enemy death for now, but make pickup spawning independent from score.

  Editor

  - Create one DropTable per enemy type:
      - Standard
      - Shooter
      - Kamikaze
  - Assign those tables to the three EnemyData assets.
  - Use prototype weights, not final balance.

  Acceptance

  - Killing enemies produces pickups.
  - Different enemy types can have different drop tendencies.
  - Drops are pooled and do not create clone buildup.

  ———

  ## Phase 8 — Cash HUD, Stat Bars, Lives, Armour, and Weapon HUD

  Code

  - Add HUD views for:
      - cash
      - lives
      - armour count
      - current weapon tier
      - Speed bar
      - Bullets bar
      - Time bar
      - active timed buffs
  - HUD listens to event channels from RunStatsManager and BuffManager.

  Refactor

  - Avoid direct HUD polling of singletons except for initial value sync in Awake/OnEnable.

  Editor

  - Extend the existing Canvas.
  - Use TMP text and simple bars.
  - Placeholder visuals are acceptable; M8 handles polish.

  Acceptance

  - Collecting pickups updates HUD immediately.
  - Sucker penalty is visible.
  - Hit/life/armour changes are visible.
  - HUD remains readable in 1920x1080 WebGL layout.

  ———

  ## Phase 9 — Shop Items and Shop Overlay

  Code

  - Add ShopItem ScriptableObject.
  - Shop item types:
      - Speed upgrade
      - Extra Bullets upgrade
      - Extra Time upgrade
      - Armour
      - Extra Life
      - Weapon upgrade
  - Prototype economy:
      - cash drops: $10 and $50
      - shop prices tuned so the player can usually buy 1-2 useful items at each shop
      - final pricing deferred to M9
  - Add ShopController UI:
      - opens in Shop state
      - displays item grid
      - shows current cash
      - disables unaffordable/maxed items
      - buy button spends cash and applies item
      - leave button returns to gameplay
  - Shop is an in-scene overlay:
      - combat paused
      - enemy/bullet/pickup activity stopped or cleared before entering
      - no separate Unity scene load in M4

  Refactor

  - GameManager owns entering/leaving shop state.
  - LevelManager asks GameManager to enter shop after shop-eligible level completion.

  Editor

  - Create ShopItem assets in Assets/ScriptableObjects/Shop/.
  - Build a simple shop panel under the existing Canvas.
  - Wire item list in inspector.

  Acceptance

  - Shop opens cleanly.
  - Player can buy upgrades.
  - Cash decreases.
  - Stats/weapon/lives/armour update immediately.
  - Leaving shop starts/resumes the next level.

  ———

  ## Phase 10 — Four-Level Shop Cadence

  Code

  - Add serialized shop interval to LevelManager, default 4.
  - After level completion:
      - if CurrentLevel % 4 == 0, enter shop before incrementing/starting next level.
      - leaving shop advances to the next level.
  - Add a debug/context-menu method to force shop entry for testing.
  - Keep real boss levels and bonus waves deferred:
      - M4 does not implement boss-level logic.
      - M4 does not implement meteor/memory mini-games.

  Refactor

  - Ensure level transition, game over, pause, and shop states cannot fight each other.
  - Pause only toggles while in Playing.
  - Game over exits shop/pause state if needed.

  Editor

  - With existing five levels, shop should appear after Level 4.
  - Level 5 should start after leaving the shop.

  Acceptance

  - Play levels 1-4, shop appears, buy something, leave shop, level 5 starts.
  - Game over before shop resets run state.
  - Debug force-shop works for quick testing.

  ———

  ## Phase 11 — M4 Content Pass

  Code

  - No new systems unless a bug appears during tuning.

  Editor

  - Tune pickup drop tables for prototype visibility.
  - Tune shop prices so the first shop is meaningful.
  - Tune buff durations and Time-level scaling.
  - Tune stat caps:
      - Speed cap high enough to feel risky but usable.
      - Bullets cap high enough to make quad shot useful.
      - Time cap high enough to noticeably extend timed buffs.
  - Confirm all three enemy types have assigned drop tables.

  Acceptance

  - Across levels 1-5, the player can:
      - collect cash
      - collect good pickups
      - hit a sucker and feel the penalty
      - use autofire/rapid fire/shield
      - buy shop upgrades after level 4
      - feel bought upgrades in level 5

  ———

  ## Phase 12 — M4 Gate Build and Cleanup

  Code

  - Remove temporary debug logs that are not useful.
  - Keep context-menu/debug force-shop if helpful for development.
  - Fix any obvious null-reference or missing-reference warnings.
  - Do not add art/audio polish beyond placeholders.

  Editor

  - Run a WebGL build.
  - Test in browser at 16:9.
  - Confirm no mobile/touch-specific UI or architecture was added.

  Acceptance

  - M4 milestone gate:
      - Play several levels.
      - Pickups drop from enemies.
      - Cash accumulates.
      - Suckers create risk.
      - Timed buffs work.
      - Armour and shield protect correctly.
      - Shop opens after every 4th level.
      - Buying upgrades changes the next level.
      - Game over resets run-only upgrades and returns to level 1.
      - WebGL build runs.

  ———

  ## Cut / Defer From M4

  - True boss fights stay in M5.
  - Meteor Storm and Memory Match stay in M6.
  - UGS, leaderboard, Cloud Save, and Analytics stay out of M4.
  - Final economy balance stays in M9.
  - Real art/audio polish stays in M8.
  - Permanent profile saving is out of scope; all M4 upgrades are run-only.
  - Full 100-level enemy-set rotation is deferred, but M4 establishes the every-4th-level shop cadence.

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

**Goal:** scale from 5 levels to 100 + infinite. Mostly authoring, after the content model is made scalable.

**Build**
- [ ] `LevelManager` modulo logic: `level % 25 == 0` → boss, `level / 3` → enemy set rotation
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
