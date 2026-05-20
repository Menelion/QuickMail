using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using QuickMail.Models;

namespace QuickMail.Services;

public class SmtpService : ISmtpService
{
    private readonly IOAuthService _oauth;
    private static readonly string UserAgent =
        "QuickMail/" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0");

    public SmtpService(IOAuthService oauth) => _oauth = oauth;

    public async Task SendAsync(ComposeModel compose, AccountModel account, string? password, CancellationToken ct = default)
    {
        var message = MimeMessageBuilder.Build(compose, account, UserAgent);

        using var client = new SmtpClient();

        if (account.SmtpAcceptInvalidCert)
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;

        var ssl = account.SmtpUseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        try
        {
            LogService.Log($"SmtpService: connecting to {account.SmtpHost}:{account.SmtpPort} ssl={ssl}");
            await client.ConnectAsync(account.SmtpHost, account.SmtpPort, ssl, ct);
            LogService.Log($"SmtpService: connected to {account.SmtpHost}:{account.SmtpPort}");

            if (account.AuthType == AuthType.OAuth2Microsoft)
            {
                LogService.Debug($"SmtpService: authenticating via XOAUTH2");
                var token = await _oauth.GetAccessTokenAsync(account, ct);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(account.Username, token), ct);
            }
            else
            {
                await client.AuthenticateAsync(account.Username, password!, ct);
            }
            LogService.Log($"SmtpService: authenticated, sending.");
            await client.SendAsync(message, ct);
            LogService.Log($"SmtpService: send complete");
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            LogService.Log($"SmtpService: send failed ({ex.GetType().Name})", ex);
            throw;
        }
    }
}
