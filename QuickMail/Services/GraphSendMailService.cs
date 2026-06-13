using System;
using System.Buffers.Text;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using QuickMail.Models;
using QuickMail.Services.Graph;

namespace QuickMail.Services;

/// <summary>
/// Send path for Microsoft Graph accounts. Posts a MIME message to <c>/me/sendMail</c>
/// (base64-encoded, <c>Content-Type: text/plain</c>); Graph delivers it and auto-saves a copy
/// to the Sent folder, so <see cref="IMailService.AppendToSentAsync"/> is a no-op for Graph.
/// </summary>
public class GraphSendMailService : ISendMailService, IDisposable
{
    private readonly GraphClient _client;

    /// <param name="http">Optional injected HttpClient for tests; null uses a real one.</param>
    public GraphSendMailService(IOAuthService oauth, HttpClient? http = null) => _client = new GraphClient(oauth, http);

    public Task SendAsync(ComposeModel compose, AccountModel account, string? password, CancellationToken ct = default)
        => SendMimeAsync(account, MimeMessageBuilder.Build(compose, account, MimeMessageBuilder.AppUserAgent), ct);

    public Task SendIcsReplyAsync(string icsReplyContent, AccountModel account, string? password,
        string organizerEmail, CancellationToken ct = default)
        => SendMimeAsync(account, MimeMessageBuilder.BuildIcsReply(account, icsReplyContent, organizerEmail), ct);

    private async Task SendMimeAsync(AccountModel account, MimeMessage message, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await message.WriteToAsync(ms, ct);
        int mimeLength = (int)ms.Length;

        // Graph /sendMail takes the MIME message base64-encoded as a text/plain body. Encode the
        // MIME bytes straight to base64 ASCII bytes — no intermediate base64 string allocation.
        var body = new byte[Base64.GetMaxEncodedToUtf8Length(mimeLength)];
        Base64.EncodeToUtf8(ms.GetBuffer().AsSpan(0, mimeLength), body, out _, out _);

        LogService.Log($"GraphSendMailService: sending {mimeLength} MIME bytes via /me/sendMail");
        await _client.PostRawAsync(account, "/me/sendMail", body, "text/plain", ct);
        LogService.Log("GraphSendMailService: send complete");
    }

    public void Dispose() => _client.Dispose();
}
