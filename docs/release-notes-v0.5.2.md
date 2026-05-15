# QuickMail v0.5.2 Release Notes

## New Features

### From (Sender) View
- New **From** view groups your inbox by sender so you can see at a glance who has sent you messages and how many.
- Select a sender group to expand it and read individual messages, or press **Delete** on the group to delete all messages from that sender at once.
- Toggle between Messages, From, and Conversations views from the View menu or with **Ctrl+Shift+V**.

### Command Palette
- Press **Ctrl+Shift+P** to open the command palette and run any action by typing its name — no need to remember every keyboard shortcut.
- All registered commands are searchable, including folder navigation, compose, delete, and view switching.
- Keyboard shortcuts can be customised via the command registry.

### Context Menus
- Right-click context menus are now available throughout the app: message list, folder tree, sender groups, and conversation groups.
- Common actions (Reply, Reply All, Forward, Delete, Mark as Read/Unread, Move to Folder) are available without leaving the keyboard.

### Virtual Folders
- In addition to **All Mail**, the folder tree now includes **All Inboxes**, **All Drafts**, **All Sent**, and **All Trash** — each aggregating the corresponding folder across all your accounts.

### Folder Picker Pre-selection
- **Ctrl+Y** folder picker now opens with the current folder already selected and scrolled into view.

## Bug Fixes

- **From/Conversations view group delete** — Deleting a sender group or conversation group on the second+ keypress previously deleted only one message instead of the whole group. Root cause: the global `Delete` hotkey was intercepting the keypress because `SelectedMessage` was left non-null after the previous delete. Fixed in three parts:
  1. `DeleteMessagesAsync` now clears `SelectedMessage` after a group delete (rather than leaving the old selection in place), preventing the global hotkey from firing on subsequent presses.
  2. The focus-landing listener (`LandOnSenderGroupAfterRebuild`) is now registered *before* the delete awaits IMAP, ensuring it catches the rebuild that fires ~60 ms in.
  3. A per-account semaphore in `ImapService.ExecuteWithRetryAsync` prevents background sync and a user-triggered delete from hitting the same `ImapClient` concurrently (which previously caused an `InvalidOperationException`).

- **Reply All missing recipients** — Reply All now correctly includes all original To recipients in addition to Cc. Previously only Cc was copied.

- **Startup crash** — Fixed an `APPCRASH` on launch caused by a WPF XAML compiler issue with `GridSplitter` click handlers inside `ContextMenu` style setters.

- **Empty Trash scope** — Empty Trash no longer clears the message list when you are viewing a non-trash folder. The list and selection are now unaffected unless you are currently looking at a Trash folder.

- **Empty message list keyboard navigation** — Arrow keys on an empty message list no longer escape the pane and accidentally move focus to the toolbar.

- **Command palette screen reader navigation** — Up/Down in the command palette results list now correctly moves focus to `ListBoxItem` elements, and typed characters are forwarded back to the search box via `PreviewTextInput`.

## Accessibility

- Status bar changes (folder loading, message counts) are now announced to screen readers via live regions.
- A dedicated hotkey focuses the status bar for screen reader users.
- The reading pane title and header fields (From, To, Subject) are now properly exposed to accessibility tools as read-only.
- Shift+Tab navigation out of the reading pane works correctly.
- Screen reader double-announcement on message selection has been eliminated by aligning the programmatic `Announce` text with the `AutomationProperties.Name` binding.
- Command palette now restores focus to the previous element on dismiss.
