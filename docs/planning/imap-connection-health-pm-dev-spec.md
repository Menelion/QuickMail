# IMAP Connection Health, Sync Visibility, and Error Noise — PM/Dev Spec

## Context

QuickMail surfaces too much connection noise and too little useful sync information. Users with
screen readers hear "Syncing mail…", "Connection failed: …", "Empty trash failed: …", and
"Disconnected" labels on accounts that are functionally working. Professional clients like Outlook
handle connection lifecycle silently and show meaningful status only when the user asks for it.

Three root problems drive the user experience:

1. **False "disconnected" labels.** `AccountModel.IsConnected` is set once at startup and never
   updated. If a connection times out at startup (30 s limit), the account shows "Disconnected"
   for the entire session with no automatic retry. If pool connections die mid-session, the label
   still shows "Connected" even though the next operation will silently create a fresh connection.
   Neither state is accurate.

2. **False operation errors.** `EmptyTrashAsync` issues `SELECT → SEARCH → STORE +Deleted →
   EXPUNGE` sequentially. If the TCP connection drops after the server completes `EXPUNGE` but
   before the `OK` acknowledgement is read, MailKit throws an `IOException`. The status bar says
   "Empty trash failed" even though the server deleted every message. The same pattern affects
   `DeleteMessagesAsync`. There is no post-failure verification step.

3. **No useful sync visibility.** The indeterminate spinner and "Syncing mail…" string during
   startup say nothing about whether sync is 10% or 99% done. There is no "last synced at" time
   visible anywhere except the Properties window (which requires Alt+Enter on an account). There
   is no indication of the sync window gap (only the last 30 days / 500 messages are fetched;
   older mail is on the server but not locally cached — the user cannot tell this from the UI).

Additionally, `AnnounceLoadingProgressAsync` — a method that should announce "N messages loaded
so far" every 10 seconds during long syncs — exists in `MainViewModel.cs` but is **never called**.

---

## Technical Background

Findings from a deep review of `ImapMailService.cs`, `SyncService.cs`, and `MainViewModel.cs`:

- **`IsConnected` is a startup snapshot.** `ApplyAccountStatus` sets it once at connect time and
  never again. The IDLE watcher reconnects silently but `IsConnected` never reflects recovery.
- **`IsClientUsable` is a local flag check**, not a network probe. Silently dropped connections
  (no TCP FIN received yet) pass this test and only fail when the next command is sent.
- **IDLE watcher uses a flat 60-second retry** with no exponential backoff. Reconnects are
  unlimited but always at the same cadence.
- **IDLE covers INBOX only.** Other folders are never pushed; they rely entirely on the
  one-time startup sync.
- **No recurring background sync.** The pattern is: one startup sweep + IDLE push for INBOX.
  There is no `PeriodicTimer` or poll loop after startup.
- **`EmptyTrashAsync`** issues EXPUNGE and then reads the server's acknowledgement. If the TCP
  connection drops after EXPUNGE succeeds on the server, MailKit throws and the UI says "failed."
- **`SyncProgressChanged` event does not exist.** There is no way for SyncService to report
  per-folder progress to the ViewModel. The "Syncing mail…" string is static for the whole sweep.
- **`AnnounceLoadingProgressAsync`** exists in `MainViewModel.cs` but has no call sites — it is
  dead code.
- **`SyncService.LastSyncedUtc`** exists and is already used in the Properties window. It is not
  shown anywhere in the status bar.

---

## Goals

- Accounts show "Disconnected" only when genuinely unreachable; reconnect automatically.
- Background connection lifecycle (IDLE reconnect, pool client replacement) is invisible to the
  user — no announcements, no status bar churn.
- Operation errors reflect reality: if the server acted on a command and TCP dropped on the ACK,
  the UI does not say the operation failed.
- "Last synced at" and cache message count are visible without opening Properties.
- Long syncs announce progress to screen reader users who have Status announcements enabled.

---

## Fix 1 — Connection startup retry and dynamic `IsConnected`

**Primary files:** `QuickMail/ViewModels/MainViewModel.cs`,
`QuickMail/Services/ImapMailService.cs`

