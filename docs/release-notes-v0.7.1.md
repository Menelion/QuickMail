# QuickMail v0.7.1 Release Notes

## Download

Two options are available for v0.7.1:

| Download | When to use |
|----------|-------------|
| **`quickmail-v0.7.1-setup.exe`** — Windows installer | Recommended for most users. Installs per-user with no elevation required, checks for the WebView2 Runtime, and registers an uninstaller. |
| **`QuickMail.exe`** — standalone portable executable | No installation required. Copy it anywhere and run. |

Both downloads include the .NET 8 runtime — you do not need to install .NET separately.

---

## New Features

### Address Book Contacts management

The Address Book Contacts tab has been completely redesigned with explicit Add, Edit, and Delete workflows:

**Adding a contact:**
- Press **Add** or activate the button from the action buttons
- Screen reader announces: "Add contact"
- Name and Email fields become editable
- Type the contact's name and email address
- Press Enter or activate **Save** to add the contact
- The contact is added to the list and fields return to readonly

**Editing a contact:**
- Select a contact from the list
- Press **F2**, click **Edit**, or activate the Edit command (`contacts.edit`)
- Screen reader announces: "Edit {contact name}"
- Name and Email fields become editable
- Modify the contact's name or email address
- Press Enter or activate **Save** to save changes
- If the email address is already used by another contact, an error message appears and you can correct it
- On successful save, the contact list updates and fields return to readonly

**Deleting a contact:**
- Select a contact and press Delete or activate the Delete button
- A confirmation dialog asks you to confirm
- If confirmed, the contact is deleted from the address book

**Reading contact details (keyboard-only access):**
- Select a contact to populate the Name and Email display fields
- Press Tab to focus the Name field (readonly, but fully navigable)
- Use arrow keys to move through the text character-by-character (useful for email addresses)
- Your screen reader will read the text as you navigate
- Press Ctrl+A to select all text; Ctrl+C to copy
- Press Tab to move to the Email field or other controls

### Readonly field navigation

Name and Email display fields are fully keyboard-navigable even when readonly:
- Arrow keys move the cursor character-by-character
- Home / End jump to the start or end of the field
- Tab / Shift+Tab navigate to the next/previous control
- Ctrl+A selects all text
- Screen readers can read the content using their standard text access commands

### IMAP Connection Health

QuickMail now implements robust connection recovery and failure verification to maintain reliable mail sync even on unstable networks:

**Startup retry with exponential backoff:**
- On first launch, QuickMail retries account connections up to 3 times
- Delays increase: 15 seconds, then 30 seconds, then 60 seconds
- Timeouts also increase per attempt (30s, 45s, 60s)
- Automatically handles brief network interruptions without user intervention

**Dynamic connection status:**
- Account connection state is continuously monitored via the IDLE watcher
- If the connection drops, the app detects it and updates the UI in real-time
- IDLE watcher automatically reconnects with exponential backoff (30s, 60s, 120s cap)
- Connection status is reflected in the status bar

**Periodic heartbeat:**
- Every 10 minutes, QuickMail sends a NOOP (no-op) command to keep connections alive
- Prevents idle connection timeouts from slow/inactive networks
- Detects dropped connections mid-session

**Sync visibility in the status bar:**
- During sync: "Syncing… (X of Y folders)" shows folder-by-folder progress
- After sync completes: "Synced HH:MM" shows the last successful sync time
- Initial state: "Never synced" until first sync completes
- Sync in progress: "In progress" to avoid confusing "Never synced" + "Syncing…" sequence

**Account Properties Sync section:**
- New **Sync** section shows cache statistics for each account:
  - **Messages in cache:** Actual count of synced messages in the local SQLite database
  - **Oldest cached:** Date of the earliest message in cache (useful for understanding sync history)
  - **Sync window:** "All mail" or "Last N days" (configurable via settings)

**False operation error verification:**
- **Empty Trash:** After an exception, QuickMail verifies if the trash is actually empty on the server. If the trash count is zero, the operation is considered successful despite the error (TCP acknowledgment timeout).
- **Delete messages:** If deletion fails, the app shows "Delete may not have completed — refreshing" and automatically re-syncs the affected folders after 5 seconds to reconcile the UI with server state.

**Error announcements:**
- All connection, sync, and operation errors are announced through the screen reader announcement system
- Users can control which categories of announcements they hear via **Settings** → **Custom Announcements**

---

## Accessibility

- **Address Book contact editing is now fully discoverable.** The previous implicit "edit-on-populate" behavior has been replaced with explicit **Add**, **Edit**, and **Save** buttons visible in the command palette and as keyboard commands (`contacts.add`, `contacts.edit`, `contacts.save`, `contacts.cancelEdit`).
- **Readonly fields support full cursor navigation.** The Name and Email fields in display mode allow arrow-key navigation, Home/End, selection, and copying without allowing text modification. Screen reader users can read the content using their reader's standard text access commands.
- **Error feedback is visible and announced.** When an email conflict is detected during edit, an error message appears inline and is announced to screen readers.
- **Field labels are properly associated.** The Name and Email fields use `AutomationProperties.LabeledBy` so screen readers announce the label when focusing each field.
- **Edit-mode button set is distinct from display-mode buttons.** When editing, the **Add** / **Edit** / **Delete** buttons are replaced with **Save** and **Cancel** buttons, making the current mode clear to all users.

---

## Bug Fixes

