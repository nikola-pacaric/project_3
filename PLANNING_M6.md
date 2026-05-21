# M6 — Content Fill Phase Plan

## Summary

M6 scales the game from the current five-level vertical slice into the full core campaign structure: 100 authored levels, bosses at levels 25 / 50 / 75 / 100, infinite cycle scaling after level 100, and developer tools for testing the expanded progression.

This milestone is mostly content-model and authoring work. The important risk is not writing 100 assets; it is making sure the content model is safe before authoring those assets. Formation shape/composition separation, level routing, validation, and dev tools happen before the large authoring pass.

M6 follows the same placeholder-first rule as earlier milestones. Final difficulty balance stays in M9. Real art, VFX, lighting polish, particles, audio, and screen shake stay in M8.

Key decisions locked:
- M6 completion means level 100 is reachable through real level flow, not only hardcoded tests.
- Bosses move from M5 test placement into real campaign placement at levels 25 / 50 / 75 / 100.
- Campaign structure follows Warblade-style 25-level chapters:
  - 24 normal levels
  - 1 boss level
  - repeated four times for levels 1-100
- Each 24-level normal chapter is six repeated 4-level enemy-set blocks:
  - block level 1: enemy-set intro
  - block level 2: harder version of the same enemy set
  - block level 3: mother-alien bug encounter
  - block level 4: kamikaze / fast snake bonus-risk wave
- Level 4 of each enemy-set block can award a score bonus if the player kills every kamikaze/fast-moving enemy before they leave.
- Mini-games are deferred to v1.1 so v1.0 can focus on the playable 100-level campaign, polish, tuning, and WebGL stability.
- The 100 levels are first-pass authored and playable, not final balanced.
- Infinite cycles after level 100 must be reconstructible from `currentLevel`.
- No mobile/touch-specific input, UI, or architecture.
- No new dependencies without explicit approval.
- No Unity scene, prefab, ProjectSettings, or other editor-owned YAML edits by Codex unless explicitly requested.

---

## Phase 1 — M6 Baseline and Routing Design

Status: Pending.

Code:
- [x] Audit the current M5 level flow, shop cadence, boss test hook, and level-complete path.
- [x] Decide the runtime routing model before code changes:
  - normal wave level
  - boss level
  - shop transition
- [x] Define how `currentLevel` maps to:
  - boss levels: `currentLevel % 25 == 0`
  - chapter index: `(currentLevel - 1) / 25`
  - chapter level: `((currentLevel - 1) % 25) + 1`
  - enemy-set block index inside chapter: `(chapterLevel - 1) / 4` for chapter levels 1-24
  - block step inside enemy set: `((chapterLevel - 1) % 4) + 1`
  - cycle index: `(currentLevel - 1) / 100`
- [x] Resolve enemy-set rotation mismatch: M6 uses six 4-level blocks per 24 normal levels, not the earlier placeholder `level / 4` or `currentLevel / 3` notes.
- [x] Define block-step behavior:
  - step 1: enemy-set intro
  - step 2: harder version
  - step 3: mother-alien bug encounter
  - step 4: kamikaze / fast snake bonus-risk wave
- [x] Defer mini-game routing to v1.1.
- Keep the existing M5 test boss path until the real boss routing is ready, then remove or demote it to a dev-only helper.

Editor:
- No required Editor work.

Acceptance:
- [x] M6 routing approach is written down before implementation.
- No M5 functionality regresses during the design pass.

---

## Phase 2 — Formation Shape / Composition Refactor

Status: Complete.

Code:
- [x] Inspect current formation pipeline before editing:
  - `FormationData`
  - `Formation`
  - `WaveData`
  - `EnemySpawner`
  - existing `LevelData` assets for levels 1-5
- [x] Identify every place that currently reads enemy data from `FormationData`.
- [x] Refactor `FormationData` into shape-only data:
  - slot positions
  - slot entry control offsets
  - formation breathing/motion settings
  - slot count
