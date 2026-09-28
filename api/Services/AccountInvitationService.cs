using System.ComponentModel.DataAnnotations;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class AccountInvitationService(
    IAccountRepository accounts,
    IOrganizationRepository organizations,
    NexoDbContext db) : IAccountInvitationService
{
    private const string EmailInUse = "An account already exists for this email.";

    public async Task<AccountDto> Invite(Guid organizationId, Guid? callerAccountId, InviteMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
            throw new Domain.Exceptions.ValidationException(
                new Dictionary<string, string[]> { [nameof(dto.Email)] = ["A valid email is required."] });

        var caller = callerAccountId is { } id ? await accounts.GetByIdAsync(id) : null;
        if (caller is not { Role: Role.Admin } || caller.OrganizationId != organizationId)
            throw new ForbiddenException("Only an admin of the organization can invite members.");

        var organization = await organizations.GetByIdAsync(organizationId)
            ?? throw new InvalidOperationException("The caller's organization does not exist.");
        var active = await accounts.CountByOrganizationAndStatusAsync(organizationId, AccountStatus.Active);
        if (active >= organization.Plan!.MemberLimit)
            throw new ConflictException("The organization's member limit has been reached.");

        if (await accounts.FindByEmailAsync(Account.NormalizeEmail(dto.Email)) is not null)
            throw new ConflictException(EmailInUse);

        var account = AccountMapper.ToInvitedAccount(dto, organizationId);
        accounts.Add(account);
        // A concurrent invitation for the same email may have passed the lookup and saved first.
        await db.SaveChangesOrConflictAsync(EmailInUse);

        return AccountMapper.ToDto(account);
    }
}