### 1a. Startup retry with backoff in `ConnectOneAccountAsync`

Wrap the existing single-attempt connect in a loop with up to 3 attempts:

```
attempt 1: 30 s timeout → on failure, wait 15 s, try again
attempt 2: 45 s timeout → on failure, wait 30 s, try again
attempt 3: 60 s timeout → on failure, set IsConnected = false, return null
```

Only `OperationCanceledException` from the *outer* connect CTS aborts all retries immediately.
Per-attempt timeouts trigger the wait-and-retry path. Log each attempt:
`"ConnectAll/{account}: attempt {n} failed — {ex.Message}. Retrying in {delay}s."`.

### 1b. Dynamic `IsConnected` via IDLE watcher status events

Add to `IImapMailService`:

```csharp
event Action<Guid, bool>? AccountReachabilityChanged;  // (accountId, isReachable)
```

`RunIdleWatcherAsync` fires:
- `AccountReachabilityChanged(accountId, false)` — when it catches an exception and enters retry
- `AccountReachabilityChanged(accountId, true)` — when it successfully re-enters IDLE

`MainViewModel` subscribes in `StartBackgroundSyncAsync` and calls `ApplyAccountStatus` with a
synthetic `null` (false) or existing folder list (true) accordingly. **No screen reader
announcement** — this is internal bookkeeping.

### 1c. IDLE watcher exponential backoff

Replace the flat 60-second retry with:

```csharp
int retryCount = 0;
// in catch block:
var delay = retryCount == 0 ? 30 : retryCount == 1 ? 60 : 120;
retryCount++;
await Task.Delay(TimeSpan.FromSeconds(delay), ct);
// on successful re-entry to IDLE:
retryCount = 0;
AccountReachabilityChanged?.Invoke(accountId, true);
```

- 1st failure: 30 s
- 2nd failure: 60 s
- 3rd+ failures: 120 s (cap)
- Successful reconnect: reset to 0

### 1d. Periodic NOOP heartbeat for non-INBOX folders

After the initial sync completes in `StartBackgroundSyncAsync`, start a `PeriodicTimer` with a
**10-minute interval** that calls `_imap.NoOpAsync` for each connected account. This keeps pool
connections alive and detects mid-session drops. On NOOP failure, fire
`AccountReachabilityChanged(accountId, false)` and let the existing reconnect path handle
recovery. Cancel the timer when the application-level `CancellationToken` fires.

---

## Fix 2 — False operation errors (EmptyTrash, Delete)

**Primary file:** `QuickMail/ViewModels/MainViewModel.cs`
**Service change:** `IImapMailService.cs`, `ImapMailService.cs`

### 2a. EmptyTrash post-failure verification

Add to `IImapMailService` and `ImapMailService`:

```csharp
Task<int> CountTrashMessagesAsync(Guid accountId, CancellationToken ct = default);
```

Implementation: open Trash folder read-only, return `SEARCH ALL` UID count (0 if folder missing).

In `MainViewModel.EmptyTrashAsync`, after catching a non-cancellation exception:

```csharp
catch (Exception ex)
{
    LogService.Log("EmptyTrash", ex);
    try
    {
        using var verifyCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        int remaining = await _imap.CountTrashMessagesAsync(account.Id, verifyCts.Token);
        if (remaining == 0)
        {
            trashEmptied = true;  // server succeeded; fall through to success path
        }
        else
        {
            StatusText = $"Empty trash failed: {ex.Message}";
        }
    }
    catch
    {
        StatusText = $"Empty trash failed: {ex.Message}";
    }
}
```

If `remaining == 0`, the normal success path runs (status text, announcement, UI cleared) with no
error shown.

### 2b. Delete — honest uncertainty message

`DeleteMessagesAsync` already removes messages from the UI optimistically before the IMAP call.
If the IMAP call throws, the messages are gone from view but may still exist on the server. Change
the failure message from:

```csharp
StatusText = $"Delete failed: {ex.Message}";
```

to:

```csharp
StatusText = "Delete may not have completed — refreshing.";
```

Then schedule a targeted sync of the affected folders within 5 seconds:

