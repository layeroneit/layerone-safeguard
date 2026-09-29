# Safeguard backlog

## Core

1. Run M0 spike with Discord and Roblox open; fix any failed path.
2. M1 pipeline: FeatureWatcher → capture → normalize → score → console alert.
3. M2: admin wizard, SMTP test against the installing admin mailbox, encrypted store, parent dashboard.
4. M3: local admin on/off, per-app toggles, logon task, disclosure tray.
5. M5: installer (Inno Setup preferred) with AS-IS notice.

## UI

- Apply Layer One palette from `Safeguard.Brand`.
- Blender asset kit after the palette lock (already locked).

## Security

- DPAPI for admin SMTP secrets in the admin session only.
- No chat content in logs.
- Child session cannot disable monitoring.