- [x] Remove enemy-type ownership from reusable formation shapes.
- [x] Move enemy composition into wave-level data:
  - formation shape reference
  - enemy data per slot
  - spawn timing
  - entry start info
- [x] Decide whether composition should live directly on `WaveData` or in a separate reusable loadout asset.
- [x] Prefer the simpler option unless two concrete waves need to reuse the exact same composition independently.
- [x] Update `EnemySpawner` so it resolves each slot from:
  - shape slot position/control data from `FormationData`
  - enemy type from the wave composition/loadout
- [x] Preserve current Level 1-5 behavior after migration.
- [x] Add validation before mass authoring:
  - wave has missing formation shape
  - wave has empty enemy composition
  - composition count does not match formation slot count
  - any composition slot has missing `EnemyData`
  - old formation assets still contain obsolete enemy ownership if temporary compatibility exists
- [x] Keep migration code temporary and obvious if any compatibility bridge is needed.
- [x] Do not add editor tooling beyond simple validation unless manual migration becomes error-prone.

Editor:
- [x] Back up or inspect the current Level 1-5 intended enemy layouts before changing assets.
- [x] Update reusable formation assets so they are shape-only.
- [x] Update existing wave assets so each wave owns its enemy composition.
- [x] Confirm Level 1-5 waves still use the same intended enemy layouts after migration.
- [x] Fix all validation warnings/errors on the migrated M3/M4/M5 assets.

Acceptance:
- [x] Reusing a V/line/snake/dual-flank shape in a later level no longer changes enemy types in earlier levels.
- [x] Levels 1-5 still play correctly after asset migration.
- [x] A single formation shape can be reused by at least two waves with different enemy compositions.
- [x] Invalid wave composition data produces clear warnings/errors before Play Mode testing.
- [x] No M5 boss/shop/loot behavior is touched by this refactor except where level flow naturally references waves.

Follow-up decision:
- [x] Simplified the active authoring model after editor workflow review: `WaveData` now owns its editable slots directly (`LocalPosition`, `EnemyData`, `EntryControlOffset`). `FormationData` remains available only as an inactive preset/reference concept, not as the runtime source of wave shape data.

---

## Phase 3 — Entry Spawn vs Final Formation Placement

Status: Complete.

Code:
- [x] Separate final formation placement from entry spawn placement where needed.
- [x] Let designers place:
  - where enemies enter from
  - where enemies settle in formation
- [x] Avoid compensating for entry starts through awkward local slot offsets.
- [x] Preserve current simple wave authoring for levels that do not need custom entry starts.

Editor:
- [x] Update existing Level 1-5 waves if the serialized model changes.
- [x] Author at least two test waves with different entry starts and final positions.

Acceptance:
- [x] A side-entry wave can start offscreen and settle into a centered formation without distorting the reusable formation shape.
- [x] Existing waves still work.

Follow-up decision:
- [x] Extended Phase 3 authoring with shared waypoint entry paths, live formation sway, explicit enemy behavior modes, special-wave perfect-clear bonuses, and a standard-enemy dive coordinator. This makes early level authoring more fluent before the larger 100-level pass.

---

## Phase 4 — Content Validation Safeguards

Status: Complete.

Code:
- [x] Add validation/editor safeguards for scalable authoring:
  - missing enemy data
  - empty wave lists
  - duplicate `LevelData.LevelNumber`
  - missing level numbers in the current authored range
  - strict M6 gate check for missing level numbers in the 1-100 range
  - missing boss data coverage for the M6 gate
  - missing boss phases and attack pattern references
  - suspicious WaveData path/motion settings
- [x] Prefer clear warnings/errors in `OnValidate` or editor-only validation helpers.
- [x] Do not add third-party validation packages.

Editor:
- [x] Add `Warblade/Validate Content` for current authored content.
- [x] Add `Warblade/Validate M6 Gate (1-100)` for strict milestone validation.
- [x] Run validation from the Unity Editor against current assets.
- [x] Fix existing validation warnings before mass authoring.

