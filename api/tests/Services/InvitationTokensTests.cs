using Nexo.Api.Services;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class InvitationTokensTests
{
    [Fact]
    public void GivenANewToken_WhenMatchedAgainstItsHash_ThenItMatchesAndOthersDoNot()
    {
        var (token, hash) = InvitationTokens.Create();

        Assert.NotEqual(token, hash);
        Assert.True(InvitationTokens.Matches(token, hash));
        Assert.False(InvitationTokens.Matches(token + "x", hash));
        Assert.False(InvitationTokens.Matches(token, null));
    }

    [Fact]
    public void GivenTwoTokens_WhenCreated_ThenTheyDiffer() =>
        Assert.NotEqual(InvitationTokens.Create().Token, InvitationTokens.Create().Token);
}
