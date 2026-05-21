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

## Phase 1 — Presentation Audit

Status: Pending.

Goal:
- Decide what visual/audio work is actually needed before producing assets.

Checklist:
- List all visible placeholder sprites in gameplay, boss fights, pickups, shop, HUD, and menus.
- List missing feedback moments: shoot, hit, death, pickup, shop, boss intro, boss death, level complete, game over.
- Lock one visual direction: readable arcade sci-fi, strong silhouettes, clear colors.
- Lock one audio direction: short arcade SFX, non-fatiguing music, clear UI sounds.

Acceptance:
- M7 has a concrete asset/feedback checklist.

---

## Phase 2 — Sprite Replacement

Status: Pending.

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

Status: Pending.

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

Status: Pending.

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

Status: Pending.

Goal:
- Add depth and polish with URP 2D lighting while keeping WebGL performance reasonable.

Checklist:
- Add restrained 2D lights to bullets, explosions, pickups, bosses, or background where useful.
- Add a restrained Global Volume: bloom and color grading first, vignette only if it helps.
- Verify dense waves and boss fights stay readable.

Acceptance:
- The game looks less flat without making bullets or pickups hard to see.

---

## Phase 6 — Background Pass

Status: Pending.

Goal:
- Replace empty/flat backgrounds with a readable space backdrop.

Checklist:
- Add parallax starfield or layered space background.
- Add subtle chapter/cycle color variation if practical.
- Keep background contrast below gameplay objects.

Acceptance:
- Background adds motion and atmosphere without competing with gameplay.

---

## Phase 7 — Screen Feedback

Status: Pending.

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

Status: Pending.

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

Status: Pending.

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

Status: Pending.

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

Status: Pending.

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

Status: Pending.

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
