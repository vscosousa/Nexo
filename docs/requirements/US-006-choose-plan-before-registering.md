# US-006 - Choose a plan before registering an organization

[Requirements](README.md)

**ID and title:** US-006 - Choose a plan before registering an organization.
**Status:** draft.
**Story:** As a prospective admin, I want to see and pick a plan before entering my organization's details, so that I know what I'm signing up for (e.g. the Free tier's member limit) before creating an account.

**Preconditions:** none. This replaces `/register` as the entry point into org registration; the existing organization/admin form (US-001) moves one step later.

**Acceptance criteria:**

- Given the register-org link, when the person lands on it, then a plans page is shown first, listing each available plan's name, price, member limit, resource limit, and included functionality, plus a full feature comparison table across all plans.
- Given the plans page, when they pick a plan and continue, then they reach the existing organization/admin form (US-001) with that plan carried through.
- Given the organization/admin form is completed and submitted, when the request is created, then the organization is registered on the plan chosen on the plans page (not always "Free").
- Given no plan is selected, when they try to continue, then they stay on the plans page.

**Exceptions:**

- No plans are available to show (empty list) - not yet defined.
- The organization/admin form is reached directly (e.g., a bookmarked URL) without a plan chosen first - the user is sent to the plans page.

**Related artifacts:** [Domain model](../domain-models/README.md#organizations-and-accounts), [HLD](US-006-HLD.md), [LLD](US-006-LLD.md), [SSD/SD diagrams](../us/US-006/README.md), [database design](../database/README.md) (existing `Plans` table, `ResourceLimit`, `MonthlyPrice`, and five feature-flag columns added). Depends on [US-001](US-001-create-organization-admin.md); see the "Organization plans" note in the [scope](README.md#context-and-scope) (plans are simulated as a data field, no payment processing — `MonthlyPrice` is display-only).
