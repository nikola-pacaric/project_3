# M7 — Game Feel, Visuals, and Audio Plan

## Summary

M7 turns the completed 100-level placeholder campaign into a presentable arcade game. The milestone is about visuals, sound, feedback, and UI polish. It is not the final tuning pass.

M8 owns difficulty, speed, health, shop prices, drop rates, economy balance, and final gameplay tuning. M9 owns leaderboard/UGS. Mini-games stay deferred to v1.1.

Key decisions:
- WebGL desktop browser remains the only target.
- Keep gameplay logic stable unless presentation work exposes a real bug.
- Prefer built-in Unity systems: SpriteRenderer, Animator, Particle System, URP 2D Lights, AudioSource, AudioMixer, TextMeshPro.
- Ask before adding packages or third-party assets.
- Codex should not edit scene, prefab, ProjectSettings, or other Unity-owned YAML directly unless explicitly asked.

---

## Phase 0 — Presentation Direction — Done

Goal:
- Lock the M7 visual, audio, UI, and tuning rules before production work begins.

Locked direction:
- Visual style: Neon Arcade.
- Sprite source: generated bitmap sprite sheets.
- Audio style: Punchy Arcade.
- Readability: player, enemies, bullets, pickups, VFX, and UI must remain instantly distinguishable.
- Tuning: presentation values for color, alpha, intensity, timing, speed, frequency, and volume must be Inspector-tunable through serialized fields or ScriptableObjects.

Deliverable:
- A short presentation spec used by Phases 1-11. Actual production checklists stay in those working phases.

Acceptance:
- The look, sound, UI language, and tuning model are decided.
- No gameplay tuning, economy, level balance, or leaderboard scope is added.

---

## Phase 1 — Background Pass — Done

Goal:
- Add a readable arcade starfield that supports chapter palettes, cycle tinting, and subtle motion without competing with gameplay.

Done:
- Generated background assets, added palette data/controller support, and wired the scene setup manually from Codex steps.
- Playtest confirmed chapter colors, cycle tint behavior, screen coverage, and gameplay readability.
 
---

## Phase 2 — Sprite Replacement — Done

Goal:
- Replace gameplay-critical placeholders with readable final or near-final sprites.

Done:
- Player, enemy, boss, bullet, pickup, shield, boss bar, and run-stat bar visuals were replaced or upgraded for gameplay readability.
- Kamikaze and bonus enemies intentionally reuse regular enemy visuals; their behavior is what differentiates them.
- Remaining visual adjustments, including bullet glow/color, deeper HUD/shop polish, and final boss data hookup, are deferred to later polish/tuning phases.

---

## Phase 3 — Animation Polish — Done

Goal:
- Add simple animations where they improve readability and arcade feel.

Done:
- Enemy sprite loops are already wired for the current enemy sets.
- Added player thruster particles, pickup spin, and player death/respawn timing with temporary invulnerability.
- Bosses now enter on a frozen first frame, start their idle animation after reaching position, become vulnerable at that moment, and trigger the boss HUD slide-in.
- Playtest confirmed the Phase 3 animation timing works. Explosions and impact particles stay in Phase 4.

---

## Phase 4 — VFX Pass — Done

Goal:
- Add readable combat and reward effects without hiding gameplay.

Checklist:
- [x] Player muzzle flash.
- [x] Bullet impact sparks.
- [x] Enemy death explosions.
- [x] Pickup collect effect.
- [x] EnemyBulletImpact VFX.
- [x] Shield hit effect.
- [x] Player death effect.
- [x] Boss muzzle flash.
- [x] Boss hit effect.
- [x] Boss phase-change effect.
- [x] Boss warning effect.
- [x] Boss death effect.
- [x] Boss defeat effect.
- [x] Sector warp transition after every 4-level enemy set and boss level.

Decisions:
- Small enemy muzzle flashes are intentionally skipped so enemy bullets remain clean and readable.
- Pickup spawn effects are intentionally skipped so drops can appear without adding screen clutter.
- Boss hit particles were chosen not to implement because player bullet impact sparks already show the contact point, and an extra boss-centered hit pulse made fast multi-shot damage harder to read.
- Boss warning particles were chosen not to implement because the boss sprite animation already signals the active/combat-ready moment after the slide-in.
- Boss muzzle flashes are attached under each boss BulletPoint instead of pooled through VfxManager so they stay visually locked to moving bosses.
- Boss defeat is a sprite/object presentation: the boss stops, shakes, rises slightly, then triggers the BossDeath line-burst VFX.

Acceptance:
- Combat feels responsive and readable.
- Effects do not obscure enemy bullets or pickups.
- All four bosses were playtested with the muzzle flash, phase-change ring, and delayed death presentation working cleanly.

