# Progress

## 2026-09-29

Shipped: private-repo scaffold, locked decisions, Layer One brand tokens, and the M0 capture spike (Discord UIA + Roblox Graphics.Capture/OCR). Capture item GUID, free-threaded frame pool, and frame-copy-before-dispose are in so Roblox can return a real frame.

Blockers: none for capture APIs. Discord Friends chrome and Roblox home UI both returned text. Open a Discord text channel and in-game chat for a tighter M0 read.

Next:
1. Run `dotnet run --project src/Safeguard.Spike` with Discord and Roblox open.
2. Fix whichever capture path fails before starting M1.
3. Scaffold Core / Capture / Text after M0 passes.
