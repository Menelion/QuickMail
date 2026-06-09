# Address Book — Contacts Tab Redesign Spec

## Context

The Contacts tab in the Address Book grew incrementally (Grab Addresses first, then groups) and
never received a coherent contact-management design. Three distinct problems need fixing:

1. **JSON redundancy**: `ContactModel.Display` is a computed property with no `[JsonIgnore]`
   attribute. `System.Text.Json` serializes any public getter, so every `contacts.json` entry
   contains a redundant `"Display": "Name <email>"` key alongside the authoritative `DisplayName`
   and `EmailAddress` fields. It is never read back (no setter), just noise in the file.

2. **Broken edit flow**: The current "edit" is implicit — selecting a contact auto-populates the
   Name/Email form, but `NewNameBox_TextChanged` and `NewEmailBox_TextChanged` immediately clear
   the selection the moment the user starts typing. This makes editing impossible: changing the
   name loses the email address association, and changing the email creates a new contact instead
   of updating the old one.

3. **No discoverable actions**: There is no Add, Edit, or Update button — the only affordances
   are "Add" (which also secretly edits if the email matches) and "Delete". New users have no way
   to discover how to change a contact's name.

This spec redesigns the Contacts tab around explicit modes: **Display**, **Adding**, and
**Editing**. The Groups tab is not touched.

---

## Fix 1 — JSON redundancy (ContactModel)

**File:** `QuickMail/Models/ContactModel.cs`

Add `[JsonIgnore]` to `Display`:

```csharp
using System.Text.Json.Serialization;

[JsonIgnore]
public string Display => ...;
```

Existing `contacts.json` files that contain the stale `Display` field will be silently ignored on
next load (`System.Text.Json` skips unknown properties by default) and will be cleaned up on the
next save.

---

## Fix 2 — Service layer

### Add `UpdateContactAsync` to `IContactService`

**File:** `QuickMail/Services/IContactService.cs`

```csharp
/// <summary>
/// Updates an existing contact by id. Returns false if a different contact
/// already owns the target email address (caller should surface the conflict).
/// </summary>
Task<bool> UpdateContactAsync(int id, string displayName, string emailAddress);
```

### Implement in `ContactService`

**File:** `QuickMail/Services/ContactService.cs`

Inside `_loadLock`:
1. Find contact by `id`; throw `InvalidOperationException` if not found.
2. If `emailAddress` changed: check no *other* contact already owns that email
   (case-insensitive). Return `false` if conflict.
3. Update `DisplayName`, `EmailAddress`, `LastUsedTicks = UtcNow`.
4. Call `SaveContactsAsyncLocked()`. Return `true`.

`UpsertContactAsync` (used by Grab Addresses and Compose) is unchanged.

---

## Fix 3 — ViewModel redesign

**File:** `QuickMail/ViewModels/AddressBookViewModel.cs`

### Remove
- `NewName`, `NewEmail` observable properties
- `AddContactCommand` (replaced by mode-aware `SaveContactCommand`)

### Add

```csharp
// Editable fields — always bound to the Name/Email TextBoxes.
// In Display mode they mirror SelectedContact; in Add/Edit mode
// the user modifies them directly.
[ObservableProperty] private string _editName  = string.Empty;
[ObservableProperty] private string _editEmail = string.Empty;

// True while in Add or Edit mode.
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsContactReadOnly))]
[NotifyPropertyChangedFor(nameof(CanEditContact))]
[NotifyPropertyChangedFor(nameof(CanDeleteContact))]
private bool _isEditingContact = false;

private enum ContactEditMode { None, Adding, Editing }
private ContactEditMode _contactEditMode = ContactEditMode.None;
private int _editingContactId;  // id of contact being edited

public bool IsContactReadOnly => !IsEditingContact;
public bool CanEditContact    => HasSelectedContact && !IsEditingContact;
public bool CanDeleteContact  => HasSelectedContact && !IsEditingContact;

[ObservableProperty] private string _contactError = string.Empty;
```

### Selection → display (replaces code-behind PropertyChanged hack)

```csharp
partial void OnSelectedContactChanged(ContactModel? value)
{
    if (!IsEditingContact)
    {
        EditName     = value?.DisplayName  ?? string.Empty;
        EditEmail    = value?.EmailAddress ?? string.Empty;
        ContactError = string.Empty;
    }
}
```

Also add `[NotifyPropertyChangedFor(nameof(CanEditContact))]` and
`[NotifyPropertyChangedFor(nameof(CanDeleteContact))]` to the `_selectedContact` field
so the Edit/Delete button enabled states update when selection changes.

### Commands

**`BeginAddContactCommand`**:
- Set `EditName = ""`, `EditEmail = ""`, `ContactError = ""`
- `IsEditingContact = true`, `_contactEditMode = Adding`
- Announce: `"Add contact"` (Result)

**`BeginEditContactCommand`** (enabled when `CanEditContact`):
- Snapshot `_editingContactId = SelectedContact.Id`
- `IsEditingContact = true`, `_contactEditMode = Editing`, `ContactError = ""`
- Announce: `"Edit {contact.DisplayName}"` (Result)

