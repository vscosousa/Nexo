# Interface design

[Documentation index](../README.md)

**Status:** application shell, landing page, and styled sign-in and register screens. Product wireframes, a visual system, and business-form interactions have not been agreed.

## Current experience

The router protects `/` with `RequireAuth`. Without a stored token, the browser moves to `/login`, where it shows the sign-in screen (email, password with a show/hide button, and Google sign-in) beside a brand panel on desktop. `/register` has the same layout. After signing in, `/app` opens the app shell: a dark sidebar (a top bar on phones) with the logo and tagline, the built sections and, at its foot, the person (initials, name, and role) and sign-out; there is no language switch inside the app. Each page has an 80px top bar on the page background (title on the left, the page's main action on the right) above full-width content in flat bordered cards. `/app` is the dashboard (a welcome card with shortcuts) and `/app/resources` the Resources section, where admin and staff register resources in a modal dialog (US-004). See the [frontend guide](../../web/README.md).

## Planned journeys

| Journey | Requirement | Design coverage |
| --- | --- | --- |
| Register an organization and admin | [US-001](../requirements/US-001-create-organization-admin.md) | Proposed SSD/SD; no wireframe |
| Invite a member by email | [US-002](../requirements/US-002-register-member-email.md) | Proposed SSD/SD; no wireframe |
| Activate an invited account | [US-003](../requirements/US-003-create-member-account.md) | Proposed SSD/SD; no wireframe |
| Register a resource | [US-004](../requirements/US-004-register-resource.md) | Implemented. Layout follows the Figma `App Screens` page (`nexo-dashboard` for the shell, `nexo-modal-nova-reserva` for the dialog), restyled with the Foundation tokens below; those mockups use a teal palette and a serif title face that the Foundation page and `theme.css` do not |
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
- **App shell.** Proportions follow the Figma `nexo-dashboard` frame at 1440px: a 220px sidebar, an 80px top bar, 40px content padding, cards with a 1px border, 4px corners, and no shadow; colors and type come from the tokens, not the mockup. The sidebar stays dark in both themes by overriding the semantic tokens on `.app-sidebar`, so shared controls inside it (wordmark, language switch, icon buttons) adapt without variants. Only built sections appear in the navigation. Forms that create something open in a native `<dialog>` (`showModal`), which provides focus trapping, Escape to close, and the backdrop.
- **App-drawn controls.** Controls whose open state the browser draws (dropdown lists) are replaced by app components styled with the tokens: `shared/Select` follows the WAI-ARIA select-only combobox pattern (arrows, Home/End, type-to-find, Enter/Space/Tab to pick, Escape to close without closing a surrounding dialog) and submits through a hidden input. Its list is fixed-positioned so a scrolling dialog cannot clip it, and closes when the page scrolls or resizes, but not when the list itself scrolls.
- **English and Portuguese.** UI copy lives in `shared/preferences/messages.ts`. The language follows the browser until the user picks one. Server validation messages are not translated.
