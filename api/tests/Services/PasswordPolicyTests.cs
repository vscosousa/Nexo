using Nexo.Api.Services;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class PasswordPolicyTests
{
    [Fact]
    public void GivenAStrongPassword_WhenChecked_ThenThereAreNoErrors() =>
        Assert.Empty(PasswordPolicy.Check("Str0ng!Passw0rd", "Local Club", "Ana Admin"));

    [Theory]
    [InlineData("Sh0rt!a")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!!")]
    [InlineData("NoSymbols1234Ab")]
    public void GivenAPasswordMissingARequiredRule_WhenChecked_ThenItIsRejected(string password) =>
        Assert.NotEmpty(PasswordPolicy.Check(password));

    [Fact]
    public void GivenAPasswordOverTheMaximumLength_WhenChecked_ThenItIsRejected() =>
        Assert.NotEmpty(PasswordPolicy.Check("Aa1!" + new string('x', PasswordPolicy.MaxLength)));

    [Theory]
    [InlineData("Local Club", "Xx!LocalClub9")]
    [InlineData("Local Club", "xx!LOCAL club9")]
    [InlineData("Local Club", "Xx!L0c@lCl_ub9")]
    [InlineData("Ana Admin", "Xx!Adm1n-9zq")]
    [InlineData("Ana Admin", "Xx!ana9zqvw")]
    [InlineData("Ana Admin", "Xx!AnaAdmin9")]
    public void GivenAPasswordContainingAForbiddenTermOrAVariation_WhenChecked_ThenItIsRejected(
        string forbidden, string password) =>
        Assert.NotEmpty(PasswordPolicy.Check(password, forbidden));

    [Fact]
    public void GivenShortOrMissingForbiddenTerms_WhenChecked_ThenTheyAreIgnored() =>
        Assert.Empty(PasswordPolicy.Check("Str0ng!Passw0rd", null, "  ", "Al", "Bo Li"));
}
