using Nexo.Api.Services;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Sh0rt!a", PasswordStrength.Weak)]
    [InlineData("abcdefgh", PasswordStrength.Weak)]
    [InlineData("ALLUPPERCASE", PasswordStrength.Weak)]
    [InlineData("abcdefghijklmno", PasswordStrength.Weak)]
    [InlineData("Abcdefg1", PasswordStrength.Reasonable)]
    [InlineData("abcdef1!", PasswordStrength.Reasonable)]
    [InlineData("Abcdefg1!", PasswordStrength.Reasonable)]
    [InlineData("NoSymbols1234Ab", PasswordStrength.Reasonable)]
    [InlineData("abcdefghijklmnop", PasswordStrength.Reasonable)]
    [InlineData("abcdefghijklmnopqrs", PasswordStrength.Reasonable)]
    [InlineData("Abcdefgh1!xy", PasswordStrength.Strong)]
    [InlineData("Str0ng!Passw0rd", PasswordStrength.Strong)]
    [InlineData("NoSymbols1234Abcd", PasswordStrength.Strong)]
    [InlineData("correct horse battery", PasswordStrength.Strong)]
    [InlineData("Str0ng!Passw0rd!!", PasswordStrength.VeryStrong)]
    [InlineData("NoSymbols1234Abcdefgh", PasswordStrength.VeryStrong)]
    [InlineData("correct horse battery staple", PasswordStrength.VeryStrong)]
    public void GivenAPassword_WhenMeasured_ThenItGetsTheExpectedStrength(string password, PasswordStrength expected) =>
        Assert.Equal(expected, PasswordPolicy.Measure(password));

    [Fact]
    public void GivenALongPasswordContainingAName_WhenMeasured_ThenItIsWeak() =>
        Assert.Equal(PasswordStrength.Weak, PasswordPolicy.Measure("Xx!AnaAdmin9-long-enough", "Ana Admin"));

    [Theory]
    [InlineData("Str0ng!Passw0rd")]
    [InlineData("Abcdefg1")]
    [InlineData("abcdefghijklmnop")]
    public void GivenAReasonableOrStrongPassword_WhenChecked_ThenThereAreNoErrors(string password) =>
        Assert.Empty(PasswordPolicy.Check(password, "Local Club", "Ana Admin"));

    [Theory]
    [InlineData("Sh0rt!a")]
    [InlineData("abcdefgh")]
    [InlineData("ALLUPPERCASE")]
    [InlineData("abcdefghijklmno")]
    public void GivenAWeakPassword_WhenChecked_ThenItIsRejected(string password) =>
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
