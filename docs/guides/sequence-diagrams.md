# Designing SSD and SD diagrams

[Guides](README.md)

**Task:** design a user story's SSD (level 1) and SD (levels 2-3) diagrams.
**Starting conditions:** the story's HLD and LLD already exist (see [requirements](../requirements/README.md)); this guide covers the diagram *content* convention. For the mechanics of turning a `.puml` file into an `.svg`, see [diagrams.md](diagrams.md).

## The four diagrams per story

Every story gets exactly four sequence diagrams, of increasing detail:

1. **SSD level 1** (`docs/us/US-XXX/ssd/level-1/`): the actor versus **Nexo as one participant**. No internal structure at all, this is the contract with the outside world. See [SSD convention](../ssd/README.md).
2. **SD level 2** (`docs/us/US-XXX/sd/level-2/`): coarse, actor, `Web UI`, and `Nexo API` (the backend, as a whole, not a named controller; that belongs to level 3). This is where the frontend and backend first connect, but neither side's internals are shown yet.
3. **SD level 3 - Backend** (`docs/us/US-XXX/sd/level-3/backend/`): every backend participant in the collaboration (controller, service, repository interfaces, mapper, `NexoDbContext`, and a terminal `Database` participant for the actual persistence step), matching the layers in [architecture](../architecture/README.md) and the numbered steps in the story's LLD. **The caller is the frontend as a whole** (one `Web App` participant), never the actor directly: the actor only ever appears in the frontend diagram, since by this level the request has already left the browser.
4. **SD level 3 - Frontend** (`docs/us/US-XXX/sd/level-3/frontend/`): every frontend participant, a page-level **View**, a feature **Component** (the form or list that does the work), a feature API module (**Service**, e.g. `resourcesService`), and the shared **`HttpClient`** (the single Axios instance from `shared/http/client.ts`, per [ADR-004](../decisions/ADR-004-frontend-architecture.md)), ending at a single **`Nexo API`** participant representing the backend as a whole, not the specific controller class. The actor always talks to the View, never straight to a nested Component; the View forwards the actor's input to the Component that owns the form.

Level 1 and level 2 stay coarse on purpose: level 3 is where each side's real structure shows up, split in two so a single diagram never mixes frontend and backend implementation detail. Each side's diagram treats the other side as a single opaque participant at its boundary (`Web App` in the backend diagram, `Nexo API` in the frontend diagram), so neither one repeats the other's internals.

## Activation bars

Give every diagram a story ID, descriptive title, and level. Number messages, use readable line wrapping, and keep styling consistent. Keep unresolved contract details in notes so a rendered image remains understandable outside its README.

SSDs show only actor inputs and observable system outcomes. Do not add `System -> System` messages for validation, database access, or credential verification; put that work in the SDs. Distinguish accepted requests from validation, authorization, and conflict outcomes with `alt` branches.

At level 2, keep only the actor, Web UI, and Nexo API. At level 3, show validation before repository access, repository reads through the context/database, and a separate save step after entity tracking. `Add` tracks an entity; it does not imply that an INSERT has already committed. Show the save-failure result where the LLD defines it.

Keep success and error results separate. If a requirement promises automatic sign-in but its HLD returns no session, annotate that gap instead of inventing token delivery. Password-flow diagrams do not stand in for a designed OAuth exchange.

Every diagram shows an activation bar (a vertical box on the lifeline) for the duration a participant is doing work, added with explicit `activate X` / `deactivate X` lines around plantuml `->` calls:

```plantuml
Admin -> UI: Submit organization and admin details
activate UI
UI -> API: POST /organizations (RegisterOrganizationDto)
activate API
API --> UI: 201 Created (OrganizationDto) or 400/409
deactivate API
UI --> Admin: Show registration result
deactivate UI
```

Automatic sign-in after registration is a requirement, but US-001's credential and session contract remains unresolved; annotate that gap in complete diagrams.

