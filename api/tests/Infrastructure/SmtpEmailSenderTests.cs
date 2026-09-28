using Microsoft.Extensions.Options;
using Nexo.Api.Infrastructure.Email;
using Nexo.Api.Tests.FakeSmtp;
using Xunit;

namespace Nexo.Api.Tests.Infrastructure;

public class SmtpEmailSenderTests : IClassFixture<FakeSmtpHarness>
{
    private readonly FakeSmtpHarness _server;

    public SmtpEmailSenderTests(FakeSmtpHarness server) => _server = server;

    [Fact]
    public async Task GivenAMessage_WhenSentOverSmtp_ThenTheFakeServerStoresItWithBothBodies()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailOptions
        {
            Host = "127.0.0.1",
            Port = _server.SmtpPort,
            From = "Nexo <no-reply@nexo.local>",
        }));

        await sender.SendAsync(new EmailMessage("bob@example.com", "You're invited", "Plain invite", "<p>Rich <b>invite</b></p>"));

        var summary = Assert.Single(_server.Store.List("You're invited"));
        Assert.Equal("Nexo", summary.FromName);
        Assert.Equal("no-reply@nexo.local", summary.FromAddress);
        Assert.Equal(["bob@example.com"], summary.To);
        var detail = _server.Store.Get(summary.Id)!;
        Assert.Contains("Plain invite", detail.Text);
        Assert.Contains("<b>invite</b>", detail.Html);
    }

    [Fact]
    public async Task GivenNoServerListening_WhenSending_ThenItThrows()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailOptions { Host = "127.0.0.1", Port = 1, TimeoutSeconds = 2 }));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendAsync(new EmailMessage("bob@example.com", "s", "t", "<p>h</p>")));
    }
}
