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
- Replace the flat/empty gameplay backdrop with a readable arcade starfield that adds motion and atmosphere without competing with bullets, pickups, enemies, or boss attacks.

Direction:
- Visual style: restrained arcade starfield.
- Implementation: hybrid built-in Unity setup with SpriteRenderers for gradient/nebula layers and Particle Systems for stars.
- Asset source: generated bitmap assets, no third-party dependencies.
- Variation: subtle chapter palette shifts for levels 1-25, 26-50, 51-75, 76-100, plus cycle tint support for level 101+.
- Scene setup: code and assets are okay, but Unity scene/prefab setup should be done through explicit Editor steps unless direct YAML edits are explicitly approved.

Checklist:
- [x] Generate background assets: soft star dot, soft nebula/wisp, and 16:9 vertical space gradient.
- [x] Add `BackgroundPaletteData` ScriptableObject for four chapter palettes.
- [x] Add `SpaceBackgroundController` that listens to `LevelStarted` and applies chapter/cycle visuals.
- [x] Add far and near star Particle System layers.
- [x] Add gradient, far nebula, and near nebula SpriteRenderer layers.
- [x] Keep all background renderers behind gameplay using negative sorting orders on the existing Default sorting layer.
- [x] Add slow background drift/parallax that does not affect gameplay timing.
- [x] Write Unity Editor setup steps for creating and wiring the `Background` scene root.
- [x] Verify readability in an early level, dense level, boss level, shop transition, and level 101.

Completion notes:
- Editor setup was completed manually from Codex setup instructions.
- Playtest confirmed chapter color changes and cycle tint behavior.
- Final tuning adjusted chapter transparency and expanded far-star emission to cover the full gameplay screen.
 
---

## Phase 2 — Sprite Replacement

Goal:
- Replace gameplay-critical placeholders with readable final or near-final sprites.

Checklist:
- Player ship and thruster visuals.
- All normal enemy sprites.
- Special enemy sprites: kamikaze, bonus snake, mother, and current variants.
- Four distinct boss sprites.
- Player bullets and enemy bullets.
- Pickup icons/sprites with colors that match their actual effect.
- Basic UI icons for lives, armour, cash, weapon tier, buffs, shop, pause, and settings where useful.

Acceptance:
- A normal level can be played without obvious colored-box placeholder sprites.
- Player, enemies, bullets, pickups, and hazards are readable at gameplay speed.

---

## Phase 3 — Animation Polish

Goal:
- Add simple animations where they improve readability and arcade feel.

Checklist:
- Player thrust, hit, and death presentation.
- Enemy idle/formation and death animation.
- Boss intro, hurt, phase-change, and death presentation.
- Pickup spawn, idle shimmer/bob, and collect animation.

Acceptance:
- Animation improves feel without changing gameplay timing or breaking pooling.

---

## Phase 4 — VFX Pass

Goal:
- Add readable combat and reward effects without hiding gameplay.

Checklist:
- Player and enemy muzzle flashes.
- Bullet impact sparks.
- Enemy death explosions.
- Boss hit, phase-change, and death effects.
- Player hit, shield hit, and death effects.
- Pickup spawn and collect effects.
- Level start, level complete, boss warning, and boss defeat effects.

Acceptance:
- Combat feels responsive and readable.
- Effects do not obscure enemy bullets or pickups.

---

## Phase 5 — Lighting and Post-Processing

Goal:
- Add depth and polish with URP 2D lighting while keeping WebGL performance reasonable.

Checklist:
- Add restrained 2D lights to bullets, explosions, pickups, bosses, or background where useful.
- Add a restrained Global Volume: bloom and color grading first, vignette only if it helps.
- Verify dense waves and boss fights stay readable.

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
