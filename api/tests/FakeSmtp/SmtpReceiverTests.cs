using System.Net.Mail;
using Xunit;

namespace Nexo.Api.Tests.FakeSmtp;

public class SmtpReceiverTests : IClassFixture<FakeSmtpHarness>
{
    private readonly FakeSmtpHarness _server;

    public SmtpReceiverTests(FakeSmtpHarness server) => _server = server;

    [Fact]
    public async Task GivenAStandardClient_WhenItSendsAMessage_ThenTheInboxHoldsIt()
    {
        using var client = new SmtpClient("127.0.0.1", _server.SmtpPort);
        using var message = new MailMessage("Nexo <no-reply@nexo.local>", "bob@example.com", "Hello Bob", "Plain body");

        await client.SendMailAsync(message);

        var stored = Assert.Single(_server.Store.List("Hello Bob"));
        Assert.Equal("Hello Bob", stored.Subject);
        Assert.Equal("no-reply@nexo.local", stored.FromAddress);
        Assert.Equal("Nexo", stored.FromName);
        Assert.Contains("bob@example.com", stored.To);
        Assert.False(stored.Read);
    }

    [Fact]
    public async Task GivenADataLineStartingWithADot_WhenTheMessageIsStored_ThenTheDotIsUnstuffed()
    {
        var replies = await _server.ConverseAsync(
            "EHLO test",
            "MAIL FROM:<a@example.com>",
            "RCPT TO:<b@example.com>",
            "DATA",
            "Subject: Dots",
            "From: a@example.com",
            "To: b@example.com",
            "",
            "..hidden line",
            ".",
            "QUIT");

        Assert.Contains(replies, r => r.StartsWith("354"));
        Assert.Contains(replies, r => r.StartsWith("250 OK: queued as"));
        Assert.Contains(replies, r => r.StartsWith("221"));
        var stored = _server.Store.List("Dots").Single();
        Assert.Contains(".hidden line", _server.Store.Get(stored.Id)!.Text);
        Assert.DoesNotContain("..hidden line", _server.Store.Get(stored.Id)!.Text);
    }

    [Fact]
    public async Task GivenNoRecipient_WhenDataIsRequested_ThenItRepliesBadSequence()
    {
        var replies = await _server.ConverseAsync("HELO test", "MAIL FROM:<a@example.com>", "DATA", "QUIT");

        Assert.Contains(replies, r => r.StartsWith("503"));
    }

    [Fact]
    public async Task GivenAnUnsupportedCommand_WhenSent_ThenItRepliesNotImplemented()
    {
        var replies = await _server.ConverseAsync("EHLO test", "STARTTLS", "RSET", "NOOP", "QUIT");

        Assert.Contains(replies, r => r.StartsWith("502"));
        Assert.Equal(2, replies.Count(r => r.StartsWith("250 OK")));
    }
}