```csharp
_ = Task.Run(async () =>
{
    await Task.Delay(5000);
    foreach (var (accountId, folder) in affectedFolders)
        await _sync.SyncOneFolderAsync(account, folder, CancellationToken.None);
});
```

This reconciles the UI with server state silently. If the delete succeeded, messages stay gone.
If it failed, they reappear.

---

## Fix 3 — Sync visibility

**Primary files:** `QuickMail/ViewModels/MainViewModel.cs`,
`QuickMail/Services/SyncService.cs`,
`QuickMail/Services/ISyncService.cs`,
`QuickMail/Views/MainWindow.xaml`,
`QuickMail/Helpers/AccountPropertiesBuilder.cs`,
`QuickMail/Services/ILocalStoreService.cs`,
`QuickMail/Services/LocalStoreService.cs`

### 3a. "Last synced" in status bar

Add to `MainViewModel`:

```csharp
[ObservableProperty] private string _lastSyncText = string.Empty;
```

Set at the end of `StartBackgroundSyncAsync` and after every IDLE-triggered targeted sync:

```csharp
LastSyncText = $"Synced {DateTime.Now:t}";
```

Add a new status bar region in `MainWindow.xaml` between `StatusText` and `ConnectionStatusText`,
visible only when `LastSyncText` is non-empty. `AutomationProperties.Name = "Last sync time"`.

### 3b. Wire `AnnounceLoadingProgressAsync`

The method exists and is correct. In `StartBackgroundSyncAsync`, start it as a concurrent task:

```csharp
using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
var progressTask = AnnounceLoadingProgressAsync(progressCts.Token);
try
{
    await _sync.SyncAllAccountsAsync(...);
}
finally
{
    progressCts.Cancel();
    await progressTask.ConfigureAwait(false);
}
```

### 3c. Folder-level sync progress

Add to `ISyncService`:

```csharp
event Action<int, int>? SyncProgressChanged;  // (completedFolders, totalFolders)
```

In `SyncService.SyncAllAccountsAsync`, count total syncable folders before the loop, then fire
after each folder completes:

```csharp
int total = accounts.Sum(a => cachedFolders.TryGetValue(a.Id, out var f) ? f.Count : 0);
int done  = 0;
// after each SyncFolderAsync call:
SyncProgressChanged?.Invoke(++done, total);
```

`MainViewModel` subscribes and updates `StatusText`:

```csharp
_sync.SyncProgressChanged += (done, total) =>
    StatusText = $"Syncing… ({done} of {total} folders)";
```

### 3d. Account Properties — Sync section

Add to `ILocalStoreService`:

```csharp
Task<int> CountSummariesAsync(Guid accountId);
Task<DateTimeOffset?> GetOldestMessageDateAsync(Guid accountId);
```

`LocalStoreService` implements these with two SQLite queries:
- `SELECT COUNT(*) FROM MessageSummary WHERE AccountId = @id`
- `SELECT MIN(Date) FROM MessageSummary WHERE AccountId = @id`

`AccountPropertiesBuilder.Build` gains two new parameters `int cacheCount` and
`DateTimeOffset? oldestDate`. A new **Sync** section appears after Authentication:

```
Sync
  Last synced:       Today at 2:34 PM     (from SyncService.LastSyncedUtc)
  Messages in cache: 2,847                (from CountSummariesAsync)
  Oldest cached:     March 8, 2026        (from GetOldestMessageDateAsync; "None" if empty)
  Sync window:       Last 30 days         (from ConfigService.SyncDays; "All mail" if 0)
```

`MainViewModel.ShowPropertiesAsync` calls both new store methods before building the properties VM.

---

## Fix 4 — ConnectionStatusText discipline

**Primary file:** `QuickMail/ViewModels/MainViewModel.cs`

`"Connection error"` in `ConnectionStatusText` is currently set on any exception in
`StartBackgroundSyncAsync`, including transient per-folder errors that don't affect the
connection. Change the outer catch to only set it when no connections were established:

```csharp
catch (Exception ex)
{
    LogService.Log("BackgroundSync", ex);
    if (_cachedFolders.Count == 0)
        ConnectionStatusText = "Connection error";
    // StatusText still shows the error for sighted users
    StatusText = $"Sync error: {ex.Message}";
}
```

