using Nexo.Api.Services;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class InvitationTokensTests
{
    [Fact]
    public void GivenANewLinkToken_WhenMatchedAgainstItsHash_ThenItMatchesAndOthersDoNot()
    {
        var (token, hash) = InvitationTokens.CreateLinkToken();

        Assert.NotEqual(token, hash);
        Assert.True(InvitationTokens.TokenMatches(token, hash));
        Assert.False(InvitationTokens.TokenMatches(token + "x", hash));
        Assert.False(InvitationTokens.TokenMatches(token, null));
    }

    [Fact]
    public void GivenTwoLinkTokens_WhenCreated_ThenTheyDiffer() =>
        Assert.NotEqual(InvitationTokens.CreateLinkToken().Token, InvitationTokens.CreateLinkToken().Token);

    [Fact]
    public void GivenALinkToken_WhenMatchedInADifferentCase_ThenItDoesNotMatch()
    {
        var (token, hash) = InvitationTokens.CreateLinkToken();

        Assert.False(InvitationTokens.TokenMatches(token.ToLowerInvariant(), hash));
    }

    [Fact]
    public void GivenANewCode_WhenMatchedAgainstItsHash_ThenItMatchesAndOthersDoNot()
    {
        var (code, hash) = InvitationTokens.CreateCode();

        Assert.NotEqual(code, hash);
        Assert.True(InvitationTokens.CodeMatches(code, hash));
        Assert.False(InvitationTokens.CodeMatches(code + "x", hash));
        Assert.False(InvitationTokens.CodeMatches(code, null));
    }

    [Fact]
    public void GivenTwoCodes_WhenCreated_ThenTheyDiffer() =>
        Assert.NotEqual(InvitationTokens.CreateCode().Code, InvitationTokens.CreateCode().Code);

    [Fact]
    public void GivenANewCode_WhenCreated_ThenItIsSixCharactersLong() =>
        Assert.Equal(6, InvitationTokens.CreateCode().Code.Length);

    [Fact]
    public void GivenACode_WhenMatchedInADifferentCase_ThenItStillMatches()
    {
        var (code, hash) = InvitationTokens.CreateCode();

        Assert.True(InvitationTokens.CodeMatches(code.ToLowerInvariant(), hash));
    }
}
