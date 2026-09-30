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

## 2026-09-29 — Parent Windows applet

- **Audience.** Parents, not technicians. WPF window + always-visible tray. Plain language.
- **Not a Windows Service.** A service in session 0 cannot see the desktop. Safeguard runs in the signed-in session.
- **Autostart.** Logon scheduled task starts Safeguard in the tray (`--quiet`). Capture starts only when Discord or Roblox is running.
- **No impact.** Do not restore, focus, inject, or read process memory. Skip Roblox OCR when the frame hash is unchanged. Scale OCR frames down. Rest when both apps are closed.
- **M0 result.** Discord Friends chrome and Roblox home UI both returned text. Chat-specific reads still need a channel / in-game chat.

## 2026-09-29 — Outbound email alerts are parent copy

- **Audience.** Written for parents. Plain English. No technician jargon in the
  subject or body (no OCR, UI Automation, pipeline, SMTP, and the like).
- **From / display name.** LayerOne App Safeguard. The mailbox is still the
  admin’s (see Mail above). The name on the message is the app.
- **Subject.** Always names Safeguard so a parent knows who sent it, e.g.
  “Safeguard notice: …”.
- **Sign-off.** Body ends with Safeguard, then Layer One IT Consultants LLC on
  the next line.
- **Copy only.** Subject, body, and signature are built from a simple
  AlertMessage (app name, time, short snippet, category if present). SMTP send
  and the admin mail wizard stay a later step.

## 2026-09-29 — Child identity filter is day 2

- **Recommendation.** Day 2 for filtering. Day 1 already watches the Discord and
  Roblox apps on this Windows sign-in. The parent screen can collect the child’s
  Discord and Roblox usernames now (optional). Leave them blank and Safeguard
  keeps watching the whole app, same as today.
- **Why not day 1.** Seeing those two apps without touching them is enough for
  day 1. A name filter is a second product. Discord text channels often show who
  wrote a line, so a username can help there later. The Friends list and other
  Discord chrome are not “this child said.” Roblox is a picture of the whole
  game window. In-game chat mixes many players. Guessing “this line is the
  child’s” from a screenshot is approximate, not exact. On a shared family PC,
  more than one Roblox account can sit on the same Windows login. A screenshot
  cannot perfectly tell them apart.
- **Honesty for later.** When we do filter: Discord text channels can be good
  enough if the author name is in the accessibility tree. Discord Friends and
  other chrome cannot. Roblox will miss some lines and can attach the wrong
  name. We will say that in the parent UI. Blank names still mean watch the app.
- **Stub only.** Store the two optional names in AppSettings. Do not filter
  capture yet. Empty names must never stop a read.
