# QuickMail v0.5.7 Release Notes

## New Features

### Settings dialog

A new **Settings** dialog (**File → Settings**) puts all user-configurable options in one place
 
**General tab**

| Setting | What it controls |
|---------|-----------------|
| View Mode | Default message grouping (Messages, Conversations, By Sender, By Recipient) |
| Sync Days | How many days back to fetch on sync |
| Preview Lines | Number of body preview lines shown in the message list |
| Show Message Status | Whether read/unread labels appear in the message list |
| Initial Sync Count | Maximum messages fetched per folder on first account sync |

**Keyboard Shortcuts tab**

Assign custom key bindings to any registered command without editing config files:

- Select a command from the list and press **Set…** to open a dedicated key capture dialog
- Press any `Ctrl`, `Shift`, or `Alt` combination to assign it; unmodified single keys are not accepted
- If the combination is already used by another command, a conflict dialog identifies the clash and lets you choose another key or cancel
- Press **Restore** to remove a custom binding and return to the default
- All custom bindings survive app restarts and work alongside the command palette

### Move/Copy to Folder for grouped views

**Move to Folder…** and **Copy to Folder…** now appear in the Shift+F10 and right-click context menu for sender groups (By Sender view) and recipient groups (By Recipient view), in addition to the existing per-message support. All messages in the group are moved or copied in one action, and focus returns to the group list when the operation completes.

### Search Folders command

A dedicated **Search Folders…** command (`Ctrl+Shift+F`, also in **View → Search Folders…**) opens the flat, searchable folder picker. This is separate from the folder tree (`Ctrl+2` / `Ctrl+Y`), which now consistently focuses the tree pane.

---

## Improvements

### Message performance

- **Prefetching** — When you open a folder, the first ten messages are prefetched from IMAP in the background so that opening them shows the body instantly. The five messages above and below a newly opened message are also prefetched so navigation feels immediate.
- **Faster sync** — Folder sync operations that process large All Mail views (thousands of messages) now use key-based lookups instead of scanning the full list on every incoming message, eliminating multi-second UI pauses during background sync on large mailboxes.

### Message rendering

- Heavy or marketing-style HTML messages are prepared off the UI thread before being sent to the reading pane, so the interface stays responsive while complex mail loads.
- Messages with deeply nested tables, large embedded images, or excessive inline styling are shown in a simplified reader mode rather than attempting to render the full HTML.
- A timeout on reading pane navigation prevents a malformed message from holding up the interface indefinitely.

### Folder navigation

- `Ctrl+2` and `Ctrl+Y` consistently focus the **folder tree** pane. Use `Ctrl+Shift+F` when you want the searchable flat list.
- Selecting a folder from the picker now shows cached messages immediately while IMAP refreshes in the background, rather than waiting for the server before returning control.

### IMAP connection management

- The default connection limit is 6 per account (configurable up to 15 via `MaxImapConnectionsPerAccount` in `config.ini`).
- Background work (sync, polling, UID checks, preview fetches) is capped below the pool maximum so opening a message or downloading an attachment always has connection capacity available.

---

## Accessibility

### Quieter startup

Screen readers no longer receive a flood of "structure changed" events while the initial background sync runs. The message list is now populated silently during sync and presented in a single update at the end. The periodic "N messages loaded so far" announcements have also been removed.

### Default account label

The default account in **File → Manage Accounts** now shows a **- default** label next to the account name in the account list, both visually and for screen readers.

### Reading pane focus

Focus is restored into the message body in the reading pane after a message finishes loading.

---

## Bug Fixes

- **Escape freeze** — Pressing Escape to close the reading pane could hang the UI for several seconds. Fixed by properly cancelling the in-flight body load chain and stopping WebView2 navigation before clearing the pane.
- **Tab lands on menu bar** — Pressing Tab from the message list no longer moved focus to the menu bar. Focus now continues through the reading pane as expected.
- **Toolbar in Tab order** — The toolbar was included in the normal Tab sequence, causing an unexpected stop between the folder tree and message list. It is now excluded.
- **Delete in grouped views** — After deleting a message in Conversations, By Sender, or By Recipient view, focus now moves to the next message in the same group rather than losing the selection entirely.
- **Account deletion cleanup** — Deleting an account now also removes its locally cached messages from the database and clears any stored OAuth tokens, leaving no orphaned data behind.
- **Link handling** — Plain-text http and https URLs in messages were not opening correctly in some cases. Link detection and external browser handoff have been made more reliable.

---

## Security

- **Attachment path traversal** — Attachment filenames containing path separators (`..`, `/`, `\`) could previously be written outside the intended download directory. Filenames are now sanitised before saving.
- **Content Security Policy** — The reading pane CSP now explicitly blocks `object-src`, inline styles from unknown origins, and a broader range of active content.
- **PII in logs** — Email addresses and message subjects are no longer written to the log file at default log levels. They appear only when the `/debug` flag is passed at startup.

---

## Code quality (internal)

- `BatchObservableCollection<T>` added: an `ObservableCollection<T>` subclass with `BeginBatch()`/`EndBatch()` that suppresses per-item change notifications and fires a single Reset at the end. Used by the message list during incremental sync to reduce UIA noise.
- `SettingsViewModel` and `SettingsDialog` added with full test coverage in `SettingsViewModelTests`.
- All keyboard shortcuts introduced in this release are registered in `CommandRegistry` and appear in the keyboard customization UI.
