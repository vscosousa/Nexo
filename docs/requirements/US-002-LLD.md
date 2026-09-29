# US-002 - LLD - Register a member's email to an organization

[Requirements](README.md) · [US-002](US-002-register-member-email.md) · [HLD](US-002-HLD.md)

**Status:** backend implemented; the caller is the account in the bearer token's `sub` claim ([ADR-006](../decisions/ADR-006-authentication.md)); frontend not implemented. See [design review gaps](README.md#design-review-gaps).

Full technical detail, building on the [HLD](US-002-HLD.md)'s contract and structure. See the [level 3 sequence diagram](../us/US-002/README.md#level-3---backend) for the call sequence.

## Domain

Reuses the existing `Account` (per [US-001-LLD](US-001-LLD.md#domain)); no new entity. This feature creates an `Account` row with `Status = Invited`, `Role` from the request (`Member` or `Staff`), `Email` set, and `FirstName`/`LastName`/`PasswordHash` left null. The invitation has two independent secrets, stored as separate SHA-256 hashes: `InvitationTokenHash`, the long opaque link token (`InvitationTokens.CreateLinkToken`) embedded in the activation link's URL, and `InvitationCodeHash`, the short 6-character code (`InvitationTokens.CreateCode`, from an alphabet without ambiguous characters) shown only in the email's body text. Both share `InvitationExpiresAt` (now + `InvitationTokens.Lifetime`, 7 days). The plain link token and code are emailed to the invited person (`InvitationEmail`, sent through `IEmailSender`, see [ADR-008](../decisions/ADR-008-fake-smtp-server.md)); [US-003](US-003-LLD.md) requires both of them, together with the email, to activate the account before it expires. The link token alone (which can leak through browser history or a forwarded link) is not enough. Neither is ever returned by the API. Inviting an email still pending in the same organization overwrites both hashes and the expiry instead of conflicting, so re-sending the invite is just inviting again.

## Service logic (`AccountInvitationService.Invite`)

1. Validate `InviteMemberDto`: `Email` non-empty, a valid email format, and at most 320 characters; `Role` present and `Member` or `Staff`. Fail with a validation error (→ 400) otherwise.
2. `AccountInvitationsController` requires a valid bearer token (`[Authorize]`, → 401 otherwise) and reads the caller's account id from its `sub` claim. Call `IAccountRepository.GetByIdAsync(callerAccountId)`; a missing or unknown caller id is also an authorization error. If the caller's `Role` is not `Admin` or their `OrganizationId` does not match the route's `organizationId`, fail with an authorization error (→ 403).
3. Call `IOrganizationRepository.GetByIdAsync(organizationId)` for `MemberLimit`, and `IAccountRepository.FindByEmailAsync(dto.Email)`: a match that is `Invited` in the *same* organization (`organizationId`) makes this a re-invite (step 5 mutates it in place instead of inserting). Count the organization's `Active` and `Invited` accounts (`CountByOrganizationAndStatusAsync`). If the active count has reached `MemberLimit`, or this is not a re-invite and active plus pending has reached it, fail with a conflict error (→ 409); do not proceed. Counting pending invitations stops an admin from sending unlimited invitation emails ([ADR-010](../decisions/ADR-010-account-security-hardening.md)).
4. If the match from step 3 is `Active`, `Unverified`, or `Invited` in a *different* organization, fail with a conflict error (→ 409); do not proceed.
5. Create a fresh link token and code (`InvitationTokens.CreateLinkToken`, `InvitationTokens.CreateCode`) and an expiry (now + `InvitationTokens.Lifetime`). For a re-invite (step 3), overwrite `InvitationTokenHash`, `InvitationCodeHash`, and `InvitationExpiresAt` and `Role`, and reset `InvitationFailedAttempts` on the existing tracked `Account`. Otherwise map the DTO, `organizationId`, and both hashes to a new `Account` (`AccountMapper.ToInvitedAccount`), with the requested `Role` and `Status = Invited`, and add it via `IAccountRepository.Add`.
6. Call `NexoDbContext.SaveChangesAsync()`.
7. Build the invitation email (`InvitationEmail.Create`: organization name, inviter's `FullName` or, if blank, their email, the activation link carrying the link token, and the code as separate body text) and send it with `IEmailSender.SendAsync`. If sending fails: for a new invitation, remove the just-saved account (so the address can be invited again) and let the failure surface (→ 500); for a re-invite, leave the account as is (the previous, still-valid link token and code are untouched by the failed send) and let the failure surface (→ 500). The admin can simply re-invite again.
8. Map the persisted `Account` to `AccountDto` and return it (→ 201).

## Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/invalid email, or missing/non-invitable role | Step 1 (service validation) | 400, field-level errors |
| Missing/invalid/expired bearer token | Step 2 (controller `[Authorize]`) | 401, no write attempted |
| Caller is not the organization's admin | Step 2 (authorization check) | 403, no write attempted |
| Organization's member limit reached (active, plus pending for a new email) | Step 3 (repository counts) | 409, no write attempted |
| Email already `Active` or `Unverified`, or `Invited` in another organization | Step 4 | 409, no write attempted |
| Email already `Invited` in this organization | Step 3 (repository lookup) | Not an error: re-invited in place (step 5), even at the member limit |
| Same email invited concurrently (unique-index violation on save) | Step 6 | 409, same as the step 4 conflict; no partial state persisted |
| Other database failure on save | Step 6 | 500; no partial state persisted |
| Invitation email cannot be sent | Step 7 | 500; a new invitation's pending account is removed so it can be retried, a re-invite's account is left as is |

## Service logic (`AccountInvitationService.Resend`)

Lets the invited person themselves request a fresh code, distinct from the admin's `Invite` re-invite above: no caller/authorization step (the endpoint is unauthenticated), and only the code is rotated. The link token and the expiry are left untouched (resending cannot keep an invitation alive past its 7 days), since the person likely already has the activation page open with the original link's token in its URL, and rotating it would strand that page.

1. If `email` is missing or blank, return without doing anything (→ 202, see below on why this is never an error).
2. Call `IAccountRepository.FindByEmailAsync` with the normalized email. If no account is found, it is not `Status = Invited`, or its invitation has expired, return without doing anything (the admin must invite the email again).
3. Call `IOrganizationRepository.GetByIdAsync(account.OrganizationId)`. If the organization is somehow missing, return without doing anything (defensive; should not happen for a persisted account).
4. Create a fresh code (`InvitationTokens.CreateCode`), set `InvitationCodeHash` to its hash and reset `InvitationFailedAttempts` to 0 (so a person who used up the wrong-code attempts can try again with the new code), leaving `InvitationTokenHash` and `InvitationExpiresAt` unchanged. Call `NexoDbContext.SaveChangesAsync()`.
5. Build the resend email (`InvitationEmail.CreateCodeReminder`: organization name and the new code only, no link) and send it with `IEmailSender.SendAsync`, best-effort: a delivery failure here is swallowed, since the code is already saved and the person can simply ask again.
6. Return (→ 202) in every case. Steps 1–3's early returns and a successful send all respond identically, so the endpoint never reveals whether the email has a pending invitation.

### Error handling

| Case | Detection point | Response |
| --- | --- | --- |
| Missing/blank email | Step 1 | 202, nothing sent, no info revealed |
| Email not registered, or not `Invited` | Step 2 | 202, nothing sent, no info revealed |
| Organization missing (defensive) | Step 3 | 202, nothing sent, no info revealed |
| Database failure on save | Step 4 | 500 (the one case that does surface, since nothing was committed to reveal) |
| Email delivery fails | Step 5 | 202; the refreshed code is already saved, so the person can ask again |

## Related artifacts

[HLD](US-002-HLD.md), [SSD/SD diagrams](../us/US-002/README.md), [Domain model](../domain-models/README.md#accounts-and-organizations), [backend tests](../testing/README.md) (`AccountInvitationsEndpointTests` for `Invite`; `Resend` is tested alongside activation in `AccountActivationsEndpointTests`, since it lives on the same `/accounts/activation` controller).
