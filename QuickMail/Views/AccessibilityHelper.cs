using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation;

namespace QuickMail.Views;

/// <summary>
/// Raises UIA Notification events so screen readers (NVDA, JAWS, Narrator) hear
/// programmatic announcements in this native WPF app.
///
/// WPF's AutomationProperties.LiveSetting fires UIA LiveRegionChanged, which
/// screen readers support only inside web browsers (HTML aria-live). For desktop
/// apps, RaiseNotificationEvent (UIA 1.1, Windows 10 1703+) is the correct API.
/// </summary>
internal static class AccessibilityHelper
{
    private const string ActivityId = "QuickMailAnnouncement";

    /// <summary>
    /// Announces <paramref name="text"/> to the active screen reader.
    /// </summary>
    /// <param name="element">Any realized UIElement in the window (typically the window itself).</param>
    /// <param name="text">The string for the screen reader to speak.</param>
    /// <param name="interrupt">
    /// <see langword="true"/> to interrupt the current utterance (ImportantMostRecent);
    /// <see langword="false"/> to queue and replace any pending same-ID announcement (MostRecent).
    /// </param>
    public static void Announce(UIElement element, string text, bool interrupt = false)
    {
        if (string.IsNullOrEmpty(text)) return;

        var peer = UIElementAutomationPeer.FromElement(element)
                   ?? UIElementAutomationPeer.CreatePeerForElement(element);
        if (peer == null) return;

        var processing = interrupt
            ? AutomationNotificationProcessing.ImportantMostRecent
            : AutomationNotificationProcessing.MostRecent;

        peer.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            processing,
            text,
            ActivityId);
    }
}
