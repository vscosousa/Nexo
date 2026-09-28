# Interface design

[Documentation index](../README.md)

**Status:** application shell, landing page, and styled sign-in and register screens. Product wireframes, a visual system, and business-form interactions have not been agreed.

## Current experience

The router protects `/` with `RequireAuth`. Without a stored token, the browser moves to `/login`, where it shows the sign-in screen (email, password with a show/hide button, and Google sign-in) beside a brand panel on desktop. `/register` has the same layout. The rest of the app shell still uses the scaffold's styles and Vite assets, which are not an approved Nexo design system. See the [frontend guide](../../web/README.md).

## Planned journeys

| Journey | Requirement | Design coverage |
| --- | --- | --- |
| Register an organization and admin | [US-001](../requirements/US-001-create-organization-admin.md) | Proposed SSD/SD; no wireframe |
| Invite a member by email | [US-002](../requirements/US-002-register-member-email.md) | Proposed SSD/SD; no wireframe |
| Activate an invited account | [US-003](../requirements/US-003-create-member-account.md) | Proposed SSD/SD; no wireframe |
| Register a resource | [US-004](../requirements/US-004-register-resource.md) | Proposed SSD/SD; no wireframe |
| Sign in | [US-005](../requirements/US-005-sign-in.md) | Implemented; SSD/SD cover the password flow only |

The [story design index](../us/README.md) links the sequences. These describe intended behavior, not implemented screens.

## Journey template

For each future screen, record the related requirement, user goal, entry point, actions, completion state, and editable wireframe. Specify loading, empty, success, validation, and failure states alongside keyboard behavior, labels, focus handling, and contrast checks. Review these choices before implementing them.

## Shared conventions

Colors, typography, spacing, and shared controls are pending design review. Record agreed values here when selected, and link reusable components rather than copying their implementation into the documentation.

Conventions already in the code (`web/src/styles/theme.css`, `web/src/shared/preferences/`):

- **Palette and fonts.** Deep indigo (primary) and warm amber (accent) on cool grays; Plus Jakarta Sans for headings and body. The Figma file (`Primitives/Color`, plus a `Semantic/Color (Dark)` collection because the free plan allows one mode per collection) mirrors `web/src/styles/theme.css`.
- **Mobile-first.** Base styles target phones; `min-width` media queries at 640px and 1024px add room. Tap targets are at least `--size-touch` (44px) and inputs use 16px text so mobile browsers do not zoom on focus. Check each screen at phone width.
- **Light and dark themes.** Components use the semantic color tokens; dark values override them under `[data-theme="dark"]`. The theme follows the operating system until the user picks one, and only that pick is saved.
- **English and Portuguese.** UI copy lives in `shared/preferences/messages.ts`. The language follows the browser until the user picks one. Server validation messages are not translated.
