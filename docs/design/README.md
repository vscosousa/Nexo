# Interface design

[Documentation index](../README.md)

**Status:** application shell and login placeholder only. Product wireframes, a visual system, and business-form interactions have not been agreed.

## Current experience

The router protects `/` with `RequireAuth`. Without a stored token, the browser moves to `/login`, where it shows a heading and implementation placeholder. The scaffold's styles and Vite assets are not an approved Nexo design system. See the [frontend guide](../../web/README.md).

## Planned journeys

| Journey | Requirement | Design coverage |
| --- | --- | --- |
| Register an organization and admin | [US-001](../requirements/US-001-create-organization-admin.md) | Proposed SSD/SD; no wireframe |
| Invite a member by email | [US-002](../requirements/US-002-register-member-email.md) | Proposed SSD/SD; no wireframe |
| Activate an invited account | [US-003](../requirements/US-003-create-member-account.md) | Proposed SSD/SD; no wireframe |
| Register a resource | [US-004](../requirements/US-004-register-resource.md) | Proposed SSD/SD; no wireframe |
| Sign in | [US-005](../requirements/US-005-sign-in.md) | Proposed SSD/SD; placeholder screen |

The [story design index](../us/README.md) links the sequences. These describe intended behavior, not implemented screens.

## Journey template

For each future screen, record the related requirement, user goal, entry point, actions, completion state, and editable wireframe. Specify loading, empty, success, validation, and failure states alongside keyboard behavior, labels, focus handling, and contrast checks. Review these choices before implementing them.

## Shared conventions

Colors, typography, spacing, and shared controls are pending design review. Record agreed values here when selected, and link reusable components rather than copying their implementation into the documentation.
