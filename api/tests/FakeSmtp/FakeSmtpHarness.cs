using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nexo.FakeSmtp;
using Xunit;

namespace Nexo.Api.Tests.FakeSmtp;

/// <summary>Runs the fake mail server on random loopback ports with a throwaway store directory.</summary>
public sealed class FakeSmtpHarness : IAsyncLifetime
{
    private WebApplication? _app;

    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "nexo-fake-smtp-" + Guid.NewGuid().ToString("N"));

    public int SmtpPort => _app!.Services.GetRequiredService<SmtpReceiver>().Port;

    public HttpClient Http { get; private set; } = null!;

    public MailStore Store => _app!.Services.GetRequiredService<MailStore>();

    public async Task InitializeAsync()
    {
        await StartAsync();
    }

    public async Task StartAsync()
    {
        _app = FakeSmtpApp.Build(
            ["--Smtp:Port=0", "--urls=http://127.0.0.1:0", $"--Store:Directory={Directory}", "--environment=Development"]);
        await _app.StartAsync();
        Http = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task StopAsync()
    {
        Http.Dispose();
        await _app!.StopAsync();
        await _app.DisposeAsync();
    }

    public async Task DisposeAsync()
    {
        await StopAsync();
        if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, recursive: true);
    }

    /// <summary>Plays the given client lines against the SMTP port and returns every server line received.</summary>
    public async Task<List<string>> ConverseAsync(params string[] clientLines)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, SmtpPort);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.Latin1);
        await using var writer = new StreamWriter(stream, Encoding.Latin1, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };

        var received = new List<string> { (await reader.ReadLineAsync())! };
        var inData = false;
        foreach (var line in clientLines)
        {
            await writer.WriteLineAsync(line);
            if (inData && line != ".") continue;
            received.AddRange(await ReadReplyAsync(reader));
            inData = !inData && line == "DATA" && received[^1].StartsWith("354");
        }
        return received;
    }

    private static async Task<List<string>> ReadReplyAsync(StreamReader reader)
    {
        var lines = new List<string>();
        while (await reader.ReadLineAsync() is { } line)
        {
            lines.Add(line);
            if (line.Length < 4 || line[3] == ' ') break;
        }
        return lines;
    }
}
