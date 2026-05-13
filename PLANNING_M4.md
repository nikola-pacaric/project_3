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
  - Weapon pickups: enemy drops equip exact weapon tiers (Single/Double/Triple/Quad); collecting the same weapon tier grants +1 Bullets.
  - Buffs: independent timers; picking the same buff refreshes that buff only.
  - Shield: timed invulnerability pickup.
  - Armour: separate max-2 hit buffer, bought/dropped.
  - Suckers: red/green/blue variants downgrade weapon one tier and apply a specific current-level debuff to Speed/Time/Bullets.
  - Shop upgrades: run-only, reset on game over.
  - Drop tables: per enemy type.
  ———

  ## Phase 1 — Run State + Game State Foundation — Done
  Implemented the M4 state foundation without changing visible M3 gameplay.

  Done:
  - Added GameManager with Playing, Paused, Shop, and GameOver states.
  - Added RunStatsManager for run-only cash, lives, armour, weapon tier, S/B/T levels, and temporary sucker debuffs.
  - Added GameState, WeaponTier, and RunStatType enums.
  - Refactored PlayerHealth so hits now use RunStatsManager when present: armour absorbs first, unprotected hits downgrade weapon and remove a life, and game over routes through GameManager.
  - Refactored GameOverScreen restart to use GameManager when present.
  - LevelManager now clears current-level sucker debuffs when a new level starts.

  Deferred:
  - B/S/T values are stored only; gameplay formulas arrive in later shooting, movement, and buff phases.
  - Shop, pickups, drop tables, timed buffs, and event channels remain later M4 phases.


  ## Phase 2 — Event Channels Before More UI Wiring — Done
  Added ScriptableObject event channels so M4 HUD, shop, pickup, and buff systems can react to state changes without polling managers.

  Done:
  - Added void, int, game-state, and weapon-tier event channel ScriptableObjects.
  - Added matching listener components for inspector-wired UnityEvent responses.
  - GameManager now raises game-state changes.
  - RunStatsManager now raises cash, lives, armour, weapon tier, effective S/B/T level, and run-reset events.
  - LevelManager now raises level-started and level-completed channels while preserving its existing UnityEvents.
  - LevelHud and LevelCompleteScreen can listen to event channels when assigned, with fallback to the existing LevelManager path.

  Deferred:
  - Score HUD stays on the current direct ScoreManager path until the broader HUD pass.
  - Buff-specific event channels wait until BuffManager exists.


  ## Phase 3 — Stat-Driven Player Shooting — Done
  Refactored player shooting so weapon tier, bullet cap, cooldown, and future autofire/rapid-fire all share one firing path.

  Done:
  - PlayerShooting now fires through TryFire().
  - Weapon tier comes from RunStatsManager.
  - Bullet cap counts individual projectile instances, not trigger pulls.
  - Full volleys are required: no partial double/triple/quad shots when there is not enough bullet capacity.
  - Base bullet cap stays Warblade-like at 5; weapon upgrade pickups/shop purchases later grant +1 Bullets when advancing tier.
  - Added base cooldown, debug autofire, and debug rapid-fire hooks.
  - Added patterns:
      - Single: one straight bullet
      - Double: two separated straight bullets
      - Triple: center straight bullet plus angled side bullets
      - Quad: four separated bullets with slight outward angle on the outer bullets
  - Bullet.Spawn now accepts a direction and rotates the bullet visual to match travel direction.
  - Added an Enemy despawn guard so multi-shot volleys cannot double-release the same pooled enemy.


  ## Phase 4 — Stat-Driven Movement, Damage, Armour, and Shield Hooks — Done
  Connected movement, damage, armour, shield blocking, and run-stat HUD placeholders to the M4 run-stat foundation.

  Done:
  - PlayerMovement now reads effective Speed level from RunStatsManager.
  - PlayerHealth now resolves hits through shared run stats: shield blocks first, armour absorbs next, unprotected hits downgrade weapon and remove one life, and zero lives routes through GameManager game over.
  - Removed PlayerHealth's isolated fallback lives state so lives are owned by RunStatsManager.
  - Added a shield-active hook on RunStatsManager for the later timed shield buff, plus a temporary debug shield toggle for Play Mode testing.
  - Added a reusable RunStatHud for lives, armour, cash, and S/B/T placeholder text.
  - Current-level sucker debuffs already clear on level start through LevelManager.


  ## Phase 5 — Enemy Drop Pickups — Done
  Implemented enemy-dropped pickups and the first M4 loot pass.

  Done:
  - Added PickupData ScriptableObject and PickupEffectType values for cash, exact weapon pickups, S/B/T stat upgrades, armour, extra life, timed buff placeholders, and specific sucker variants.
  - Added DropTable ScriptableObject with drop chance and weighted pickup entries.
  - Added Pickup MonoBehaviour: falls downward, despawns below the play area, applies effects on player trigger, and returns to its pool.
  - Added PickupDropPool using UnityEngine.Pool.ObjectPool<Pickup>.
  - Added DropTable references to EnemyData.
  - Enemy.Die now awards score, rolls the enemy's drop table, and releases a pickup at the enemy position when the roll succeeds.
  - Added RunStatsManager.EquipWeaponTierFromPickup() so exact weapon pickups equip that tier, while collecting the matching current weapon grants +1 Bullets.
  - Added specific sucker handling so sucker pickups can downgrade the weapon and apply deterministic Speed/Bullets/Time penalties.
  - Created PickupData assets in Assets/ScriptableObjects/Pickups/.
  - Created the shared Basic Alien DropTable asset in Assets/ScriptableObjects/DropTables/.
  - Created the generic Pickup prefab.
  - Added PickupDropPool to the scene and assigned the Pickup prefab.
  - Assigned the shared Basic Alien DropTable to Standard, Shooter, and Kamikaze EnemyData assets.
  - Added HUD text for cash, Speed, Bullets, and Time so pickup effects can be validated during Play Mode.
  - Added a Pickup layer/collision matrix setup so bullets no longer collide with pickups while the player can still collect them.

  Deferred:
  - Autofire, RapidFire, and Shield pickup assets exist, but their real timed effects stay in Phase 6 with BuffManager.
  - Final drop weights and economy balance stay later; current weights are prototype-tuning values.


  ## Phase 6 — BuffManager and Timed Buff UI — Done
  Implemented the timed buff foundation for enemy-dropped Autofire, RapidFire, and Shield pickups.

  Done:
  - Added BuffType and BuffManager.
  - Added independent timers for Autofire, RapidFire, and Shield.
  - Re-collecting the same buff refreshes only that buff's timer.
  - Different buffs can run at the same time without cancelling each other.
  - Buff duration uses serialized base values plus fixed seconds per effective Time level, so Time upgrades and current-level Time suckers affect new buff durations.
  - Exposed IsAutofireActive, IsRapidFireActive, IsShieldActive, and remaining/duration read APIs.
  - Added a BuffTimerChanged ScriptableObject event channel.
  - PlayerShooting now reads Autofire and RapidFire from BuffManager while keeping debug toggles available.
  - PlayerHealth now reads Shield from BuffManager, with the old RunStatsManager shield hook kept as a fallback/debug path.
  - Timed buff pickups now activate real effects instead of logging placeholders.
  - Game over and restart clear active buffs.
  - Added BuffManager to the scene with prototype durations.
  - Added simple right-side TMP timer labels for Autofire, RapidFire, and Shield.

  Deferred:
  - Final buff duration tuning stays in the M4 content pass.
  - Polished buff icons/bars stay in the broader HUD/art polish passes.
  ———

  ## Phase 7 — Drop Table Tuning Pass — Done
  Tuned the first prototype drop tables now that Phase 5/6 pickups and timed buffs are working.

  Done:
  - Kept the Phase 5 drop architecture unchanged.
  - Reduced the first-pass validation drop chance from 77% to prototype gameplay values.
  - Replaced per-normal-enemy drop tables with one shared Basic Alien DropTable, closer to Warblade's normal-alien bonus pool.
  - Standard, Shooter, and Kamikaze enemies all use the shared basic pool.
  - Cash remains common in the basic pool.
  - Weapon bonuses, S/B/T upgrades, timed buffs, armour, extra life, and suckers are all available from normal aliens.
  - Suckers remain uncommon but visible enough for testing.
  - Special drops for future big aliens, bosses, hurry-up ships, rank markers, and mini-game extras remain deferred until those enemy/content types exist.

  Deferred:
  - Final economy balance remains later; these weights are prototype values for M4 playtesting.
  ———

  ## Phase 8 — Cash HUD, Stat Bars, Lives, Armour, and Weapon HUD — Done
  Consolidated the M4 HUD placeholders into a functional run HUD.

  Done:
  - Cash, lives, armour, Speed, Bullets, and Time HUD readouts listen to RunStatsManager event channels.
  - Speed, Bullets, and Time now display simple text bars with current/max values.
  - Added WeaponTierHud for current weapon display through the weapon-tier event channel.
  - Active timed buffs continue to display through TimedBuffHud and BuffTimerChanged.
  - RunStatHud still uses singleton polling only for initial refresh; live updates come from event channels.
  - Added max Speed/Bullets/Time accessors to RunStatsManager for HUD display.

  Deferred:
  - Final art, icons, animated bars, and layout polish stay in the later visual polish pass.
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
  - Weapon upgrade purchases follow the same rule as pickups:
      - advancing to a higher weapon tier grants +1 Bullets once
      - buying a tier the player already has or exceeds must not farm extra Bullets
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
