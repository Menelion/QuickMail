namespace QuickMail.Models;

/// <summary>
/// Feature-gate keys. Adding a new gate is one enum value here plus an entry
/// in ConfigFeatureGate.Defaults.
/// </summary>
public enum FeatureFlag
{
    /// <summary>
    /// Enables Microsoft Graph as a mail-backend option in the Add Account dialog.
    /// Default: false. Flip the default to true via a future joint-decision PR.
    /// </summary>
    GraphBackend,

    /// <summary>
    /// Shows the Google OAuth (Gmail) option in Add Account and Account Manager dialogs.
    /// Default: false. Enable in config.ini as GoogleAuth=true under [features].
    /// Requires GoogleClientId and GoogleClientSecret in the [google] section of config.ini.
    /// </summary>
    GoogleAuth,
}
