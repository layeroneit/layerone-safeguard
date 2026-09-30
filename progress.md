# Progress

## 2026-09-29

Shipped: private-repo scaffold, locked decisions, Layer One brand tokens, M0 capture spike, then the parent Windows applet (tray + simple window). Capture wakes only when Discord or Roblox is open. Roblox OCR skips unchanged frames and does not restore or steal focus.

Icons: Safeguard mark (violet tile, Layer One gradient ring, white shield) built by `tools/make_icons.py`. Exe/taskbar/window icon, header mark + Layer One wordmark on Welcome and Main, and a tray icon per state (resting, watching, needs a look, off). App, Core and Capture are now in the solution so a solution build covers the parent app.

Blockers: none for capture APIs. Discord Friends chrome and Roblox home UI both returned text. Open a Discord text channel and in-game chat for a tighter read.

Later the same day: phrase taxonomy v1 + on-device scorer (40 synthetic tests green), "Needs a look" list with talk-it-over tips, encrypted flagged-only log + viewer, email setup on first run with required test email, alert emails with cooldown, turned-off email, kids info window, PARENTS.md.

Next:
1. Parent runs the email setup and confirms the test email arrives.
2. Open a Discord text channel and in-game Roblox chat; confirm "Last text" updates and a synthetic phrase shows in Needs a look.
3. Day 2: filter to the child's usernames.
4. Installer (Inno Setup).
