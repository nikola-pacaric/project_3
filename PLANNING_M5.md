# M5 — Boss Fights Phase Plan

## Summary

M5 adds the first real boss-fight architecture to the project. The goal is to build one strong, reusable boss system, prove it with the first playable boss after the current level-5 slice, then author three additional boss prefabs/data sets that reuse the same architecture.

M5 follows the same placeholder-first rule as earlier milestones. Visual polish, real sprite sheets, audio, explosions, screen shake, and final VFX stay in M8.

Key decisions locked:
- M5 completion means four boss prefabs/data sets exist and can be fought, with the first boss used for level-5 slice testing while the architecture is proven.
- First boss style: classic Galaga-like readable spectacle, not bullet-hell density.
- Boss test placement: after level 5 for now.
- Real boss placement at levels 25 / 50 / 75 / 100 stays in M6.
- Bosses 2-4 are M5 content, but final level routing for them is M6.
- Bosses are separate from future mother aliens / big aliens.
- No Unity scene, prefab, ProjectSettings, or build-file editing by Codex unless explicitly requested.

---

## Phase 1 — Boss Data Model

Status: Complete.

Code:
- Add `BossData` ScriptableObject for boss-level tuning:
  - display name
  - max health
  - score value
  - entry start / target positions
  - entry speed
  - contact damage
  - phase list
  - optional reward drop table
- Add `BossPhaseData` ScriptableObject or serializable phase data:
  - phase name
  - health threshold
  - movement behavior
  - attack pattern list
  - attack cooldown range
- Add `BossAttackPatternData` ScriptableObject:
  - pattern type: aimed, radial, sweep
  - bullet count
  - spread angle
  - pattern duration / interval
  - bullet direction settings

Editor:
- Create placeholder data assets for the first boss.

Acceptance:
- Boss tuning values live in ScriptableObjects, not hardcoded in runtime behavior.

---

## Phase 2 — Boss Runtime and Health

Status: Complete.

Code:
- Add `Boss` MonoBehaviour in `Assets/Scripts/Entities`.
- Boss implements `IDamageable`.
- Boss owns:
  - entry movement
  - current health
  - current phase
  - phase switching by health threshold
  - death flow
- Add public/read-only accessors for current health, max health, and current phase.
- Add UnityEvents or ScriptableObject event channels for:
  - boss spawned
  - boss health changed
  - boss phase changed
  - boss defeated

Editor:
- Create a placeholder boss GameObject/prefab manually.
- Add a large colored placeholder sprite/shape and collider.
- Assign `BossData`.

Acceptance:
- Boss enters from offscreen/top into its arena position.
- Player bullets damage the boss.
- Boss switches phases as health drops.
- Boss death can be detected by level flow/UI.

---

## Phase 3 — Boss Bullet Patterns

Status: Complete.

Code:
- Add boss bullet spawning with `UnityEngine.Pool.ObjectPool<Bullet>`.
- Reuse the existing `Bullet` behavior where possible.
- Implement first-pass attack patterns:
  - aimed shot toward the player
  - radial burst
  - horizontal or angled sweep
- Keep projectile density readable for WebGL and current placeholder visuals.
- Do not add third-party dependencies.

Editor:
- Assign existing enemy bullet prefab or a manually created boss bullet prefab.
- Tune pool sizes from the Inspector.

Acceptance:
- Boss can fire all three pattern types.
- Boss bullets damage the player.
- Shield, armour, and lives work during the boss fight.
- Bullet pooling prevents hot-path Instantiate/Destroy spam.

---

## Phase 4 — Boss HUD, Intro, Death, and Rewards

Status: Complete.

Code:
- Add a simple boss HUD:
  - boss name
  - health bar or health text
- Add a simple warning/name intro flow before boss attacks begin.
- Add death flow:
  - stop boss attacks
  - despawn/disable boss
  - award score
  - optionally drop reward pickups through existing pickup/drop systems
- Keep effects as placeholders.

Editor:
- Add boss HUD text/bar manually under the existing Canvas.
- Wire boss events or references in the Inspector.

Acceptance:
- Boss fight has clear start, readable health feedback, and clean defeat.
- Defeating the boss gives score/reward feedback.

---

## Phase 5 — Level-5 Test Integration

Status: Complete.

Code:
- Extend level flow so the first boss can be tested after level 5 waves clear.
- Add serialized optional boss test configuration to `LevelManager` or a small boss encounter runner.
- Keep this as test placement only.
- Do not implement `currentLevel % 25 == 0` boss routing yet.
- Preserve M4 shop cadence:
  - shop after level 4
  - level 5 starts after leaving shop
  - boss appears after level 5 waves

Editor:
- Assign the test boss manually.
- Confirm level 5 waves still run before the boss.

