using System;

namespace QuickMail.Services;

/// <summary>
/// Thrown when creating or renaming an address book group to a name that already
/// exists (case-insensitive). Group names must be unique so a user can never pick
/// the wrong group of the same name and send to the wrong people. No merging is
/// performed — the caller must surface the conflict and ask for a different name.
/// </summary>
public sealed class DuplicateGroupNameException : Exception
{
    /// <summary>The conflicting group name (trimmed).</summary>
    public string? GroupName { get; }

    public DuplicateGroupNameException() { }

    public DuplicateGroupNameException(string groupName)
        : base($"A group named \"{groupName}\" already exists.")
        => GroupName = groupName;

    public DuplicateGroupNameException(string message, Exception innerException)
        : base(message, innerException) { }
}