**`CancelEditCommand`**:
- `IsEditingContact = false`, `_contactEditMode = None`
- Restore `EditName`/`EditEmail` from `SelectedContact`
- `ContactError = ""`
- Announce: `"Edit cancelled"` (Result)

**`SaveContactCommand`** (async):
- Validate: `EditEmail` non-empty → set `ContactError` and `return` if not
- If `Adding`:
  - Call `UpsertContactAsync(new ContactModel { ... })`
  - Reload; select the upserted contact by email
  - `IsEditingContact = false`, `_contactEditMode = None`
  - Announce: `"{name} added"` (Result)
- If `Editing`:
  - Call `UpdateContactAsync(_editingContactId, EditName.Trim(), EditEmail.Trim())`
  - If returns `false`: `ContactError = "Email address is already used by another contact"`,
    announce same (Result), `return`
  - Reload; restore selection by id
  - `IsEditingContact = false`, `_contactEditMode = None`
  - Announce: `"{name} updated"` (Result)

---

## Fix 4 — XAML redesign

**File:** `QuickMail/Views/AddressBookWindow.xaml`

Add to `Window.Resources`:
```xml
<BooleanToVisibilityConverter x:Key="BoolToVis"/>
```

Replace the old `<!-- Add-contact form + action buttons -->` Grid (7-column layout) with a
3-row Grid:

- **Row 0**: Name label + `ContactNameBox` (IsReadOnly=IsContactReadOnly) +
  Email label + `ContactEmailBox` (IsReadOnly=IsContactReadOnly).
  Both TextBoxes bound to `EditName`/`EditEmail` with `PreviewKeyDown="EditFieldBox_PreviewKeyDown"`.
- **Row 1**: Two overlapping `DockPanel` rows (one visible at a time):
  - *Display mode* (`Visibility="{Binding IsContactReadOnly, Converter={StaticResource BoolToVis}}"`)
    — Add, Edit, Delete buttons + spacer + Close
  - *Edit mode* (`Visibility="{Binding IsEditingContact, Converter={StaticResource BoolToVis}}"`)
    — Save, Cancel buttons + spacer + Close
- **Row 2**: Error `TextBlock` bound to `ContactError`, hidden via `DataTrigger` when empty.

Neither Close button in the contacts tab uses `IsCancel="True"` (Escape is handled entirely by
`Window_PreviewKeyDown`; see Fix 5).

---

## Fix 5 — Code-behind cleanup

**File:** `QuickMail/Views/AddressBookWindow.xaml.cs`

### Remove
- `NewNameBox_TextChanged` handler
- `NewEmailBox_TextChanged` handler
- `NewEmailBox_PreviewKeyDown` handler
- The `vm.PropertyChanged` subscription block that auto-populated `vm.NewName`/`vm.NewEmail`

### Add / change

- **`EditFieldBox_PreviewKeyDown`**: Enter → `_vm.SaveContactCommand.ExecuteAsync(null)`
  (but only if `_vm.IsEditingContact`). Escape → `_vm.CancelEditCommand.Execute(null)`.

- **`ContactList_PreviewKeyDown`**: add `F2` → `_vm.BeginEditContactCommand.Execute(null)`
  when `_vm.CanEditContact`.

- **`Window_PreviewKeyDown` Escape branch**: add a check for `_vm.IsEditingContact` before
  the `Close()` call. Order:
  1. `GroupNameEntryPanel` visible → hide it
  2. `IsEditingContact` → cancel edit
  3. Otherwise → `Close()`

- **Focus-on-edit**: subscribe to `vm.PropertyChanged` for `IsEditingContact`; when it becomes
  `true`, call `ContactNameBox.Focus()` and `ContactNameBox.SelectAll()`.

- **`RegisterCommands`**: add palette-visible commands for
  `contacts.addContact` (Add Contact), `contacts.editContact` (Edit Contact),
  `contacts.saveContact` (Save Contact), `contacts.cancelEdit` (Cancel Edit).
  `editContact` and `saveContact`/`cancelEdit` use `isAvailable:` guards based on
  `MainTabs.SelectedIndex == 0` and `_vm.CanEditContact`/`_vm.IsEditingContact`.

- **`DeleteSelectedContactsAsync`**: change `IsEnabled` check to `CanDeleteContact`
  (already guarded by code, but the XAML binding changes too).

---

## Keyboard Walkthrough

### Walkthrough A — View and copy a contact's email address

1. User opens Address Book (Ctrl+Shift+B). Focus: Search box.
2. User presses Tab. Focus: Contact list.
3. User arrows to a contact. Screen reader announces: `"Jane Smith <jane@example.com>"`. The
   Name and Email fields below show `Jane Smith` and `jane@example.com` (readonly).
4. User presses Tab. Focus: Contact Name field (readonly). User can arrow through the text.
5. User presses Tab. Focus: Contact Email field (readonly). User can arrow through the email.
6. User presses Escape. Window closes.

