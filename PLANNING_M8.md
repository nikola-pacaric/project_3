# M8 — Tune and Ship Core Plan

## Summary

M8 turns the polished M7 build into a stable, balanced, release-ready core game.

The goal is not to add major new systems. The goal is to make the existing 100-level campaign playable, readable, performant, and worth sharing before online leaderboards are connected in M9.

M8 owns difficulty curve, full-run balance, drop rates, buff durations, shop prices, WebGL build size/performance, cross-browser validation, and portfolio-ready release packaging.

M8 does not own UGS Authentication, Leaderboards, Cloud Save, Analytics, mini-games, mobile/touch support, localization, control rebinding, or new campaign content unless required to fix a balance problem.

Key rule:
- Tune what already exists before adding anything new.

---

## Phase 0 — M8 Baseline Capture - DONE

Establish the current build baseline before changing balance or optimization settings.

Baseline:
- Commit: `b50385d2d54569af8fe55c24adcb047d1bad3df2` (`after closing save`), captured after the first opening-level tuning commit `35070e4`.
- Player starts at level 1 with cash 0, lives 3, armour 0, Single weapon, and Speed/Bullets/Time upgrades at 0.
- Core player tuning: speed 5 (+0.75 per Speed level), max active bullets 5, fire cooldown 0.2s, respawn invulnerability 3s.
- Timed buffs: Autofire 8s, Rapid Fire 8s, Shield 6s, +1s per Time level.
- Current economy anchors: shared enemy drop table rolls at 18%; shop prices range from 50 for Extra Speed to 700 for Extra Life.
- Current enemy/boss/drop/shop values live in their ScriptableObjects; use this commit as the comparison point for future tuning diffs.
- WebGL build size and first-load time were not measured here; record them during Phase 6.

---

## Phase 1 — First 10 Levels Difficulty Curve - DONE

Make the opening stretch readable, fair, and motivating for a new player.

Check:
- Level 1 teaches movement and shooting without overwhelming the player.
- Levels 3-4 introduce real pressure, and level 10 is noticeably harder than level 1.
- A competent first-time player can reach the first shop opportunities.

- Levels 1-10 feel like a smooth ramp instead of ten isolated tests.
- Deaths feel earned, not random or visually unclear.
- The player sees enough rewards to understand the upgrade loop.

First 12 levels feel playable and not too difficult; cash drops feel too generous and move to Phase 2 tuning.

---

## Phase 2 — Rewards, Drops, and Buff Durations - DONE

Make pickups exciting without making survival or progression depend on luck.

Check:
- Upgrades are frequent enough to feel rewarding.
- Strong drops stay rare enough to feel exciting.
- Sucker downgrades create tension without becoming pure frustration.
- Timed buffs last long enough for the player to notice and use them.

- A normal 10-25 level run contains meaningful rewards without flooding the playfield.
- Timed buffs feel useful but not permanent.
- Bad drops create tension without feeling unfair.

Phase 2 completed after reward/drop/buff tuning, pickup readability checks, and the fixed 10000-point special-level clear bonus.

---

## Phase 3 — Shop Economy and Upgrade Value - DONE

Make the shop loop feel strategic and affordable without trivializing combat.

Check:
- Players can usually buy something after a few levels of decent play.
- No upgrade is obviously better than every other option.
- Saving cash can feel valid, and scoring still has room to matter in M9.

- Shop visits feel like decisions, not automatic clicks.
- Prices support gradual growth across the campaign.
- The player can recover after poor play, but not erase every mistake instantly.

Phase 3 completed after playtesting and adjusting shop prices, affordability, item value, and saving-versus-spending choices.

---

## Phase 4 — Midgame, Boss, and Full-Campaign Balance Pass

Extend tuning beyond the opening stretch and prove the full 100-level route is coherent.

Tasks:
- Play or debug-jump through representative levels from each chapter.
- Test levels near 24/25, 49/50, 74/75, and 99/100.
- Tune boss health, attack pressure, reward value, and readability only as needed.
- Check dense later waves for collision clarity, bullet readability, and performance.
- Confirm level-complete, shop, boss intro, boss death, and game-over flows still behave after tuning.

Check:
- Each chapter feels meaningfully harder than the previous one.
- Bosses feel like milestones, not random spikes.
- Outlier levels are documented and tuned before optimization.

- The 100-level route has a believable first-pass difficulty curve.
- Boss levels feel harder and more dramatic without breaking readability.
- Any known rough levels are documented before moving to optimization.

---

## Phase 5 — Infinite Cycle Scaling Check

Confirm levels 101+ reuse the campaign content cleanly and scale without obvious breakage.

