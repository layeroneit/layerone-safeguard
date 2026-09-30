# LayerOne App Safeguard

> 👪 **Parents: start with [PARENTS.md](PARENTS.md).** It explains what Safeguard does in plain English.

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

## Parent app (Windows)

```powershell
dotnet run --project src\Safeguard.App\Safeguard.App.csproj
```

A simple window and a tray icon. First run asks you to accept the AS-IS notice. After that, Safeguard starts when you sign in. It rests until Discord or Roblox is open. Closing the window hides it to the tray; use Quit in the tray menu to stop.

First run: AS-IS notice, then **Email alerts** (your address, app password, optional second parent, test email must go through before Finish). Change it later from the main window or tray.

## Phrases and scoring

`phrases/taxonomy.v1.json` is the versioned list of warning-sign behaviours (personal info, moving apps, secrecy, trust-building, meeting, photo requests). Each category has a parent-facing label and a **talk-it-over** line used in the app and email. Scoring runs on-device (`src/Safeguard.Scoring`). Tests are synthetic only:

```powershell
dotnet test tests\Safeguard.Scoring.Tests
```

## Flagged log

Only reads that score "needs a look" or higher are written, DPAPI-encrypted to the Windows account, one file per day under `%LOCALAPPDATA%\LayerOne\Safeguard\flagged`, pruned after 30 days. View it from **Flagged log** in the main window.

## Icons

`python tools/make_icons.py` rebuilds `assets/icon/app.ico`, the header mark, and the tray state icons from the Layer One palette.

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
