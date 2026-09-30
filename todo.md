# Safeguard backlog

## Core

1. Tighten Discord to a text channel and Roblox to in-game chat.
2. Live-test email setup (Gmail app password) and one real alert.
3. Outlook.com: confirm SMTP app-password still works; else add OAuth (MailKit).
4. Day 2 username filter.
5. Installer (Inno Setup) with AS-IS notice.
6. Hide empty provider/found lines in email setup (spacing).

## UI

- Keep parent copy plain. Tray always visible.
- Blender asset kit after the palette lock (already locked).

## Security

- DPAPI for admin SMTP secrets in the admin session only.
- No chat content in logs.
- Child session cannot disable monitoring.
