using System.Windows.Input;

namespace QuickMail.Models;

/// <summary>
/// A user-defined override that maps a keyboard shortcut to a command by ID.
/// Stored in hotkeys.json.
/// </summary>
public sealed class HotkeyBinding
{
    public string CommandId { get; set; } = string.Empty;

    /// <summary>Integer value of <see cref="System.Windows.Input.Key"/>.</summary>
    public int Key { get; set; }

    /// <summary>Integer value of <see cref="System.Windows.Input.ModifierKeys"/>.</summary>
    public int Modifiers { get; set; }
}
