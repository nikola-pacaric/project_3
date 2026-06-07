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

Done:
- Chose Neon Arcade visuals, generated bitmap sprite sheets, Punchy Arcade audio, and Inspector-tunable presentation values.
- Kept gameplay tuning, economy balance, level balance, and leaderboard work out of M7.

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
- Player, enemy, boss, bullet, pickup, shield, boss bar, and run-stat bar visuals were replaced or upgraded for readability.
- Kamikaze and bonus enemies intentionally reuse regular enemy visuals; behavior differentiates them.

---

## Phase 3 — Animation Polish — Done

Goal:
- Add simple animations where they improve readability and arcade feel.

Done:
- Added player thruster particles, pickup spin, and player death/respawn timing with temporary invulnerability.
- Bosses now enter on a frozen first frame, start idle animation after reaching position, become vulnerable then, and trigger the boss HUD slide-in.

---

## Phase 4 — VFX Pass — Done

Goal:
- Add readable combat and reward effects without hiding gameplay.

Done:
- Added muzzle flashes, bullet impacts, enemy/player/boss death effects, shield hits, pickup collection effects, boss phase/defeat effects, and sector warp transitions.
- Skipped small enemy muzzle flashes, pickup spawn effects, extra boss hit particles, and boss warning particles because they added clutter or duplicated clearer signals.
- Boss muzzle flashes are attached under boss BulletPoints so they stay locked to moving bosses.

---

## Phase 5 — Lighting and Post-Processing — Done

Goal:
- Add depth and polish with URP 2D lighting while keeping WebGL performance reasonable.

Done:
- Added restrained 2D lights to bullets, pickups, and combat VFX where glow helps readability.
- Added restrained bloom and subtle color grading.
- Reworked boss health bar colors and playtested level 1 plus boss fights for readability.

---

## Phase 6 — Background Integration Check — Done

Goal:
- Re-check the background after sprite, animation, VFX, lighting, and post-processing work are in place.

Done:
- Playtest confirmed the background stays behind gameplay objects, bullets remain clear, chapter colors still work, and boss fights remain readable.
- No background brightness, alpha, density, or drift-speed changes were needed.

---

## Phase 7 — Screen Feedback — Done

Goal:
- Make hits, deaths, boss moments, and major events feel stronger.

Done:
- Added `ScreenFeedbackManager`, white screen flashes, and restrained Cinemachine impulse for player death, Mother death, and boss death.
- Wired feedback through event channels and fixed multi-hit death resolution so one death only applies one life loss and one weapon-tier downgrade.
- Skipped hit pause and normal player-hit shake/flash to preserve bullet readability.

---

## Phase 8 — Audio Foundation — Done

Goal:
- Add a stable audio system before assigning many clips.

Done:
- Added `AudioManager`, `AudioCue`, and `AudioBus` for global audio service, cue IDs, one-shots, looping music, and mixer volume control.
- Added and wired `GameplayAudioMixer` with Master, Music, SFX, and UI groups.
- Playtested the foundation with player shoot, shield hit, pause, and game-over cues.

---

## Phase 9 — SFX Pass — Done

Goal:
- Add first-pass sound feedback for the full v1.0 gameplay loop.

Done:
- Wired player, enemy, Mother enemy, boss, pickup, shop, UI, pause, game-over, sector-warp, kamikaze spawn, and bonus-special spawn cues.
- Removed kamikaze return and bonus-special despawn/death cue options because the level mix became too busy.
- Kept level start/complete visual-only for this pass, and kept pickup sounds collection-only.

---

## Phase 10 — Music Pass — Done

Goal:
- Add first-pass music for the current v1.0 game flow.

Done:
- Assigned menu, gameplay, boss, and game-over music clips on the `AudioManager`.
- Routed gameplay, boss, and game-over music through the current game flow; menu music is ready for the menu flow.
- Created loop-processed WAV versions so loop seams are cleaner without baking a fade into every repeat.
- Runtime music fades use `AudioSource` volume with a slower curved fade-in so music starts softly and loop points remain untouched.

---

## Phase 11 — UI and UX Polish

Goal:
- Make existing UI readable, consistent, and presentable without adding leaderboard or mini-game UI.

Planned:
- Polish HUD, shop, main menu, pause menu, settings, first-run controls hint, and game-over presentation.
- Add shop animation and visual polish so the between-level upgrade flow feels intentional instead of placeholder.
- Keep UI focused on the current v1.0 loop; leaderboard and mini-game UI remain out of scope.

Done so far:
- Added shop presentation art/data polish and a slide-in panel transition for the between-level shop.
- Sequenced the shop after the sector warp transition when both happen on the same completed level.
- Restored the shop gate so it only opens/stays open when at least one item is currently purchasable.
- Added a `Get ready!` / `Level X` message before enemy or boss spawning starts, using the same presentation path as level-complete messaging.

Acceptance:
- UI looks intentional, fits 1920x1080, and does not hide active gameplay.

---

## Phase 12 — Integration and WebGL Check

Goal:
- Confirm visual/audio polish did not break performance, readability, or existing systems.

Planned:
- Playtest early normal levels, dense later levels, boss levels, shop, pickup-heavy moments, game over, and level 101.
- Run a WebGL build manually and confirm SFX/music, VFX, readability, and performance in browser.

Acceptance:
- Core game is visually and audibly presentable.
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