Acceptance:
- Play levels 1-4.
- Shop opens after level 4.
- Leave shop and play level 5.
- Boss appears after level 5 waves clear.
- Defeating boss completes the current test slice.

---

## Phase 6 — Boss Variants 2-4

Status: Complete.

Code:
- [x] Reuse the same `Boss`, `BossData`, phase, and attack-pattern systems from boss 1.
- [x] Add any missing data hooks only if a second concrete boss needs them.
- Avoid one-off runtime branches for specific bosses unless the behavior cannot be represented cleanly through data.
- [x] Keep all new tuning in ScriptableObjects.
- [x] Add placeholder `BossData` assets for Sentinel Sweeper, Orbital Core, and Dread Comet.
- [x] Add reusable attack-pattern assets for faster aimed shots, wider sweeps, forward arcs, and denser radial bursts.
- [x] Implement the existing phase `MovementBehavior` data at runtime so boss variants can differ by movement style.

Editor:
- [x] Create three additional placeholder boss prefabs manually.
- [x] Assign the authored `BossData` assets to those prefabs manually.
- [x] Give each boss a distinct readable identity through data:
  - different phase thresholds
  - different movement behavior emphasis
  - different aimed/radial/sweep pattern mixes
  - different cooldowns, bullet counts, spread angles, and bullet speeds

Acceptance:
- [x] Bosses 1-4 can each be spawned/tested manually.
- [x] Each boss fights differently enough to justify being separate content for M5 placeholder validation.
- [x] No boss-specific prefab or scene wiring breaks the reusable boss architecture.

Notes:
- Boss attacks and movement are intentionally still first-pass placeholder tuning. Deeper tuning belongs in M6 content pass and M8 game-feel polish.

---

## Phase 7 — M5 Refactor and Bullet-Storm Audit

Status: Complete.

Code:
- [x] Formalize the enemy FSM if current enemy behavior is too ad-hoc and bosses can share a clearer pattern.
- [x] Audit collision layers for boss, boss bullets, player bullets, pickups, and existing enemies.
- [x] Audit bullet and pickup pool capacities for boss-fight bursts.
- [x] Remove any hot-path `Instantiate` / `Destroy` introduced during boss work.

Editor:
- [x] Review layer assignments on boss and boss-bullet prefabs manually.
- [x] Tune pool capacities in the Inspector after Play Mode testing.

Acceptance:
- [x] Boss fights do not create collision leaks or friendly-fire mistakes.
- [x] Bullet bursts remain pooled and readable.
- [x] The enemy/boss state structure is understandable before M6 content scaling.

Notes:
- Collision matrix is correct for M5: player bullets hit enemy-layer targets, enemy/boss bullets hit the player, pickups only hit the player, and bullets do not collide with each other.
- The existing enemy enum FSM is not worth abstracting into a shared enemy/boss framework yet. Bosses already have their own explicit encounter FSM, and forcing a shared base now would add complexity before M6 proves a second need.
- Added pool prewarming for player bullets, enemy instances, enemy bullets, boss bullets, and pickups so object creation happens before active combat instead of on the first burst/drop.

---

## Phase 8 — M5 Gate Cleanup and Validation

Status: Complete.

Code:
- [x] Remove noisy temporary logs.
- [x] Keep useful context-menu debug helpers if they help development.
- [x] Fix obvious null-reference and missing-reference warnings.
- [x] Keep placeholder visuals.

Editor:
- [x] Run Play Mode validation manually.
- [x] Run WebGL build manually when ready.
- [x] Confirm no mobile/touch-specific UI or architecture was added.

Acceptance:
- M5 milestone gate:
  - [x] boss 1 enters cleanly after level 5 waves for the test slice
  - [x] bosses 1-4 can each be test-spawned/fought
  - [x] every boss takes damage from all current weapon tiers
  - [x] every boss changes phases
  - [x] aimed/radial/sweep attacks work across the boss set
  - [x] player can die during boss fight
  - [x] shield and armour protect correctly
  - [x] boss defeat ends the encounter cleanly
  - [x] existing shop/loot/run-state systems still work
  - [x] all 4 bosses fight differently

Notes:
- Code-side validation passes: `dotnet build project_3.sln` succeeds with 0 warnings and 0 errors.
- Log audit found useful missing-reference/data guardrails, not temporary spam that should be removed.
- Context-menu helpers remain because they support M5/M6 development testing.
- Static search found no mobile/touch input code in `Assets/Scripts`.
- Editor Play Mode and WebGL validation passed manually.

---

## Cut / Defer From M5

- Boss placement at levels 25 / 50 / 75 / 100 stays in M6.
- Final boss routing and campaign placement stay in M6.
- Mother aliens / big aliens stay in M6 content work.
- Mini-games stay in M6.
- Final art, sprite sheets, VFX, lighting, particles, audio, and screen shake stay in M8.
- Final boss balance stays in M9.
- Boss rush modes are out of scope.
