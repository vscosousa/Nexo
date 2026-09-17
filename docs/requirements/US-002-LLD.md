# US-002 - LLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [HLD](US-002-HLD.md)

**Status:** proposed; not implemented. See [design review gaps](README.md#design-review-gaps) before implementing this contract.

Full technical detail, building on the [HLD](US-002-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-002/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain)); no new entity. This feature creates an `Account` row with `Status = Invited`, `Role = Member`, `Email` set, and `Name`/`PasswordHash` left null.

## Service logic (`AccountInvitationService.Invite`)

1. Validate `InviteMemberDto`: `Email` non-empty and a valid email format. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.GetByIdAsync(callerAccountId)`. If the caller's `Role` is not `Admin` or their `OrganizationId` does not match the route's `organizationId`, fail with an authorization error (→ 403).
3. Call `IOrganizationRepository.GetByIdAsync(organizationId)` for `MemberLimit`, and `IAccountRepository.CountByOrganizationAndStatusAsync(organizationId, Status.Active)` for the current **active** account count. If the count has reached `MemberLimit`, fail with a conflict error (→ 409); do not proceed. `Invited` accounts do not count toward this limit.
4. Call `IAccountRepository.FindByEmailAsync(dto.Email)`. If a match is found (in any `Status`), fail with a conflict error (→ 409); do not proceed.
5. Map the DTO and `organizationId` to a new `Account` (`AccountMapper.ToInvitedAccount`), with `Role = Member` and `Status = Invited`.
6. Add via `IAccountRepository.Add`.
7. Call `NexoDbContext.SaveChangesAsync()`.
8. Map the persisted `Account` to `AccountDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid email | Step 1 (service validation) | 400, field-level errors |
| Caller is not the organization's admin | Step 2 (authorization check) | 403, no write attempted |
| Organization's active-member limit reached | Step 3 (repository count, `Active` only) | 409, no write attempted |
| Email already has an account (`Invited` or `Active`) | Step 4 (repository lookup) | 409, no write attempted |
| Database failure on save | Step 7 | 500; no partial state persisted |

## Related artifacts

[HLD](US-002-HLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), tests (not yet created, required before implementation per [AGENTS.md](../../AGENTS.md#working-rules)).
