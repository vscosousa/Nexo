using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Nexo.FakeSmtp;

/// <summary>
/// A deliberately small SMTP server: it accepts any sender, recipient and credentials-free session and
/// hands each message to <see cref="MailStore"/>. It never relays anything. No TLS or AUTH is offered.
/// </summary>
public sealed partial class SmtpReceiver(MailStore store, IConfiguration configuration, ILogger<SmtpReceiver> logger)
    : BackgroundService
{
    private const int MaxMessageBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(2);
    private static readonly byte[] LineEnd = "\r\n"u8.ToArray();

    private TcpListener? _listener;

    /// <summary>The port actually bound; differs from the configured one when that is 0.</summary>
    public int Port => ((IPEndPoint)_listener!.LocalEndpoint).Port;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var address = IPAddress.Parse(configuration["Smtp:Address"] ?? "127.0.0.1");
        _listener = new TcpListener(address, configuration.GetValue("Smtp:Port", 1025));
        _listener.Start();
        logger.LogInformation("Fake SMTP server listening on {Endpoint}", _listener.LocalEndpoint);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Stop();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(stoppingToken);
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }
            _ = Task.Run(() => HandleAsync(client, stoppingToken), stoppingToken);
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken stoppingToken)
    {
        try
        {
            using (client)
            {
                await using var stream = client.GetStream();
                // Latin-1 maps bytes to chars one to one, so message bytes round-trip unchanged.
                using var reader = new StreamReader(stream, Encoding.Latin1);
                await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
                await ConverseAsync(reader, writer, stoppingToken);
            }
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException)
        {
            // A dropped or timed-out client only ends its own session.
        }
    }

    private async Task ConverseAsync(StreamReader reader, StreamWriter writer, CancellationToken stoppingToken)
    {
        async Task<string?> ReadLine()
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(IdleTimeout);
            return await reader.ReadLineAsync(timeout.Token);
        }

        string? sender = null;
        var recipients = 0;
        await writer.WriteLineAsync("220 fake-smtp ready");
        while (await ReadLine() is { } line)
        {
            var verb = line.Split(' ', 2)[0].ToUpperInvariant();
            switch (verb)
            {
                case "EHLO":
                    (sender, recipients) = (null, 0);
                    await writer.WriteLineAsync("250-fake-smtp");
                    await writer.WriteLineAsync($"250-SIZE {MaxMessageBytes}");
                    await writer.WriteLineAsync("250 8BITMIME");
                    break;
                case "HELO":
                    (sender, recipients) = (null, 0);
                    await writer.WriteLineAsync("250 fake-smtp");
                    break;
                case "MAIL":
                    if (!FromPattern().IsMatch(line)) { await writer.WriteLineAsync("501 Syntax: MAIL FROM:<address>"); break; }
                    (sender, recipients) = (FromPattern().Match(line).Groups[1].Value, 0);
                    await writer.WriteLineAsync("250 OK");
                    break;
                case "RCPT":
                    if (sender is null) { await writer.WriteLineAsync("503 Send MAIL first"); break; }
                    if (!ToPattern().IsMatch(line)) { await writer.WriteLineAsync("501 Syntax: RCPT TO:<address>"); break; }
                    recipients++;
                    await writer.WriteLineAsync("250 OK");
                    break;
                case "DATA":
                    if (sender is null || recipients == 0) { await writer.WriteLineAsync("503 Send MAIL and RCPT first"); break; }
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    await ReceiveDataAsync(ReadLine, writer);
                    (sender, recipients) = (null, 0);
                    break;
                case "RSET":
                    (sender, recipients) = (null, 0);
                    await writer.WriteLineAsync("250 OK");
                    break;
                case "NOOP":
                    await writer.WriteLineAsync("250 OK");
                    break;
                case "VRFY":
                    await writer.WriteLineAsync("252 Cannot verify, will accept");
                    break;
                case "QUIT":
                    await writer.WriteLineAsync("221 Bye");
                    return;
                default:
                    await writer.WriteLineAsync("502 Command not implemented");
                    break;
            }
        }
    }

    private async Task ReceiveDataAsync(Func<Task<string?>> readLine, StreamWriter writer)
    {
        using var buffer = new MemoryStream();
        var tooBig = false;
        while (await readLine() is { } line)
        {
            if (line == ".")
            {
                if (tooBig) { await writer.WriteLineAsync("552 Message too large"); return; }
                try
                {
                    var id = store.Add(buffer.ToArray());
                    logger.LogInformation("Stored message {Id}", id);
                    await writer.WriteLineAsync($"250 OK: queued as {id}");
                }
                catch (FormatException)
                {
                    await writer.WriteLineAsync("554 Message could not be parsed");
                }
                return;
            }
            if (buffer.Length > MaxMessageBytes) { tooBig = true; continue; }
            // A leading dot was doubled by the client (dot-stuffing).
            buffer.Write(Encoding.Latin1.GetBytes(line.StartsWith("..") ? line[1..] : line));
            buffer.Write(LineEnd);
        }
    }

    [GeneratedRegex(@"^MAIL FROM:\s*<([^>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex FromPattern();

    [GeneratedRegex(@"^RCPT TO:\s*<([^>]+)>", RegexOptions.IgnoreCase)]
    private static partial Regex ToPattern();
}