Acceptance:
- [x] Bad level assets fail loudly before Play Mode.
- [x] The project has a practical way to detect missing/duplicate authored levels before M6 gate.

Follow-up note:
- [x] The old missing-formation-data and composition-slot-mismatch checks were replaced by direct `WaveData` slot validation because Phase 2/3 moved active wave authoring away from `FormationData`.

---

## Phase 5 — Real Level Routing and Boss Placement

Status: Complete.

Code:
- [x] Replace the level-5 boss test route with real boss routing:
  - [x] `currentLevel % 25 == 0` means boss encounter
  - [x] levels 25 / 50 / 75 / 100 map through explicit campaign boss routes
- [x] Keep shop cadence compatible with boss levels.
- [x] Ensure level completion, score, pickups, player state, timed buffs, and game over still behave correctly around boss levels.
- [x] Keep the whole game state reconstructible from `currentLevel` + player upgrades + active timed buffs.

Editor:
- [x] Assign boss routing data/references manually on `LevelManager`.
- [x] Confirm the four boss prefabs are available for campaign placement.

Acceptance:
- [x] Levels 25, 50, 75, and 100 run boss encounters through normal level flow.
- [x] Boss test placement at level 5 is no longer part of the normal campaign route.

Validation note:
- [x] Manually playtested by setting `LevelManager.Starting Level` to 25, 50, 75, and 100; all four boss routes started correctly.

---

## Phase 6 — Cycle Scaling

Status: Complete.

Code:
- [x] Implement `cycleIndex = (currentLevel - 1) / 100`.
- [x] Expose `CurrentCycleNumber` as the player-facing cycle number, so level 101 reports cycle 2.
- [x] Resolve repeated cycles from authored campaign content:
  - [x] level 101 resolves to campaign level 1
  - [x] level 125 resolves to the level-25 campaign boss route
  - [x] level 200 resolves to the level-100 campaign boss route
- [x] Apply cycle scaling after level 100:
  - [x] enemy health multiplier
  - [x] enemy speed multiplier
  - [x] boss health multiplier
  - [x] boss movement/projectile pressure multiplier
  - [x] visual tinting for cycle readability
- [x] Keep cycle scaling data-driven where practical through `CycleScalingData`.
- [x] Avoid final balance tuning; this is first-pass functional scaling.

Editor:
- [x] Provide code fallback placeholder tint/scaling values when no `CycleScalingData` asset is assigned.
- [x] Defer creating/assigning a project-specific `CycleScalingData` asset until tuning needs values different from the code fallback.
- [x] Test cycle 2 through dev tools.

Acceptance:
- [x] Level 101 resolves to campaign level 1 as cycle 2 in code.
- [x] Level 101 plays as cycle 2 in Unity Play Mode.
- [x] Difficulty visibly increases.
- [x] Cycle tinting is visible with placeholder visuals.

Validation note:
- [x] Static implementation pass complete.
- [x] Unity Play Mode validation completed by testing the level-101 cycle path.
- [x] Runtime `Enemy(Clone)` scene pollution from out-of-Play-Mode context menu use was cleaned up, and dev helpers are now Play Mode guarded.
- [x] A project-specific `CycleScalingData` asset was intentionally deferred; code fallback values are the current placeholder tuning until a real balance pass needs an asset.

---

## Phase 7 — Developer Progression Tools

Status: Complete.

Scope note:
- Phase 7 is not meant to become a full debug console before the 100-level authoring pass.
- Build only the minimum dev-only helpers needed to make level authoring and validation practical.
- The level-jump pieces already added during Phase 5/6 count toward this phase; add cash grant or kill-all only when the next authoring/testing step actually needs them.

