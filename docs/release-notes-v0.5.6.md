# QuickMail v0.5.6 Release Notes

## New Features

### Address book

A new built-in address book stores email addresses and display names for quick reuse. Contacts can be added manually, edited, or imported directly from messages. The address book is stored as human-readable JSON in your AppData folder alongside other QuickMail settings.

- Open **File → Address Book** (or press `Ctrl+Shift+B`) to manage contacts
- Search by name or email address
- Click on a contact to view and edit its details

### Grab addresses from messages

While reading a message, quickly save sender, recipient, and reply-to addresses to your address book in bulk:

- Open **Message → Grab Addresses from Message** (or press `Ctrl+Shift+G`)
- A dialog shows all addresses found in the message (From, To, Cc)
- All addresses are checked by default; uncheck any you don't want to save
- Click **Save** to add selected addresses to your address book

### Address autocomplete in compose

When composing a message, matching contacts from your address book appear as you type in the **To**, **Cc**, or **Bcc** fields:

- Type at least one character to see suggestions, sorted by recency
- Press **Down** arrow to move into the suggestion list
- Press **Enter** or **Tab** to insert a contact
- The autocomplete respects your address separator preference (comma or semicolon) — whatever you've been using in the field, new addresses use the same separator

### Menu bar

All major features are now organized in a menu bar at the top of the window:

| Menu | Contains |
|------|----------|
| **File** | New Message, Manage Accounts, Address Book, Exit |
| **Message** | Reply, Reply All, Forward, Delete, Empty Trash, Move/Copy to Folder, Grab Addresses |
| **View** | Refresh, View Mode (Messages / Conversations / By Sender / By Recipient), Sync Range, Go to Folder, Command Palette |
| **Help** | User Guide |

All menu items show their keyboard shortcuts for quick reference.

---

## Improvements

### Accessibility

- **Grab Addresses dialog** — Focus now starts on the first address so keyboard and screen reader users can immediately interact with the checkbox list. The mysterious empty list container is no longer announced separately.
- **Address book** — Keyboard navigation and screen reader support throughout. Delete key removes selected contacts; Enter in the email field submits the add form.
- **Autocomplete focus management** — Focus doesn't automatically move to the suggestion list — you must press Down arrow to enter it. This is the correct screen-reader-friendly pattern: announcements happen, focus stays put unless the user chooses to move it.

### Address separator consistency

When inserting an address from the address book, QuickMail now detects which separator (comma or semicolon) you've been using in the field and inserts the new address with the same separator. Both formats are supported throughout: `address1, address2` or `address1; address2`.

---

## Bug Fixes

- **Autocomplete race condition** — Fast typing no longer causes stale search results to overwrite recent ones. Previous searches are properly cancelled when new text arrives.
- **Contact service thread safety** — Fixed a race condition in the contact loading logic where concurrent callers could load and cache the contact list twice. Now uses proper synchronization.
- **Non-atomic contact saves** — Contact data is now written atomically (to a temp file, then renamed) to prevent corruption if the process is interrupted mid-write.

---

## Code quality (internal)

- `IContactService` interface extracted for contact operations, replacing contact CRUD methods in `ILocalStoreService`. Contact data is now separate from mail caching.
- Contact search operations now accept `CancellationToken` to prevent autocomplete race conditions in the UI layer.
- All confirmation dialogs (e.g., contact deletion) moved from ViewModels to view code-behind, maintaining strict MVVM separation.
- Exception logging added to contact service initialization instead of silently swallowing errors.
- XAML parse tests extended to cover `AddressBookWindow` and `GrabAddressesDialog`.

---

## Updated documentation

The User Guide has been updated with complete coverage of the new address book features, keyboard shortcuts, and address separator behavior.
