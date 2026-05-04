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

---

## Current work

    Here's the plan. I'm sequencing this around the **"no abstraction without two concrete use cases"** rule from PLANNING.md, so SO refactors come *after* we have multiple consumers — not before.

    ## Strategy

    The risky part of M3 isn't the wave system — it's that the *Enemy* class is currently one monolithic script with all behavior hardcoded. If I extract `EnemyData` SO right now with one consumer, I'll guess wrong about what fields belong on it. So we add a second enemy type the dumb way first, *then* extract the SO with both use cases in front of us. Same logic for formations.

    Order: utilities → pooling refactor → second enemy → extract SOs → formations → waves → LevelManager → polish.

    ## Phases

    ### Phase 1 — Bezier path utility
    **Code:** new `Systems/BezierPath.cs` static helper — `EvaluateQuadratic(p0, p1, p2, t)` and `EvaluateCubic(p0, p1, p2, p3, t)`. Pure math, no MonoBehaviour.
    **Refactor:** rewrite the current `Enemy.Entering` state to follow a quadratic Bezier from off-screen to formation slot instead of whatever lerp it uses now.
    **Editor:** none.
    **Acceptance:** existing enemy enters along a smooth curved path instead of a straight line. Game still plays identically otherwise.

    ### Phase 2 — Enemy pooling
    **Code:** mirror what we did for bullets. `Enemy` gets `SetPool` / `Spawn(...)` / `ReturnToPool`. A new `Systems/EnemySpawner` owns an `ObjectPool<Enemy>` and exposes `Spawn(formationSlot, entryPath)`.
    **Editor:** create `EnemySpawner` GameObject in scene, drag Enemy prefab into its slot. Remove any direct `Instantiate` of enemies from current code.
    **Acceptance:** play, kill enemies, watch the Hierarchy — pool reuses the same Enemy instances; no `(Clone)` count explosion.

    ### Phase 3 — Add a second enemy type (hardcoded)
    **Code:** copy `Enemy.cs` to `Enemy.cs` variant — actually, stay in one class but add a `_kind` enum field and branch on it for the differences (e.g. Type B doesn't sit in formation, dive-only kamikaze). Goal: make the *differences* visible so the SO extraction in Phase 4 has real data to model.
    **Editor:** duplicate Enemy prefab → "EnemyKamikaze" prefab. Different placeholder color (red vs original green/whatever). Wire it into `EnemySpawner` as a second prefab slot temporarily.
    **Acceptance:** two enemy types visibly behave differently in-game.

    ### Phase 4 — Extract `EnemyData` SO
    **Code:** new `Data/EnemyData.cs` with `[CreateAssetMenu]`. Fields: speed, maxHealth, fireCooldownMin/Max, diveCooldownMin/Max, diveBottomY, scoreValue, sprite color, behavior flags (sitsInFormation, canFire, divesOnPassThrough). `Enemy.cs` reads everything from a serialized `_data` reference; `_kind` enum goes away.
    **Editor:** create two SO assets in `Assets/ScriptableObjects/Enemies/` — `EnemyData_Standard` and `EnemyData_Kamikaze`. Wire each into the matching prefab.
    **Acceptance:** game plays identically to Phase 3, but every tuning value lives in the SO. Confirm by editing speed in SO at runtime — see it apply on next spawn.

    ### Phase 5 — Third enemy type, no new code
    **Editor only:** create `EnemyData_Shooter.asset` (different stats — stays in formation, fires more, no dive). Make a third prefab variant with that data. Add to spawner.
    **Acceptance:** three enemy types exist. The fact that this is data-only authoring is the validation that Phase 4 worked.

    ### Phase 6 — Formation system + Sin/Cos breathing
    **Code:** `Systems/Formation.cs` MonoBehaviour. Holds an array of `FormationSlot` (local position offsets). Drives a `_breatheAmplitude * Sin(time * _breatheSpeed)` offset on its transform every frame. Enemies in formation look up their slot's *world* position each frame (slot position + formation transform position + breathing offset).
    **Editor:** prefab a Formation GameObject. Test by hand-spawning 5 enemies into one formation in the scene.
    **Acceptance:** formation visibly breathes side-to-side; enemies move with it as a group.

    ### Phase 7 — `FormationData` SO + four formation shapes
    **Code:** `Data/FormationData.cs` SO — slot positions array, breathing amp/speed, entry-path control points (or a path style enum), enemy-type-per-slot list (references `EnemyData`). `Formation` MonoBehaviour reads this on spawn.
    **Editor:** author four `FormationData` assets — V, line sweep, snake, dual flank. Hand-place slot positions in inspector.
    **Acceptance:** spawn each of the four formations one at a time via temporary debug button. They look distinct, breathe correctly, enemies enter along defined Bezier paths.




    ### Phase 8 — Wave system
    **Code:** `Data/WaveData.cs` SO — references a `FormationData` + spawn delay (seconds after previous wave) + entry side (left/right/top). `Systems/WaveRunner.cs` MonoBehaviour takes a `List<WaveData>` and coroutine-spawns them in order.
    **Editor:** wire a temporary test list of 3 waves into `WaveRunner` in the scene. Press Play — watch the level fill with enemies wave by wave.
    **Acceptance:** sequenced enemy waves appear over time, not all at once.

    ### Phase 9 — `LevelData` SO + `LevelManager` singleton
    **Code:** `Data/LevelData.cs` SO — list of `WaveData` + level number + (optional) tuning multipliers. `Managers/LevelManager.cs` singleton — holds `_currentLevel`, loads `LevelData[]` array, plays current level via `WaveRunner`, detects "all enemies dead and all waves spawned → level complete → load next." Game over resets to level 1. Score (already in `ScoreManager`) persists across level transitions but resets on game over.
    **Editor:** create `Assets/ScriptableObjects/Levels/`. Add `LevelManager` GameObject to scene with `LevelData` array slot.
    **Acceptance:** play one level end-to-end, see it advance to the next.

    ### Phase 10 — Level Complete UI + Level HUD
    **Code:** small `UI/LevelCompleteScreen.cs` — listens for level-complete signal from `LevelManager` (UnityEvent for now; event channels are M4), shows banner for ~2s, fades out. `UI/LevelHud.cs` — TMP text bound to `LevelManager.CurrentLevel`.
    **Editor:** add to Canvas. Wire UnityEvents.
    **Acceptance:** beat a level → see banner → next level starts → HUD updates "Level 2."

    ### Phase 11 — Author 5 hand-tuned levels
    **Editor only:** create five `LevelData` assets. Tune progression — Level 1: one easy formation. Level 5: multiple overlapping waves, mix of all three enemy types, tighter spacing.
    **Acceptance:** play levels 1→5 start to finish. Difficulty progresses. Game over loops back to Level 1. **WebGL build runs.** This is the M3 milestone gate.

    ---

    ## A few notes before we start

    - **Open question to settle on the way:** PLANNING.md still flags "Starting player stats" as overdue from M1. Worth a 5-min tuning pass somewhere mid-M3 once we see how the new enemy types feel against the player.
    - **Event channels stay deferred to M4** even though we'll have several systems broadcasting state by Phase 10. Per PLANNING — direct UnityEvents and singleton refs are fine for now, migrate when the count of broadcasters justifies the abstraction.
    - **No commit per phase unless you ask** — I'll suggest commits at sensible checkpoints (probably end of Phase 5, Phase 7, Phase 9, and the M3 close).

    Want to kick off **Phase 1 (Bezier path utility)** now? I'd want to peek at the current `Enemy.cs` first to see what its Entering state looks like before refactoring it.

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
