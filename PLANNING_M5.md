# M5 — Boss Fights Phase Plan

## Summary

M5 adds the first real boss-fight architecture to the project. The goal is not to author all final bosses yet; the goal is to build one strong, reusable boss system and one playable first boss that appears after the current level-5 slice for testing.

M5 follows the same placeholder-first rule as earlier milestones. Visual polish, real sprite sheets, audio, explosions, screen shake, and final VFX stay in M8.

Key decisions locked:
- M5 completion means one reusable first boss, not all four final bosses.
- First boss style: classic Galaga-like readable spectacle, not bullet-hell density.
- Boss test placement: after level 5 for now.
- Real boss placement at levels 25 / 50 / 75 / 100 stays in M6.
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

## Phase 6 — M5 Gate Cleanup and Validation

Status: Complete.

Code:
- Remove noisy temporary logs.
- Keep useful context-menu debug helpers if they help development.
- Fix obvious null-reference and missing-reference warnings.
- Keep placeholder visuals.

Editor:
- Run Play Mode validation manually.
- Run WebGL build manually when ready.
- Confirm no mobile/touch-specific UI or architecture was added.

Acceptance:
- M5 milestone gate:
  - first boss enters cleanly
  - boss takes damage from all current weapon tiers
  - boss changes phases
  - aimed/radial/sweep attacks work
  - player can die during boss fight
  - shield and armour protect correctly
  - boss defeat ends the encounter cleanly
  - existing shop/loot/run-state systems still work

---

## Cut / Defer From M5

- Boss placement at levels 25 / 50 / 75 / 100 stays in M6.
- Bosses 2-4 stay deferred until the first boss architecture is proven.
- Mother aliens / big aliens stay in M6 content work.
- Mini-games stay in M6.
- Final art, sprite sheets, VFX, lighting, particles, audio, and screen shake stay in M8.
- Final boss balance stays in M9.
- Boss rush modes are out of scope.