Rules of thumb, the "ping pong" discipline every diagram follows:

- `activate` goes immediately after the arrow that starts the work; `deactivate` goes immediately after the arrow that reports it's done.
- **Never send a second, unrelated call to a participant that's still waiting on a reply from an earlier call.** Every `->` either gets its `-->` reply before the same target is called again, or is closed with its own `deactivate` first. A View that both "renders a form" and "forwards the actor's submission" to the same Component must let the first call return (or fold the two into one call) before issuing the second. Two open, unrelated calls stacked on one participant is the bug this guide exists to prevent.
- A call with no reply message (e.g. a repository `Add` that isn't read back before the next line) still gets a tight `activate`/`deactivate` pair, so the bar shows as a brief tick rather than being omitted.
- Inside an `alt`/`else` block that is a single round trip with alternative endings (most of our error-handling branches), put `deactivate` once, after the block's `end`. The participant was doing one continuous unit of work regardless of which branch it took.
- If a participant is called again later for an unrelated round trip (rare at level 2, common at level 3 when a repository is queried more than once), open and close a fresh `activate`/`deactivate` pair each time rather than leaving the bar open across unrelated calls.

## Naming participants

Use the concrete names the HLD/LLD already committed to, not generic placeholders:

- **Backend:** the actual class/interface names from the story's HLD folder structure (`OrganizationsController`, `OrganizationService`, `IAccountRepository`, `OrganizationMapper`, `NexoDbContext`, ...), plus a literal `Database` participant as the final hop for the persistence step (`NexoDbContext` issuing the actual `INSERT`/`UPDATE`/`SELECT`). The diagram's entry point is `Web App` (alias `FE`), the frontend as a whole, not an actor.
- **Frontend:** a View named after the page it renders (`RegisterOrganizationPage`), a Component named after the form it owns (`RegisterOrganizationForm`), a Service named after the feature API module it would live in under `web/src/features/<name>/` (`organizationsService`), and `HttpClient` for the shared Axios instance, ending at `Nexo API` (the backend, as one opaque participant, same alias used at level 2). These frontend files don't exist yet for any story (the frontend has no real screens yet, per [README.md](../../README.md#features)). The diagram documents the intended shape, matching [ADR-004](../decisions/ADR-004-frontend-architecture.md)'s feature-based convention, so the frontend can be scaffolded straight from it later. The actor's message always targets the View, never the Component directly; the View is what's on screen, the Component is a detail inside it. A single "submit" call from the View into the Component is enough; don't add a separate "render" call first, since it would sit open (unanswered) while the submit call reuses the same target.

## Worked example

See [US-001](../us/US-001/README.md) for a complete instance: [SSD level 1](../us/US-001/ssd/level-1/puml/US-001-level-1.puml), [SD level 2](../us/US-001/sd/level-2/puml/US-001-level-2.puml), [SD level 3 backend](../us/US-001/sd/level-3/backend/puml/US-001-level-3-backend.puml), [SD level 3 frontend](../us/US-001/sd/level-3/frontend/puml/US-001-level-3-frontend.puml).

## Folder layout

```text
docs/us/US-001/
├── ssd/
│   └── level-1/
│       ├── puml/US-001-level-1.puml
│       └── svg/US-001-level-1.svg
└── sd/
    ├── level-2/
    │   ├── puml/US-001-level-2.puml
    │   └── svg/US-001-level-2.svg
    └── level-3/
        ├── backend/
        │   ├── puml/US-001-level-3-backend.puml
        │   └── svg/US-001-level-3-backend.svg
        └── frontend/
            ├── puml/US-001-level-3-frontend.puml
            └── svg/US-001-level-3-frontend.svg
```

After writing or editing any `.puml` file here, render it per [diagrams.md](diagrams.md) and link both the `.puml` and `.svg` from the story's `docs/us/US-XXX/README.md`, following the [SD scenario template](../sd/README.md#scenario-template).
