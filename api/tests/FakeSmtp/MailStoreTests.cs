using System.Net.Mail;
using Nexo.FakeSmtp;
using Xunit;

namespace Nexo.Api.Tests.FakeSmtp;

public class MailStoreTests
{
    [Fact]
    public async Task GivenStoredMessages_WhenTheServerRestartsOnTheSameDirectory_ThenMessagesAndReadStateSurvive()
    {
        await using var server = new FakeSmtpHarness();
        await server.InitializeAsync();
        using (var client = new SmtpClient("127.0.0.1", server.SmtpPort))
            await client.SendMailAsync(new MailMessage("a@example.com", "b@example.com", "Persist me", "body"));
        var id = server.Store.List(null).Single().Id;
        server.Store.MarkRead(id, true);

        await server.StopAsync();
        var reloaded = new MailStore(server.Directory);

        var message = Assert.Single(reloaded.List(null));
        Assert.Equal(id, message.Id);
        Assert.Equal("Persist me", message.Subject);
        Assert.True(message.Read);
        await server.DisposeAsync();
    }

    [Fact]
    public void GivenFilesThatAreNotMessages_WhenLoaded_ThenTheyAreIgnored()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nexo-fake-smtp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "notes.eml"), "not a stored message name");
            File.WriteAllText(Path.Combine(directory, "readme.txt"), "hello");

            Assert.Empty(new MailStore(directory).List(null));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
