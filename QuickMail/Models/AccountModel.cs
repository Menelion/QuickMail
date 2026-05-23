using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickMail.Models;

public partial class AccountModel : ObservableObject
{
    // ── Persistent fields (serialized to accounts.json) ──────────────────────────

    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public AuthType AuthType { get; set; } = AuthType.Password;

    // IMAP
    public string ImapHost { get; set; } = string.Empty;
    public int ImapPort { get; set; } = 993;
    public bool ImapUseSsl { get; set; } = true;
    public bool ImapAcceptInvalidCert { get; set; } = false;

    // SMTP
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = false; // STARTTLS on 587
    public bool SmtpAcceptInvalidCert { get; set; } = false;

    /// <summary>When true, this account is pre-selected when composing a new message.</summary>
    public bool IsDefault { get; set; } = false;

    // ── Runtime-only status (not serialized, updated after each connection) ──────

    [ObservableProperty]
    [property: JsonIgnore]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    private bool _isConnected;

    [ObservableProperty]
    [property: JsonIgnore]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    private int _inboxUnread;

    [ObservableProperty]
    [property: JsonIgnore]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    private int _inboxTotal;

    // ── Computed labels ───────────────────────────────────────────────────────────

    public string AccountLabel => string.IsNullOrWhiteSpace(AccountName) ? Username : AccountName;
    public string SenderDisplayName => string.IsNullOrWhiteSpace(DisplayName) ? AccountLabel : DisplayName;
    public string AccountLabelWithDefault => IsDefault ? $"{AccountLabel} - default" : AccountLabel;

    /// <summary>
    /// Short status line shown below the account name in the account list, and as a tooltip.
    /// Examples: "Disconnected", "Connected", "Connected — 5 unread, 44 total"
    /// </summary>
    [JsonIgnore]
    public string StatusLabel
    {
        get
        {
            if (!IsConnected) return "Disconnected";
            if (InboxTotal == 0) return "Connected";
            return InboxUnread > 0
                ? $"Connected — {InboxUnread} unread, {InboxTotal} total"
                : $"Connected — {InboxTotal} total";
        }
    }

    /// <summary>
    /// Full accessible name for screen readers: account label + connection status + inbox counts.
    /// Placed in AutomationProperties.Name on the list item container so it is announced on focus
    /// without requiring the user to hover.
    /// Examples: "Idea Place, disconnected", "Kelly, connected, 5 unread, 44 total"
    /// </summary>
    [JsonIgnore]
    public string AccessibleName
    {
        get
        {
            if (!IsConnected) return $"{AccountLabel}, disconnected";
            if (InboxTotal == 0) return $"{AccountLabel}, connected";
            return InboxUnread > 0
                ? $"{AccountLabel}, connected, {InboxUnread} unread, {InboxTotal} total"
                : $"{AccountLabel}, connected, {InboxTotal} total";
        }
    }

    public override string ToString() => AccountLabel;
}
