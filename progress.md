# Progress

## 2026-09-29

Shipped: private-repo scaffold, locked decisions, Layer One brand tokens, M0 capture spike, then the parent Windows applet (tray + simple window). Capture wakes only when Discord or Roblox is open. Roblox OCR skips unchanged frames and does not restore or steal focus.

Icons: Safeguard mark (violet tile, Layer One gradient ring, white shield) built by `tools/make_icons.py`. Exe/taskbar/window icon, header mark + Layer One wordmark on Welcome and Main, and a tray icon per state (resting, watching, needs a look, off). App, Core and Capture are now in the solution so a solution build covers the parent app.

Blockers: none for capture APIs. Discord Friends chrome and Roblox home UI both returned text. Open a Discord text channel and in-game chat for a tighter read.

Next:
1. Open a Discord text channel and in-game Roblox chat to tighten reads.
2. Parent mail wizard (test email).
3. Phrase scoring + alerts.
