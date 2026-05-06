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