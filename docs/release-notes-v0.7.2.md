# QuickMail v0.7.2 Release Notes

## Download

Two options are available for v0.7.2:

| Download | When to use |
|----------|-------------|
| **`quickmail-v0.7.2-setup.exe`** — Windows installer | Recommended for most users. Installs per-user with no elevation required, checks for the WebView2 Runtime, and registers an uninstaller. |
| **`QuickMail.exe`** — standalone portable executable | No installation required. Copy it anywhere and run. |

Both downloads include the .NET 8 runtime — you do not need to install .NET separately.

---

## New Features

### Microsoft Graph Backend (Preview)

QuickMail now includes a read-only Microsoft Graph backend alongside IMAP, enabling support for Microsoft 365 and Outlook.com accounts. This is a foundation for future Microsoft account support; full sync and compose features for Graph accounts are coming in future releases.

**Account setup for Graph accounts:**
- When adding an account, you can choose **OAuth** for Microsoft 365 / Outlook.com accounts
- QuickMail launches your browser to sign in via Microsoft's authentication page
- No password is stored — tokens are managed securely by the system
- After authentication, QuickMail connects and loads your account information

**Graph-backed account dialogs:**
- Dialogs for Graph accounts now show the account type and username clearly
- UI elements specific to IMAP (password, port, security settings) are hidden for Graph accounts
- OAuth token information is displayed where applicable

---

## UX Improvements

### Sync Progress Announcements

Screen reader users will experience significantly quieter sync progress announcements:

**Before:** Screen readers announced the sync progress number after every single folder completion, creating excessive chatter ("1", "2", "3", "4"... "45").

**After:** QuickMail announces sync progress only every 10 folders or at the end of sync:
- "Synced 10 of 45 folders."
- "Synced 20 of 45 folders."
- "Synced 30 of 45 folders."
- "Synced 40 of 45 folders."
- "Sync complete."

The status bar continues to show "Syncing mail…" for sighted users without duplicating announcements through the screen reader.

---

## Bug Fixes

- **Duplicate sync progress announcements.** The status bar text and explicit screen reader announcements were both being read aloud during sync, creating redundant chatter. Now only the explicit announcements (which respect user settings) are spoken, eliminating duplicates.

---

## Thank You to Contributors

Thank you to everyone who has contributed to QuickMail through code, bug reports, feature suggestions, and other feedback. Your contributions make the project better for everyone.

---

## Internal

### Microsoft Graph Support

- `IMailService` now abstracts backend operations to support IMAP, Graph, and future backends
- `MailServiceRouter` routes method calls to the appropriate backend based on account type
- `GraphMailService` — new Graph backend implementation (read-only for v0.7.2)
- Account dialog UX now detects account type and shows/hides protocol-specific settings accordingly

### Sync Progress Reporting

- `ISyncService.SyncProgressChanged` event fires after each folder completes with `(completedFolders, totalFolders)`
- `MainViewModel` now throttles status text updates to every 10 folders to prevent excessive screen reader announcements
- Announcements respect `ConfigModel.AnnounceStatus` user preference

---

## Known Limitations

### Microsoft Graph Backend

- **Read-only in v0.7.2.** Fetching and viewing messages is supported; sending, drafts, and mutations are not yet implemented.
- **No direct Microsoft 365 integration for shared mailboxes.** Only personal accounts are supported at this time.

---
