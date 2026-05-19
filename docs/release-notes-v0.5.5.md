# QuickMail v0.5.5 Release Notes

## New Features

### Per-account All Mail virtual folders

Each account now has its own **All Mail — {Account Name}** entry directly under that account in the folder tree. Selecting it shows every non-excluded message for that account across all of its folders, sorted newest-first — the same aggregate view as the global All Mail, but scoped to one account.

The per-account All Mail folders also appear in the **Ctrl+Y** folder picker under their respective accounts.

### To (Recipient) View

A new **To** view groups messages by recipient, complementing the existing **From** (sender) view. This is useful when a folder contains mail sent to multiple addresses — for example, a shared mailbox or an alias — letting you quickly scan messages by who they were addressed to.

Toggle between Messages, From, To, and Conversations views from the View menu.

### Type-ahead navigation

First-letter type-ahead is now available in the folder tree and message list. With focus in either pane, start typing a word and the selection jumps to the nearest matching item. Subsequent keypresses within a short timeout continue the search within the same prefix.

### Spell check in compose

The compose window body field now has spell checking enabled. Misspelled words are underlined as you type; right-click or use Shift+F10 a word for suggested corrections. Note, not all screen readers are picking up spelling notifications yet.

### Configurable sync range

Two new `config.ini` settings give you control over how much mail is fetched on sync:

| Setting | Default | What it does |
|---------|---------|--------------|
| `SyncDays` | `30` | How many days back to look for messages during sync. Set to `0` to fetch all messages (can be slow on large mailboxes). |
| `InitialSyncCount` | `500` | Maximum number of messages fetched per folder on the very first sync of a newly connected account. |

---

## Bug Fixes

- **Background sync ignored SyncDays** — Background sync was always fetching all messages regardless of the `SyncDays` setting. It now correctly limits its search to the configured date range.
- **Virtual folder order** — All Mail now appears before All Inboxes in the virtual folder group so keyboard and screen reader users encounter it first when arrowing down through the list.
- **Folder navigation strict matching** — Folder selection in the main window now uses strict account-ID matching, preventing a navigation edge case where the wrong account's folder could be highlighted after a folder picker selection.
- **Folder loading and account labels** — Several edge cases with folder tree population and account display name rendering have been resolved.

---

## Code quality (internal)

- Compose message building extracted into a dedicated `MimeMessageBuilder` static class, eliminating duplicated logic between compose, reply, and forward paths.
- Address parsing moved to a shared `AddressParser` utility class.
- `AccountManagerViewModel` and `AddAccountViewModel` now share a common `AccountEditorViewModel` base class, removing duplicated validation and field-binding code.
