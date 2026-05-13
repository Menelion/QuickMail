using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace QuickMail.Models;

/// <summary>
/// Metadata for a single named action that can appear in the command palette
/// and optionally be triggered by a keyboard shortcut.
/// </summary>
public sealed class CommandDefinition
{
    public string Id              { get; }
    public string Category        { get; }
    public string Title           { get; }
    public string? Description    { get; }
    public Key DefaultKey         { get; }
    public ModifierKeys DefaultModifiers { get; }
    public Action Execute         { get; }
    public Func<bool>? IsAvailable { get; }

    public CommandDefinition(
        string id,
        string category,
        string title,
        Action execute,
        Key defaultKey = Key.None,
        ModifierKeys defaultModifiers = ModifierKeys.None,
        string? description = null,
        Func<bool>? isAvailable = null)
    {
        Id             = id;
        Category       = category;
        Title          = title;
        Execute        = execute;
        DefaultKey     = defaultKey;
        DefaultModifiers = defaultModifiers;
        Description    = description;
        IsAvailable    = isAvailable;
    }

    /// <summary>Human-readable shortcut string, e.g. "Ctrl+N" or "Delete".</summary>
    public string GestureText
    {
        get
        {
            if (DefaultKey == Key.None) return string.Empty;
            var parts = new List<string>();
            if ((DefaultModifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((DefaultModifiers & ModifierKeys.Shift)   != 0) parts.Add("Shift");
            if ((DefaultModifiers & ModifierKeys.Alt)     != 0) parts.Add("Alt");
            var keyStr = DefaultKey.ToString();
            // Key.D0–Key.D9 stringify as "D0"–"D9"; show just the digit.
            if (keyStr.Length == 2 && keyStr[0] == 'D' && char.IsDigit(keyStr[1]))
                keyStr = keyStr[1..];
            parts.Add(keyStr);
            return string.Join("+", parts);
        }
    }
}
