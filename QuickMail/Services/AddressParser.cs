using MimeKit;

namespace QuickMail.Services;

/// <summary>
/// Shared address-parsing logic used by both SMTP and IMAP (drafts).
/// </summary>
public static class AddressParser
{
    /// <summary>
    /// Splits <paramref name="addressString"/> on commas and semicolons,
    /// parses each part as a <see cref="MailboxAddress"/>, and adds valid
    /// results to <paramref name="list"/>.
    /// </summary>
    public static void AddAddresses(InternetAddressList list, string addressString)
    {
        if (string.IsNullOrWhiteSpace(addressString)) return;
        foreach (var part in addressString.Split(',', ';'))
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed) && MailboxAddress.TryParse(trimmed, out var addr))
                list.Add(addr);
        }
    }
}
