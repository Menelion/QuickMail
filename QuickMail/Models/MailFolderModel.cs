using System;

namespace QuickMail.Models;

public enum SpecialFolderKind { None, Inbox, Sent, Drafts, Trash, Junk }

public class MailFolderModel
{
    public Guid AccountId { get; set; }
    public bool IsHeader { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    /// <summary>Total number of messages in the folder as reported by the server at connection time.</summary>
    public int MessageCount { get; set; }
    /// <summary>True for Trash, Junk, Sent, and Drafts — excluded from the All Mail aggregate view.</summary>
    public bool ExcludeFromAllMail { get; set; }
    /// <summary>Identifies special-purpose folders for virtual aggregate views.</summary>
    public SpecialFolderKind Kind { get; set; }

    /// <summary>Accessibility label: headers just show the name; folders include unread count.</summary>
    public string AutomationName =>
        IsHeader ? DisplayName
        : UnreadCount > 0 ? $"{DisplayName}, {UnreadCount} unread"
        : DisplayName;

    public override string ToString() =>
        IsHeader ? DisplayName
        : UnreadCount > 0 ? $"{DisplayName} ({UnreadCount})" : DisplayName;
}
