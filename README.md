# LayerOne App Safeguard

**Publisher:** Layer One IT Consultants LLC  
**License:** Freeware. Provided **AS-IS, NO WARRANTY**. See `LICENSE.txt`.  
**Platform:** Windows 10/11 (x64)

Disclosed, on-device, alert-only monitoring of Discord and Roblox chat on the
admin Windows session. A tray icon stays visible. Flagged snippets are emailed
through the installing admin’s own mail account. The app never blocks, deletes,
or acts on its own.

This repository stays **private** until family testing is complete.

## Locked product rules

1. Disclosed to the child. No hidden tray. No stealth.
2. Capture, normalize, and score stay on the machine. Raw chat does not leave it.
3. Alert-only. Human in the loop.
4. Persist flagged events only, encrypted, with a retention window (default 30 days).
5. Out-of-process capture only. No memory reads. No injection.
6. Mail uses the installing **admin** account. SMTP identity matches the address
   the admin types. Setup checks that this mailbox is present on that Windows
   account, then sends a test message before finish.
7. Parent can install, uninstall, and turn monitoring on or off locally
   (including Discord and Roblox separately). Off keeps the tray visible and
   sends one last alert. No remote cloud switch.

## Brand

Safeguard is a Layer One production. UI uses the Layer One palette in
`src/Safeguard.Brand/Colors.cs`. Do not use Nest / Google Nest colors or marks.

| Role | Hex |
|---|---|
| Canvas | `#F6F4F8` |
| Surface | `#FFFFFF` |
| Ink | `#1A1033` |
| Primary | `#3A2081` |
| Accent | `#C084FC` |
| Publisher gradient | `#FF2DC4` → `#C084FC` (lockup only) |
| Secondary text | `#5C5670` |
| Clear | `#1F8A5B` |
| Review | `#C4841A` |
| Risk | `#B42318` |

## Build order

- **M0 — Feasibility spike** (this repo’s first work): prove Discord UI Automation
  and Roblox window capture + OCR on a live machine.
- **M1** Core pipeline
- **M2** Alerts, admin wizard, config, parent dashboard
- **M3** Hardening, autostart, disclosure, local admin on/off
- **M4** Family beta
- **M5** Installer package

## M0 spike

```powershell
cd src\Safeguard.Spike
dotnet run
```

Leave Discord and/or Roblox open. The console reports whether each capture path
returns readable text. If a target is not running, that path is skipped.

## Requirements

- .NET 8 SDK (Windows)
- Windows 10/11 x64
- Discord and/or Roblox running for a live spike
