using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuickMail.Models;

namespace QuickMail.Services;

public class RuleService : IRuleService
{
    private readonly string _filePath;
    private readonly IImapService _imap;
    private readonly ILocalStoreService _store;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private List<MailRule> _cache = [];
    private bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public RuleService(IImapService imap, ILocalStoreService store, string? dataDirectory = null)
    {
        _imap = imap;
        _store = store;
        var dir = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMail");
        _filePath = Path.Combine(dir, "rules.json");
    }

    // ── Load / Save ─────────────────────────────────────────────────────────

    public List<MailRule> LoadRules()
    {
        if (_loaded) return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = [];
            _loaded = true;
            return _cache;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _cache = JsonSerializer.Deserialize<List<MailRule>>(json) ?? [];
        }
        catch
        {
            _cache = [];
        }
        _loaded = true;
        return _cache;
    }

    public void SaveRules(List<MailRule> rules)
    {
        _cache = rules;
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        // Atomic write: write to temp file, then rename.
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(rules, JsonOptions));
        File.Move(tempPath, _filePath, overwrite: true);
        _loaded = true;
    }

    // ── Rule Execution ──────────────────────────────────────────────────────

    public async Task<int> ApplyRulesAsync(
        List<MailMessageSummary> incoming,
        Guid accountId,
        CancellationToken ct)
    {
        var rules = LoadRules();
        var enabledRules = rules.Where(r => r.IsEnabled).ToList();
        if (enabledRules.Count == 0) return 0;

        int matchedCount = 0;

        foreach (var rule in enabledRules)
        {
            ct.ThrowIfCancellationRequested();

            // Account scope check
            if (rule.AccountId.HasValue && rule.AccountId.Value != accountId)
                continue;

            var matched = incoming.Where(m => MatchesRule(rule, m)).ToList();
            if (matched.Count == 0) continue;

            matchedCount += matched.Count;

            try
            {
                await ExecuteActionAsync(rule, matched, accountId, ct);

                // Remove messages from incoming that were moved or deleted so the
                // UI doesn't show them in the original folder after FolderSynced fires.
                if (rule.Action is RuleAction.MoveToFolder or RuleAction.Delete)
                {
                    var matchedKeys = new HashSet<(uint Uid, Guid AccountId, string FolderName)>();
                    foreach (var m in matched)
                        matchedKeys.Add((m.UniqueId, m.AccountId, m.FolderName));
                    incoming.RemoveAll(m => matchedKeys.Contains((m.UniqueId, m.AccountId, m.FolderName)));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LogService.Log($"Rule '{rule.Name}' action failed", ex);
            }
        }

        return matchedCount;
    }

    public List<MailMessageSummary> TestRule(MailRule rule, IEnumerable<MailMessageSummary> messages)
    {
        return messages.Where(m => MatchesRule(rule, m)).ToList();
    }

    // ── Condition Matching ──────────────────────────────────────────────────

    private static bool MatchesRule(MailRule rule, MailMessageSummary msg)
    {
        if (rule.UseFromCondition
            && !string.IsNullOrEmpty(rule.FromContains)
            && !msg.From.Contains(rule.FromContains, StringComparison.OrdinalIgnoreCase))
            return false;

        if (rule.UseToCondition
            && !string.IsNullOrEmpty(rule.ToContains)
            && !msg.To.Contains(rule.ToContains, StringComparison.OrdinalIgnoreCase))
            return false;

        if (rule.UseSubjectCondition
            && !string.IsNullOrEmpty(rule.SubjectContains)
            && !msg.Subject.Contains(rule.SubjectContains, StringComparison.OrdinalIgnoreCase))
            return false;

        if (rule.UseBodyCondition
            && !string.IsNullOrEmpty(rule.BodyContains)
            && (msg.Preview == null || !msg.Preview.Contains(rule.BodyContains, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (rule.MustHaveAttachments && !msg.HasAttachments)
            return false;

        return true;
    }

    // ── Action Execution ────────────────────────────────────────────────────

    private async Task ExecuteActionAsync(
        MailRule rule,
        List<MailMessageSummary> matched,
        Guid accountId,
        CancellationToken ct)
    {
        switch (rule.Action)
        {
            case RuleAction.MarkAsRead:
                await MarkAsReadAsync(matched, ct);
                break;

            case RuleAction.MarkAsUnread:
                await MarkAsUnreadAsync(matched, ct);
                break;

            case RuleAction.MoveToFolder:
                if (string.IsNullOrEmpty(rule.TargetFolder)) break;
                await MoveToFolderAsync(matched, rule.TargetFolder, ct);
                break;

            case RuleAction.Delete:
                await DeleteAsync(matched, ct);
                break;
        }
    }

    private async Task MarkAsReadAsync(List<MailMessageSummary> messages, CancellationToken ct)
    {
        foreach (var msg in messages)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await _imap.MarkReadAsync(msg.AccountId, msg.FolderName, msg.UniqueId, ct);
                msg.IsRead = true;
                await _store.UpdateIsReadAsync(msg.AccountId, msg.FolderName, msg.UniqueId, true);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LogService.Log($"MarkRead failed for UID {msg.UniqueId}", ex);
            }
        }
    }

    private async Task MarkAsUnreadAsync(List<MailMessageSummary> messages, CancellationToken ct)
    {
        foreach (var msg in messages)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // ImapService doesn't have a dedicated MarkUnreadAsync — we update
                // the local store only. Full IMAP unread will be added in a follow-up.
                msg.IsRead = false;
                await _store.UpdateIsReadAsync(msg.AccountId, msg.FolderName, msg.UniqueId, false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LogService.Log($"MarkUnread failed for UID {msg.UniqueId}", ex);
            }
        }
    }

    private async Task MoveToFolderAsync(
        List<MailMessageSummary> messages, string targetFolder, CancellationToken ct)
    {
        // Group messages by (AccountId, FolderName) so we issue one MOVE per source folder.
        var groups = messages.GroupBy(m => (m.AccountId, m.FolderName));
        foreach (var group in groups)
        {
            ct.ThrowIfCancellationRequested();
            var uids = group.Select(m => m.UniqueId).ToList();
            try
            {
                await _imap.MoveMessagesAsync(
                    group.Key.AccountId, group.Key.FolderName, uids, targetFolder, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LogService.Log($"MoveToFolder failed for {uids.Count} messages to '{targetFolder}'", ex);
            }
        }
    }

    private async Task DeleteAsync(List<MailMessageSummary> messages, CancellationToken ct)
    {
        var groups = messages.GroupBy(m => (m.AccountId, m.FolderName));
        foreach (var group in groups)
        {
            ct.ThrowIfCancellationRequested();
            var uids = group.Select(m => m.UniqueId).ToList();
            try
            {
                await _imap.MoveToTrashBatchAsync(
                    group.Key.AccountId, group.Key.FolderName, uids, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LogService.Log($"Delete (move to trash) failed for {uids.Count} messages", ex);
            }
        }
    }
}