Tasks:
- Test level 101 and a few later cycle samples through debug progression.
- Confirm health/speed multipliers apply correctly.
- Confirm cycle tinting still keeps sprites, bullets, pickups, and UI readable.
- Check that rewards and shop economy do not explode or collapse after the first campaign cycle.
- Decide whether cycle scaling needs only conservative numeric tuning for v1.0.

- Level 101 works as a clear continuation of the campaign.
- Cycle scaling increases pressure without immediately becoming unreadable.
- No new content is required for infinite mode in M8.

---

## Phase 6 — WebGL Build Optimization

Make the browser build small enough and efficient enough for a portfolio release.

Tasks:
- Enable and verify WebGL Brotli compression.
- Review code stripping settings.
- Audit texture import sizes and compression for generated art, backgrounds, UI, and VFX.
- Audit audio import settings: music should be streamed or compressed appropriately; short SFX should stay responsive.
- Check build report for unusually large assets.
- Measure first-load size against the target: under 30 MB if feasible.
- Confirm startup time target: playable within about 5 seconds where realistic for local/hosted test conditions.

- WebGL build size and load behavior are measured, not guessed.
- Obvious asset/import waste is fixed.
- The build is suitable for sharing as a portfolio artifact.

---

## Phase 7 — Runtime Performance and Stability

Keep the game smooth in dense waves, boss fights, and VFX-heavy moments.

Tasks:
- Test normal dense levels, boss fights, pickup-heavy moments, shop transitions, and game over in WebGL.
- Watch for frame drops, stutter, audio clipping, memory growth, or delayed input.
- Confirm pooled bullets, enemies, pickups, and VFX are not leaking or growing unexpectedly.
- Check browser console logs for runtime errors or warnings.
- Verify pause/resume does not leave music, SFX, timers, or input state stuck.

- The game targets stable 60fps in normal play.
- No crashes or obvious memory leaks appear during representative play.
- WebGL behavior matches the Unity Editor closely enough for release.

---

## Phase 8 — Cross-Browser Validation

Confirm the release build works in modern desktop browsers.

Tasks:
- Test Chrome first as the primary browser.
- Test Firefox and Edge.
- Test Safari if available, because it is the likely trouble spot.
- Verify keyboard input, audio startup, fullscreen/canvas sizing, pause, shop, boss flow, game over, and restart.
- Confirm the 16:9 canvas letterboxes correctly on non-16:9 windows.
- Record browser-specific issues and decide whether they block release.

- Chrome, Firefox, and Edge are playable.
- Safari status is known, even if a specific limitation needs to be documented.
- No browser-specific issue blocks the portfolio release.

---

## Phase 9 — External Playtest Pass

Get useful reactions from people who did not build the game.

Tasks:
- Share the build with at least 5 external playtesters.
- Ask testers to play without coaching first.
- Collect notes on difficulty, clarity, controls, shop choices, audio mix, and first impression.
- Separate true blockers from taste feedback.
- Apply small tuning fixes only; save larger feature ideas for later milestones.

Tester questions:
- Did you understand what to do in the first minute?
- Did deaths feel fair?
- Did pickups and shop upgrades make sense?
- Was anything too loud, too bright, too hard to see, or too slow?

- At least 5 playtest notes are collected.
- Critical confusion or frustration is addressed.
- Non-M8 ideas are captured without expanding scope.

---

## Phase 10 — Portfolio Package

Prepare the game to be shown publicly as a portfolio project.

Tasks:
- Host the WebGL build on itch.io or a personal site.
- Add README screenshots.
- Capture a 30-second gameplay GIF or video.
- Write a short postmortem: what worked, what did not, and what would change next time.
- Add the project link to the portfolio site, CV, or LinkedIn when ready.
- Confirm the public URL loads and plays from a clean browser session.

- There is a public URL.
- The game starts, plays, and restarts from the hosted build.
- The repository and portfolio materials explain the project clearly.

---

## Phase 11 — M8 Closeout

End M8 with the core game stable, documented, and ready for M9 leaderboard work.

Tasks:
- Update `README.md`, `PLANNING.md`, and this file with final M8 status.
- Document remaining balance concerns, known browser caveats, and deferred ideas.
- Confirm the working tree is clean after commit and push.
- Decide whether M9 should start immediately or whether a short bugfix pass is needed first.

- Public build exists and is playable.
- 100-level core is stable enough to score.
- M9 can begin without dragging unresolved M8 release chores behind it.

---

## Cut / Defer From M8

- UGS Authentication and Leaderboards stay in M9.
- Cloud Save and Analytics remain out of scope unless explicitly approved.
- Mini-games stay in v1.1.
- New art direction, new enemy families, and new boss concepts are out of scope.
- Mobile/touch support remains out of scope.
- Localization and control rebinding remain out of scope.
