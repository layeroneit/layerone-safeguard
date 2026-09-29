# Decisions

Dated entries for LayerOne App Safeguard. Publisher: Layer One IT Consultants LLC.

## 2026-09-29 — Scope lock before first commit

- **Separate private repo.** This product is not part of Tag. Repo stays private
  until family testing is complete.
- **Publisher string.** Layer One IT Consultants LLC. Copyright: © Layer One IT
  Consultants LLC.
- **Palette.** Layer One family (violet / magenta lockup). Nest cerulean `#00AFD8`
  and Nest gray `#7B858E` are rejected as Google Nest trade dress.
- **Admin install/disable (v1).** Local only. Explicit install/uninstall. Parent
  can turn monitoring on/off and toggle Discord and Roblox separately without
  uninstalling. Off disables the logon task, stops capture, keeps the tray
  visible, and sends one last alert. Child session cannot clear that state.
  No remote cloud switch in v1.
- **Mail.** Setup is per admin. SMTP sender matches the email the admin types.
  Wizard checks that this mailbox is present on that Windows account, then
  sends a test. Secrets stay with the admin session that installed the app.
- **Host process.** Interactive user session via logon scheduled task. Not a
  session-0 service.
- **Capture.** Discord via UI Automation. Roblox via Windows.Graphics.Capture +
  OCR. No process memory reads. No injection.
- **First code.** M0 spike gates the rest of the plan.