- **Contact editing was impossible.** The previous design auto-populated name/email fields when a contact was selected, but immediately cleared the selection when the user started typing, losing the email address association. Changing an email address created a new contact instead of updating the existing one. This is now fixed with explicit edit modes.
- **No way to discover how to edit a contact.** There were no visible affordances (buttons, menu options, keyboard shortcuts) for editing. Now **Edit** is a prominent button and a registered keyboard command.
- **Readonly TextBox behavior was unintuitive for screen reader users.** The previous `IsReadOnly="True"` binding prevented cursor navigation entirely. Fields now allow full keyboard navigation while preventing text modification.
- **Contact.Display property was serialized to JSON redundantly.** The computed `Display` property (formatted as "Name <email>") was being written to `contacts.json` alongside the authoritative `DisplayName` and `EmailAddress` fields, creating noise in the file. The property is now marked `[JsonIgnore]`.
- **Cache statistics queries used wrong column names.** The queries backing the new "Messages in cache" and "Oldest cached" fields in Account Properties referenced stale column names from the pre-migration schema. The queries now use the correct column names and return accurate results.

---

## Thank You to Contributors

Thank you to everyone who has contributed to QuickMail through code, bug reports, feature suggestions, and other feedback. Your contributions make the project better for everyone.

**Code contributions:**
- [CityDweller](https://github.com/CityDweller) — Microsoft Graph backend groundwork: IMailService refactoring, string MessageId migration, backend router and feature gate scaffolding
- [Menelion](https://github.com/Menelion) — Windows installer (Inno Setup)
- [serrebidev](https://github.com/serrebidev) — Pooled IMAP connections, virtualized folder picker, HTML rendering improvements

**Issues and feedback:**
- [CityDweller](https://github.com/CityDweller)
- [Dennisl123](https://github.com/Dennisl123)
- [jaybird110127](https://github.com/jaybird110127)
- [KE8UPE](https://github.com/KE8UPE)
- [paoscripts](https://github.com/paoscripts)
- [serrebidev](https://github.com/serrebidev)
- [slannon97](https://github.com/slannon97)
- [sofquipeut](https://github.com/sofquipeut)
- [taylorarndt](https://github.com/taylorarndt)

Note: Others have made contributions through various social media platforms that are not listed here yet.

---

## Internal

### Address Book

- `IContactService.UpdateContactAsync(id, displayName, emailAddress)` — new method for updating a contact's name and email with email-conflict detection. Returns false if another contact owns the target email address.
- `AddressBookViewModel` — refactored with explicit `BeginAddContactCommand`, `BeginEditContactCommand`, `SaveContactCommand`, and `CancelEditCommand`. Removed the implicit "populate-on-select" behavior and replaced it with explicit mode state (`IsEditingContact`, `EditName`, `EditEmail`, `ContactError`).
- `ContactFieldBox_PreviewKeyDown` — new keyboard handler that allows navigation keys (arrows, Tab, Home, End, Ctrl+A) in readonly mode while blocking all text-modification operations.
- `ContactModel.Display` — now marked `[JsonIgnore]` to prevent redundant JSON serialization.

### Connection Health

- `IMailService.AccountReachabilityChanged` — new event fired when a connection is lost or recovered. Backends (IMAP, Graph) raise this; `MailServiceRouter` aggregates and forwards to the UI layer.
- `IMailService.CountTrashMessagesAsync()` — new method for post-failure trash verification. Used to determine if an Empty Trash operation succeeded despite an exception.
- `ISyncService.SyncProgressChanged` — new event fired after each folder sync completes with `(completedFolders, totalFolders)` for progress reporting.
- `ILocalStoreService.CountSummariesAsync()` and `GetOldestMessageDateAsync()` — new methods for cache statistics in Account Properties. Handle graceful failures in `--online` mode.
- `MainViewModel.LastSyncText` — new property displays "Synced HH:MM", "Never synced", or "In progress" in the status bar.
- `MainViewModel.StartPeriodicNoOpAsync()` — new background task that sends NOOP commands every 10 minutes to keep connections alive.
- `ConnectOneAccountAsync()` — now implements 3-attempt startup retry with exponential backoff.
- `StartBackgroundSyncAsync()` — updated to subscribe to reachability events, report sync progress, and wire error announcements.
- `ImapMailService.RunIdleWatcherAsync()` — updated to fire `AccountReachabilityChanged` on connection loss/recovery and implement exponential backoff for reconnection attempts.
- `EmptyTrashAsync()` and `DeleteMessagesAsync()` — updated with post-failure verification and reconciliation.
- Status bar unified into single TextBlock with Run elements (StatusText + LastSyncText) for coherent screen reader reading.
- New `StringToVisibilityConverter` XAML converter.
- 465 tests, all green.

---

## Known Limitations

### Address Book

- **Email address changes are not merged with existing contacts.** If you change a contact's email from `old@example.com` to `new@example.com`, the old email entry remains in the address book if it was separate. A future merge/deduplicate feature will address this.
- **Contacts are not automatically harvested from incoming mail.** Contacts must be added manually via the **Grab Addresses** feature (Ctrl+Shift+B on a message) or the Compose window's right-click "Add to Address Book" option.

### Connection Health

- **Delete operations show uncertainty message.** If a delete fails, the message "Delete may not have completed — refreshing" appears. The UI is reconciled after 5 seconds via targeted re-sync. In rare cases on very slow networks, the reconciliation might lag behind the user's next action.
- **IDLE watcher reconnection uses exponential backoff with 120-second cap.** If a connection is unstable and drops repeatedly, reconnection attempts max out at 120 seconds between tries. A future feature may add user-configurable backoff settings.
- **--online mode provides no connection recovery.** When running with `--online` flag, there is no cache and no persistence of sync state, so connection interruptions cannot be recovered gracefully. Normal mode (with SQLite cache) is recommended for reliable sync.