Also update the Settings description for "Suppress status announcements" to explicitly name
connection/sync chatter as what this setting silences.

---

## Infrastructure Changes

| Area | Change |
|---|---|
| `IImapMailService.cs` | Add `event Action<Guid, bool>? AccountReachabilityChanged`; add `Task<int> CountTrashMessagesAsync(Guid, CancellationToken)` |
| `ImapMailService.cs` | Fire `AccountReachabilityChanged` in IDLE watcher; implement `CountTrashMessagesAsync`; add exponential backoff to IDLE retry (30/60/120 s); add periodic NOOP heartbeat via `PeriodicTimer` |
| `ISyncService.cs` | Add `event Action<int, int>? SyncProgressChanged` |
| `SyncService.cs` | Fire `SyncProgressChanged` per folder in `SyncAllAccountsAsync` |
| `ILocalStoreService.cs` | Add `Task<int> CountSummariesAsync(Guid)` and `Task<DateTimeOffset?> GetOldestMessageDateAsync(Guid)` |
| `LocalStoreService.cs` | Implement both with SQLite COUNT and MIN(Date) queries |
| `AccountPropertiesBuilder.cs` | Add Sync section (last synced, cache count, oldest date, sync window); add two new parameters to `Build` |
| `MainViewModel.cs` | Fix 1a startup retry; subscribe to `AccountReachabilityChanged`; subscribe to `SyncProgressChanged`; wire `AnnounceLoadingProgressAsync`; add `LastSyncText`; Fix 2a EmptyTrash verification; Fix 2b delete reconciliation; Fix 4 ConnectionStatusText guard |
| `MainWindow.xaml` | Add `LastSyncText` status bar region |
| `StubServices.cs` | Add no-op stubs for `AccountReachabilityChanged`, `CountTrashMessagesAsync`, `SyncProgressChanged`, `CountSummariesAsync`, `GetOldestMessageDateAsync` |

---

## Out of Scope

- **Manual "Reconnect" button** — Fix 1's automatic retry eliminates the need.
- **Per-folder IDLE** — most servers cap IDLE connections at 2–4; IDLE on non-INBOX folders
  would exhaust the pool and violate server limits. The NOOP heartbeat (Fix 1d) covers liveness.
- **Full historical mail sync** — SyncDays / InitialSyncCount cap is a deliberate performance
  decision. This spec adds *visibility* into the cap, not removal of it.
- **Configurable poll interval** — the IDLE + startup sweep model is sound; only the heartbeat
  NOOP is being added.
- **Conflict resolution for failed deletes** — the 5-second reconcile sync handles this.
- **OAuth token expiry** — MSAL's 90-day refresh token handles this silently already.
- **SMTP connection health** — send failures are already reported clearly.
- **Outlook-style "Working Offline" mode** — out of scope for this release.

---

## Verification

1. **Startup retry:** Kill network during startup. Observe log shows 3 attempts per account
   before "Disconnected". Restore network — `IsConnected` should update to true automatically.
2. **IDLE backoff:** Simulate server disconnect. Log shows 30 s, 60 s, 120 s delays.
   `AccountModel.StatusLabel` should briefly show "Disconnected" then recover to "Connected".
3. **EmptyTrash false error:** Force TCP drop after EXPUNGE. UI should show success (no error),
   trash count 0.
4. **Delete uncertainty:** Force TCP drop during delete. Status shows "will reconcile" not
   "failed". After 5 s, targeted sync runs and UI reflects actual server state.
5. **Last synced text:** Status bar shows `"Synced 2:34 PM"` after startup sync.
6. **Sync progress:** Status bar shows `"Syncing… (3 of 12 folders)"` during startup sweep.
7. **Progress announcement:** With Status announcements enabled, screen reader announces
   "N messages loaded so far" during a long initial sync.
8. **Account Properties Sync section:** Alt+Enter on account shows Sync section with all four
   rows populated.
9. **ConnectionStatusText stability:** Introduce a per-folder sync error. Confirm
   `ConnectionStatusText` stays at "N account(s) connected", not "Connection error".
