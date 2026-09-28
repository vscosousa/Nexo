using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Mappers;
using Xunit;

namespace Nexo.Api.Tests.Mappers;

public class AccountMapperTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void GivenAnEmailAndOrganization_WhenMappedToInvitedAccount_ThenItIsAPendingMemberWithoutAName()
    {
        var account = AccountMapper.ToInvitedAccount(
            new InviteMemberDto { Email = "  Bob@Example.COM " }, OrganizationId, "token-hash");

        Assert.Equal("bob@example.com", account.Email);
        Assert.Null(account.Name);
        Assert.Null(account.PasswordHash);
        Assert.Equal(Role.Member, account.Role);
        Assert.Equal(AccountStatus.Invited, account.Status);
        Assert.Equal(OrganizationId, account.OrganizationId);
        Assert.Equal("token-hash", account.InvitationTokenHash);
    }

    [Fact]
    public void GivenAnInvitedAccount_WhenActivationIsApplied_ThenItIsActiveWithANameAndAHashedPasswordAndNoToken()
    {
        var account = new Account
        {
            Email = "bob@example.com",
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = OrganizationId,
            InvitationTokenHash = "token-hash",
        };
        var hasher = new PasswordHasher<Account>();

        AccountMapper.ApplyActivation(
            account, new ActivateAccountDto { Name = " Bob ", Password = "s3cret-pass" }, hasher);

        Assert.Equal("Bob", account.Name);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(Role.Member, account.Role);
        Assert.Equal(OrganizationId, account.OrganizationId);
        Assert.Null(account.InvitationTokenHash);
        Assert.NotEqual("s3cret-pass", account.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(account, account.PasswordHash!, "s3cret-pass"));
    }

    [Fact]
    public void GivenAnAccount_WhenMappedToDto_ThenItCarriesAllPublicFields()
    {
        var account = new Account
        {
            Email = "bob@example.com",
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = OrganizationId,
        };

        var dto = AccountMapper.ToDto(account);

        Assert.Equal(account.Id, dto.Id);
        Assert.Equal("bob@example.com", dto.Email);
        Assert.Null(dto.Name);
        Assert.Equal(Role.Member, dto.Role);
        Assert.Equal(AccountStatus.Invited, dto.Status);
        Assert.Equal(OrganizationId, dto.OrganizationId);
    }
}