Code:
- Add dev-only tools needed to test the expanded game:
  - [x] level skip / jump to level
  - [x] cash grant deferred until shop/economy testing blocks authoring
  - [x] kill-all active enemies deferred until wave authoring iteration is too slow
  - [x] force boss encounter
- [x] Keep tools out of normal player UI.
- [x] Prefer context menu methods or clearly gated debug UI.

Editor:
- [x] Use the tools to jump to levels 25, 50, 75, 100, and 101.

Acceptance:
- [x] Testing level 100 and cycle 2 does not require manually playing 100 levels.
- [x] Debug tools do not affect normal gameplay unless explicitly invoked.

Validation note:
- [x] `Warblade/Validate Content` passed with 0 warnings.
- [x] Dev boss tools were tested with `LevelManager.Play On Start` disabled; bosses spawned correctly, and defeating each boss completed the matching boss level number.

---

## Phase 8 — 100-Level Authoring Pass

Status: Complete.

Code:
- [x] Add only the code support needed for scalable level authoring.
- [x] Avoid tuning-specific one-off code branches.

Editor:
- [x] Author the full 100-level campaign route: 96 normal `LevelData` assets plus 4 boss encounters.
- [x] Use reusable formation shapes plus wave composition/loadouts.
- [x] Place boss levels at 25, 50, 75, and 100.
- [x] Create first-pass difficulty progression across the full set.
- [x] Keep assets placeholder and readable.

Acceptance:
- [x] Levels 1-100 all resolve through `LevelManager`.
- [x] No missing level numbers.
- [x] No duplicate level numbers.
- [x] Boss levels are present at 25 / 50 / 75 / 100.

Validation note:
- [x] `Warblade/Validate Content` passed with 0 warnings.
- [x] `Warblade/Validate M6 Gate (1-100)` passed with 0 warnings.
- [x] Manually tested chapter boundaries 24 -> 25 -> 26, 49 -> 50 -> 51, 74 -> 75 -> 76, and 99 -> 100 -> 101.
- [x] Pickups spawned, applied correctly, and level completion continued into the next level.
- [x] First-pass content is playable but intentionally not final-balanced; final difficulty and economy tuning remain deferred to M9.

---

## Phase 9 — M6 Gate Cleanup and Validation

Status: Complete.

Code:
- [x] Remove noisy temporary logs.
- [x] Keep useful context-menu debug helpers.
- [x] Fix obvious null-reference and missing-reference warnings.
- [x] Keep placeholder visuals.

Editor:
- [x] Use dev tools to reach level 100.
- [x] Play into cycle 2.
- [x] Test all boss levels.
- [x] Run a WebGL build manually.

Acceptance:
- M6 milestone gate:
  - [x] level 100 is reachable through normal progression or dev tools
  - [x] cycle 2 begins after level 100
  - [x] cycle tinting and increased difficulty are visible
  - [x] bosses appear at 25 / 50 / 75 / 100
  - [x] all 100 `LevelData` assets resolve without missing/duplicate numbers
  - [x] shop/loot/run-state systems still work
  - [x] WebGL build runs in browser

Validation note:
- [x] `Warblade/Validate Content` passed with 0 warnings.
- [x] `Warblade/Validate M6 Gate (1-100)` passed with 0 warnings.
- [x] Boss levels played correctly.
- [x] Level 101 cycle scaling worked after creating and assigning a `CycleScalingData` asset to `LevelManager`; enemies were visibly darker and stronger.
- [x] M6 is complete for now. Final tuning, art, SFX, VFX, leaderboard integration, and mini-games are outside this milestone.

---

## Cut / Defer From M6

- Final difficulty balance stays in M8.
- Final economy/drop-rate tuning stays in M8.
- Real art, generated sprite sheets, VFX, lighting, particles, audio, and screen shake stay in M7.
- Mini-games are deferred to v1.1.
- UGS Authentication and Leaderboards stay in M9.
- Main menu, settings, pause polish, and animated transitions stay in M7.
- Mobile/touch support remains out of scope.