---

## Phase 5 — Lighting and Post-Processing — Done

Goal:
- Add depth and polish with URP 2D lighting while keeping WebGL performance reasonable.

Done:
- Added restrained URP 2D lights to player bullets, enemy bullets, boss bullets, pickups, and combat VFX where the glow helps readability.
- Added a restrained Global Volume with bloom plus subtle color grading: darker exposure, more contrast/saturation, and mild white-balance warmth.
- Reworked boss-specific health bar fills to preserve the original bright laser texture while matching each boss color, including Boss 3's greener palette.
- Playtest confirmed boss fights read well with the lighting, VFX, boss bar colors, and bullets.
- Playtest confirmed level 1 remains readable with the updated effects, colors, and bullets.

Acceptance:
- The game looks less flat without making bullets or pickups hard to see.

---

## Phase 6 — Background Integration Check

Goal:
- Re-check the background after sprite, animation, VFX, lighting, and post-processing work are in place.

Checklist:
- Verify background contrast stays below gameplay objects after new sprites and VFX are added.
- Verify chapter/cycle color shifts still work after lighting and post-processing are tuned.
- Adjust background brightness, alpha, density, or drift speed only if readability suffers.

Acceptance:
- Background still adds motion and atmosphere without competing with gameplay.

---

## Phase 7 — Screen Feedback

Goal:
- Make hits, deaths, boss moments, and major events feel stronger.

Checklist:
- Player hit flash.
- Screen shake for player hit/death, boss phase change, boss death, and major explosions.
- Optional short hit pause only if it does not hurt control precision.

Dependency note:
- Use Cinemachine only if it is already available or explicitly approved. Otherwise use a small custom camera shake component.

Acceptance:
- Feedback makes important events obvious while movement and bullet reading remain fair.

---

## Phase 8 — Audio Foundation

Goal:
- Add a stable audio system before assigning many clips.

Checklist:
- `AudioManager` global service if not already present.
- Separate Master, Music, SFX, and UI mixer groups.
- One-shot SFX support.
- Looping music support.
- Volume settings for master/SFX/music.
- No runtime `FindObjectOfType` or `GameObject.Find`.

Acceptance:
- SFX and music can play through separate channels and be controlled independently.

---

## Phase 9 — SFX Pass

Goal:
- Add first-pass sound feedback for the full v1.0 gameplay loop.

Checklist:
- Player: shoot, hit, shield hit, death, weapon upgrade.
- Enemy/combat: enemy shoot, hit, death, boss hit, boss phase change, boss death, boss intro.
- Pickups/economy: spawn, collect, cash, armour, life, timed buff, sucker downgrade.
- UI/flow: button, shop open, buy success/fail, shop leave, pause, level start, level complete, game over.

Acceptance:
- Every important action/result has sound feedback and repeated sounds are not painful.

---

## Phase 10 — Music Pass

Goal:
- Add first-pass music for the current v1.0 game flow.

Checklist:
- Main menu music.
- Gameplay music.
- Boss music.
- Game over/results sting or short loop.
- Clean transitions and fades.
- Handle WebGL audio start after user interaction.

Acceptance:
- Music works in menu, gameplay, boss, and game-over contexts without duplicate tracks or pops.

---

## Phase 11 — UI and UX Polish

Goal:
- Make existing UI readable, consistent, and presentable without adding leaderboard or mini-game UI.

Checklist:
- HUD polish: score, level, lives, armour, cash, weapon, buffs.
- Shop polish: layout, icons, selected/disabled/buy states, feedback.
- Main menu: Start and Settings.
- Pause menu.
- Settings: master/SFX/music volume.
- First-run controls hint.
- Game over polish with final score and restart.

Acceptance:
- UI looks intentional, fits 1920x1080, and does not hide active gameplay.

---

## Phase 12 — Integration and WebGL Check

Goal:
- Confirm visual/audio polish did not break performance, readability, or existing systems.

Checklist:
- Confirm repeated VFX are pooled or cheap enough.
- Confirm repeated SFX do not cause object churn.
- Play early normal level, dense later level, boss levels, shop, pickup-heavy moments, game over, and level 101.
- Run a WebGL build manually and play in browser.

Acceptance:
- Core game is visually and audibly presentable.
- SFX and music work in WebGL.
- Dense waves and boss fights remain readable.
- Tuning remains deferred to M8.

---

## Cut / Defer From M7

- Difficulty tuning stays in M8.
- Enemy speed, health, boss balance, shop pricing, drop rates, and economy balance stay in M8.
- Leaderboard and UGS integration stay in M9.
- Mini-games stay in v1.1.
- Mobile/touch support remains out of scope.
- Localization and control rebinding remain out of scope.
