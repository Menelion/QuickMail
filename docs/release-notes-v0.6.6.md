# QuickMail v0.6.6 Release Notes

## New Features

### Address chips in To, Cc, and Bcc

The **To**, **Cc**, and **Bcc** fields in the compose window now work as token fields. When you commit an address it becomes a chip — a button showing the contact's name (or email address if no name is known). Multiple addresses appear as a row of chips before the text input cursor.

**Committing an address:** Type an address or name and press **Tab**, **Enter**, **comma**, or **semicolon**. Or select a contact from the autocomplete dropdown that appears as you type.

**Keyboard navigation:**

- **Left / Right arrow** — move focus between chips
- **Right arrow** on the last chip — move back to the text input
- **Left arrow** at the start of the text input — move to the last chip
- **Delete** or **Backspace** on a focused chip — remove that address
- **Backspace** in an empty text input — remove the last chip
- **Ctrl+C** on a focused chip — copy the full address to the clipboard

**Context menu (right-click or Shift+F10 on a chip):**

- **Copy Address** — copies the full name and email to the clipboard
- **Add to Address Book** — saves the contact silently with no dialog (shows a message if the address is already saved)
- **Remove** — removes this address from the field

**Check Addresses (Ctrl+K):** Validates every address in all three fields. Addresses that cannot be validated are highlighted in red. Bare names with no @ sign are looked up in your address book — if a single match is found, the chip resolves to the full address automatically. A summary is announced when the check completes.

**Screen reader behavior:** Each chip's accessible name is the full RFC address (for example, "Kelly Ford &lt;kelly@example.com&gt;"). When you Tab into a field that already has addresses, those addresses are announced immediately so you are not left wondering whether the field is empty.

---

### Group boundary navigation in grouped views

Two new keyboard shortcuts let you jump to the start or end of a group in Conversations, By Sender, and By Recipient views:

| Shortcut | Action |
|----------|--------|
| **Shift+,** (less-than key) | Jump to the first (newest) message in the current group |
| **Shift+.** (greater-than key) | Jump to the last (oldest) message in the current group |

If the group is collapsed when you press either key, it expands automatically before moving focus. In the flat Messages view the keys do nothing. Both shortcuts are remappable via **File → Settings → Keyboard Shortcuts** and appear in the command palette.

---

### Ctrl+Enter as an alternate send shortcut

You can now send a message from the compose window with **Ctrl+Enter** in addition to the existing **Alt+S**. This is useful if you prefer not to lift your hands from the main keyboard area to reach Alt+S.

---

### Confirmation before emptying trash

**Empty Trash** (`Ctrl+Shift+E`) now shows a confirmation dialog before permanently deleting messages. The dialog reports exactly how many messages will be removed so you know what you are about to do.

If you prefer not to see the confirmation, open **File → Settings**, select the **General** tab, and turn off **Confirm before emptying trash** in the **Mail Actions** group.

---

### Junk folder support

QuickMail now recognises and tracks the **Junk** (spam) folder as a distinct special folder alongside Inbox, Drafts, Sent, and Trash. If your mail server has a dedicated Junk folder, it is no longer lumped together with Trash in QuickMail's folder handling.

---

### About dialog

**Help → About QuickMail** opens an About dialog showing the application version and a link to the MIT License on GitHub. The same dialog is accessible from the Command Palette (**Ctrl+Shift+P**) — search for **About QuickMail**.

---

## Bug Fixes

- **Reading pane closed by incoming mail** — When new mail arrived while you were reading a message, the reading pane appeared to close and focus jumped back to the message list. QuickMail now leaves focus in the reading pane when new mail arrives.

- **Trash messages reappear after Empty Trash** — If you emptied trash and then changed the sort order or applied a filter, previously deleted messages could briefly reappear in the message list. QuickMail now clears the cached message count for each account's Trash folder immediately after a successful empty, so the list stays correct.
