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
  - Weapon upgrades: advancing to a higher weapon tier also grants +1 Bullets once, matching Warblade weapon pickups.
  - Buffs: independent timers; picking the same buff refreshes that buff only.
  - Shield: timed invulnerability pickup.
  - Armour: separate max-2 hit buffer, bought/dropped.
  - Sucker: weapon tier down one for the run, plus one random temporary stat debuff for the current level among Speed/Bullets/Time.
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

  Verified:
  - Local C# compile check passes.
  - Unity Play Mode smoke test confirmed the game still plays like M3.

  Deferred:
  - B/S/T values are stored only; gameplay formulas arrive in later shooting, movement, and buff phases.
  - Shop, pickups, drop tables, timed buffs, and event channels remain later M4 phases.
  ———

  ## Phase 2 — Event Channels Before More UI Wiring — Done
  Added ScriptableObject event channels so M4 HUD, shop, pickup, and buff systems can react to state changes without polling managers.

  Done:
  - Added void, int, game-state, and weapon-tier event channel ScriptableObjects.
  - Added matching listener components for inspector-wired UnityEvent responses.
  - GameManager now raises game-state changes.
  - RunStatsManager now raises cash, lives, armour, weapon tier, effective S/B/T level, and run-reset events.
  - LevelManager now raises level-started and level-completed channels while preserving its existing UnityEvents.
  - LevelHud and LevelCompleteScreen can listen to event channels when assigned, with fallback to the existing LevelManager path.

  Verified:
  - Local C# compile check passes.
  - Unity Play Mode smoke test confirmed level HUD, level-complete banner, gameplay, game over, and restart still work.

  Deferred:
  - Score HUD stays on the current direct ScoreManager path until the broader HUD pass.
  - Buff-specific event channels wait until BuffManager exists.

  ———

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

  Verified:
  - Local C# compile check passes.
  - Unity Play Mode smoke test confirmed single/double/triple/quad, bullet cap, angled visuals, debug autofire/rapid-fire, and restart/gameplay still work.
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
  - WeaponUpgrade rule:
      - advances weapon tier by one step when possible
      - grants +1 Bullets only when the weapon tier actually increases
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
