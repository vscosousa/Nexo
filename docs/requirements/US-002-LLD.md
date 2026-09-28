# US-002 - LLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [HLD](US-002-HLD.md)

**Status:** backend implemented; the caller is read from a temporary `X-Account-Id` header until JWT authentication (US-005) exists; frontend not implemented. See [design review gaps](README.md#design-review-gaps).

Full technical detail, building on the [HLD](US-002-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-002/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain)); no new entity. This feature creates an `Account` row with `Status = Invited`, `Role = Member`, `Email` set, and `Name`/`PasswordHash` left null. It also stores `InvitationTokenHash`, the SHA-256 of a random one-time token (`InvitationTokens.Create`); the plain token is emailed to the invited person (`InvitationEmail`, sent through `IEmailSender`, see [ADR-008](../decisions/ADR-008-fake-smtp-server.md)) and is what [US-003](US-003-LLD.md) requires, together with the email, to activate the account. It is never returned by the API.

## Service logic (`AccountInvitationService.Invite`)

1. Validate `InviteMemberDto`: `Email` non-empty, a valid email format, and at most 320 characters. Fail with a validation error (→ 400) otherwise.
2. Call `IAccountRepository.GetByIdAsync(callerAccountId)`; a missing or unknown caller id is also an authorization error. If the caller's `Role` is not `Admin` or their `OrganizationId` does not match the route's `organizationId`, fail with an authorization error (→ 403).
3. Call `IOrganizationRepository.GetByIdAsync(organizationId)` for `MemberLimit`, and `IAccountRepository.CountByOrganizationAndStatusAsync(organizationId, Status.Active)` for the current **active** account count. If the count has reached `MemberLimit`, fail with a conflict error (→ 409); do not proceed. `Invited` accounts do not count toward this limit.
4. Call `IAccountRepository.FindByEmailAsync(dto.Email)`. If a match is found (in any `Status`), fail with a conflict error (→ 409); do not proceed.
5. Create the invitation token (`InvitationTokens.Create`) and map the DTO, `organizationId`, and the token hash to a new `Account` (`AccountMapper.ToInvitedAccount`), with `Role = Member` and `Status = Invited`.
6. Add via `IAccountRepository.Add`.
7. Call `NexoDbContext.SaveChangesAsync()`.
8. Build the invitation email (`InvitationEmail.Create`: organization name, inviter's name, activation link, and the token) and send it with `IEmailSender.SendAsync`. If sending fails, remove the just-saved account (so the address can be invited again) and let the failure surface (→ 500).
9. Map the persisted `Account` to `AccountDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid email | Step 1 (service validation) | 400, field-level errors |
| Caller is not the organization's admin | Step 2 (authorization check) | 403, no write attempted |
| Organization's active-member limit reached | Step 3 (repository count, `Active` only) | 409, no write attempted |
| Email already has an account (`Invited` or `Active`) | Step 4 (repository lookup) | 409, no write attempted |
| Same email invited concurrently (unique-index violation on save) | Step 7 | 409, same as the step 4 conflict; no partial state persisted |
| Other database failure on save | Step 7 | 500; no partial state persisted |
| Invitation email cannot be sent | Step 8 | 500; the pending account is removed again so the invitation can be retried |

## Related artifacts

[HLD](US-002-HLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [backend tests](../testing/README.md) (`AccountInvitationsEndpointTests`).
