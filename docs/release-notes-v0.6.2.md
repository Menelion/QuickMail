# QuickMail v0.6.2 Release Notes

## New Features

### Connection status and message counts in the account list

Each account in the account list now displays a status line showing connection state and unread/total message counts across all folders for that account:

- **Connected** — when the account is online
- **Connected — 5 unread, 44 total** — when synced and there is mail
- **Disconnected** — when the connection has been lost

Screen readers announce this status when you focus an account. Sighted users can also hover over an account to see the full status in a tooltip.

### Unread count badges on folders

The folder tree now displays an unread count badge (e.g., `(3)`) next to each folder name when there are unread messages. This gives you a quick visual scan of which folders contain new mail without opening them. This is not yet available for virtual folders such as All Mail, All Inboxes and All Sent.

### Account-wide message counts

Unread and total message counts in the account list now reflect your **entire account** across all folders — not just the inbox. This gives you an accurate picture of your mail volume at a glance.

---

## Improvements

- **Status bar message count now updates live** — The count in the status bar now updates correctly when you perform a search, apply a filter, or when new mail arrives via IMAP IDLE.
- **Sync Range setting now takes effect immediately** — Changing the Sync Range in Settings now triggers a refresh so you see the effect right away instead of requiring a manual refresh.
- **Virtual folders (All Inboxes, All Sent, etc.) now receive new mail in real-time** — New mail detected via IMAP IDLE is now properly merged into these views when they are active.
- **Folder navigation no longer flashes stale messages** — When switching between folders, you no longer see a brief flash of messages from the previous folder before the new folder's messages load.
- **Screen reader announcements are clearer** — Folder names and unread counts are no longer announced twice, and connection status is announced exactly once when you focus an account.

---

## Internal

- Account counts are now calculated using the IMAP STATUS command (instead of EXAMINE) for faster, more accurate folder metadata.
- Accessibility improvements use separate UIA properties (`AutomationProperties.ItemStatus`) to prevent screen reader duplication.
- All 235 existing tests continue to pass.
