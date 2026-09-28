# ADR-008 - Fake SMTP server for local email

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Proposed; implementation available for review.
**Date:** 2026-09-28.

## Context

[US-002](../requirements/US-002-register-member-email.md) invitations carry a one-time token that [US-003](../requirements/US-003-create-member-account.md) needs to activate the account, so the API must deliver email. The project is a local prototype with no paid services ([ADR-001](ADR-001-frontend-backend-stack.md)), and real customers' inboxes must never receive test mail. The developer also wants to switch to a real email service later without changing application code.

A fake SMTP server (see [Mailosaur's overview](https://mailosaur.com/blog/setting-up-a-fake-smtp-server-for-testing)) accepts SMTP but relays nothing, and shows what it captured in a web UI.

## Options

- Run an existing tool (smtp4dev, Mailpit): least code, but its UI cannot be restyled and it adds an external image to trust.
- Log emails to the console: no infrastructure, but hard to read links or HTML and nothing to click.
- Build a small fake SMTP service in the repository's own stack, with an inbox that looks like a webmail client.

## Decision

Add `tools/fake-smtp`, an ASP.NET Core service (one project, MimeKit for parsing) with three parts: an SMTP receiver, a store, and an HTTP API plus a static webmail-style page. Compose runs it as the `mail` service: SMTP on `127.0.0.1:1025`, inbox on `http://localhost:8025`.

- **Persistence:** every message is stored as an `.eml` file (plus a `.read` marker) in a directory mounted from the host (`MAIL_DATA_DIR`, default `./.data/mail`). `.data/` is git-ignored; only [`.env.example`](../../.env.example) is committed as the template for configuring it.
- **API side:** `IEmailSender` with an `SmtpEmailSender` (the .NET `SmtpClient`). Every setting comes from the `Email` configuration section: host, port, STARTTLS, credentials, sender, and web base URL. Compose defaults them to the fake server and reads overrides from `.env`, so a real SMTP provider needs configuration only. A provider that offers only an HTTP API needs one new `IEmailSender` implementation.
- **Invitations:** the token is no longer part of the invitation response. It is emailed as a link and as text. If the email cannot be sent, the new account is removed again and the request fails, so the address can be invited again.
- **Tests:** API tests replace `IEmailSender` with a capturing fake; the fake server has its own tests, and one test sends through the real `SmtpEmailSender` into it.

## Consequences

- One more container and project to build; the first build downloads MimeKit.
- The receiver is deliberately minimal: no TLS, no AUTH, no relaying, a 10 MB message limit, and it trusts every client. It is bound to loopback on the host and must not be exposed.
- The inbox is unauthenticated and stores plain message content, including invitation tokens; that is acceptable only because it is a local development tool.
- `SmtpClient` supports STARTTLS but not implicit TLS (port 465), and Microsoft discourages it for new large-scale work; this is enough for a prototype and replaceable behind `IEmailSender`.
- The activation link points at a `/activate` page that does not exist until the frontend is built; the token in the email also works directly against the API.

## Related artifacts

[Fake mail server guide](../guides/fake-mail-server.md), [technical reference](../reference/README.md#email), [architecture](../architecture/README.md), [ADR-007](ADR-007-local-docker-compose.md), [`.env.example`](../../.env.example), [Compose configuration](../../compose.yaml).