### Walkthrough B — Add a new contact

1. User presses Tab past the contact list to reach the Add button.
2. User activates **Add**. Screen reader announces: `"Add contact"`. Focus moves to the Name
   field; it is now editable.
3. User types a name (`Jane Smith`), presses Tab.
4. User types an email address (`jane@example.com`), presses Enter or activates **Save**.
5. Screen reader announces: `"Jane Smith added."`.
6. Fields return to readonly. Focus moves to the contact list on the newly added contact.

### Walkthrough C — Edit an existing contact's name

1. User selects `Jane Smith` in the contact list.
2. User presses **F2** or activates **Edit**. Screen reader announces: `"Edit Jane Smith"`.
   Name field becomes editable and receives focus. It contains `Jane Smith`.
3. User clears the name and types `Jane A. Smith`. Presses Tab.
4. User leaves email unchanged. User presses Enter or activates **Save**.
5. Screen reader announces: `"Jane A. Smith updated."`.
6. Fields return to readonly. Contact list shows updated name.

### Walkthrough D — Edit an existing contact's email address (conflict)

Same as C, except user modifies the Email field. If the new email is already owned by another
contact, after pressing Save:
- Screen reader announces: `"Email address is already used by another contact."`
- Error TextBlock appears. Fields remain editable. User corrects and retries.

### Walkthrough E — Cancel an edit

1. User selects a contact, presses Edit or F2.
2. User modifies the name.
3. User presses **Escape** or activates **Cancel**. Screen reader announces: `"Edit cancelled."`
4. Fields revert to original values (readonly). Focus returns to the contact list.

### Walkthrough F — Delete a contact

1. User selects a contact. Activates **Delete** or presses `Delete` key.
2. Confirmation dialog: `"Delete Jane Smith <jane@example.com>?"`.
3. User confirms. Screen reader announces: `"Jane Smith deleted."`. Focus moves to next item.

---

## Infrastructure Changes

| Area | Change |
|---|---|
| `ContactModel.cs` | Add `[JsonIgnore]` to `Display` |
| `IContactService.cs` | Add `Task<bool> UpdateContactAsync(int id, string name, string email)` |
| `ContactService.cs` | Implement `UpdateContactAsync` |
| `StubServices.cs` | Add no-op `UpdateContactAsync` to `StubContactService` |
| `AddressBookViewModel.cs` | Remove `NewName`, `NewEmail`, `AddContactCommand`; add edit-mode state and four new commands |
| `AddressBookWindow.xaml` | Replace 7-column bottom Grid with 3-row Grid; add `BoolToVis` converter |
| `AddressBookWindow.xaml.cs` | Remove three handlers + PropertyChanged subscription; add F2 wiring, focus-on-edit, Escape branch, palette commands |
| `CommandRegistry` | No main-window change — contact edit commands go in the address book's local palette only |
| F6 ring | No change — no new panes |
| `AutomationProperties.Name` | New TextBoxes: `"Contact name"`, `"Contact email"`; buttons: `"Add contact"`, `"Edit contact"`, `"Save contact"`, `"Cancel edit"` |
| Announcements | BeginAdd: `"Add contact"` (Result); BeginEdit: `"Edit {name}"` (Result); Save-add: `"{name} added"` (Result); Save-edit: `"{name} updated"` (Result); Cancel: `"Edit cancelled"` (Result); conflict: `"Email address is already used by another contact"` (Result) |

---

## Out of Scope

- **Merging duplicates**: Two contacts with the same email but different display names —
  detection and merge UI is a separate feature.
- **Import/export**: CSV or vCard import/export is not included.
- **Multiple email addresses per contact**: The one-email-per-contact model is unchanged.
- **Sorting options**: The list remains sorted by `DisplayName` then `EmailAddress`.
- **Groups tab**: No changes. Adding/removing contacts from groups remains on the Groups tab.
- **Automatic contact harvesting from incoming mail**: Contacts are still only added via Grab
  Addresses or the Compose window right-click.
- **The stale `Display` field in existing `contacts.json`**: Silently disappears after the next
  write. No migration script is needed.

---

## Tests to Add

**`QuickMail.Tests/AddressBookViewModelContactTests.cs`** (new file):

- `BeginAddContact_ClearsFieldsAndSetsEditingMode`
- `BeginEditContact_PopulatesFieldsFromSelectedContact`
- `CancelEdit_RestoresFieldsAndClearsEditingMode`
- `SaveContact_Adding_CreatesContactAndExitsEditMode`
- `SaveContact_Editing_UpdatesContactAndExitsEditMode`
- `SaveContact_Editing_EmailConflict_SetsErrorAndStaysInEditMode`
- `SelectingContact_WhileNotEditing_UpdatesDisplayFields`
- `SelectingContact_WhileEditing_DoesNotClobberEditFields`
- `UpdateContact_ChangesNameAndEmail` (service-level)
- `UpdateContact_EmailConflict_ReturnsFalse` (service-level)
- `UpdateContact_SameEmail_UpdatesNameSuccessfully` (service-level)
