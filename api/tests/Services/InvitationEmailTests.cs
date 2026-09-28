using Nexo.Api.Services;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class InvitationEmailTests
{
    [Fact]
    public void GivenAnInvitation_WhenTheEmailIsCreated_ThenItNamesTheOrganizationAndCarriesTheActivationLink()
    {
        var email = InvitationEmail.Create("bob@example.com", "Local Club", "Ana Admin", "http://localhost:5173/", "tok_en-1");

        Assert.Equal("bob@example.com", email.To);
        Assert.Contains("Local Club", email.Subject);
        Assert.Contains("Ana Admin", email.Text);
        const string link = "http://localhost:5173/activate?email=bob%40example.com&token=tok_en-1";
        Assert.Contains(link, email.Text);
        Assert.Contains($"href=\"{link.Replace("&", "&amp;")}\"", email.Html);
        Assert.Contains("tok_en-1", email.Text);
    }

    [Fact]
    public void GivenNamesWithMarkup_WhenTheEmailIsCreated_ThenTheHtmlIsEncoded()
    {
        var email = InvitationEmail.Create("bob@example.com", "<b>Evil</b> Club", "<i>Ana</i>", "http://localhost:5173", "t");

        Assert.DoesNotContain("<b>Evil</b>", email.Html);
        Assert.Contains("&lt;b&gt;Evil&lt;/b&gt; Club", email.Html);
        Assert.DoesNotContain("<i>Ana</i>", email.Html);
    }
}
