# Fake mail server

[Guides](README.md)

**Task:** read the emails Nexo sends during development, or point the API at a real email service.
**Starting conditions:** Docker Desktop, or the .NET SDK for the manual path. Design rationale: [ADR-008](../decisions/ADR-008-fake-smtp-server.md).

## Read emails locally

1. Start the stack from the repository root: `docker compose up --build` (see [Run locally](../../README.md#run-locally)).
2. Open the inbox at `http://localhost:8025`. It looks like a webmail client: search, unread counts, HTML/text/source views, attachments, delete.
3. Trigger an email, for example an invitation (`POST /organizations/{organizationId}/invitations`). It appears within a few seconds. Any SMTP client can also send to `localhost:1025`.

Without Docker, run `dotnet run --project tools/fake-smtp` (inbox on `http://localhost:8025`, SMTP on `127.0.0.1:1025`, files in `tools/fake-smtp/data/`), then start the API; its default settings already target `localhost:1025`.

## Where emails are stored

Each message is a plain `.eml` file in `MAIL_DATA_DIR` (default `./.data/mail`), so it survives restarts and can be opened in any mail client. The folder is git-ignored and must stay out of version control. To keep mail elsewhere, copy [`.env.example`](../../.env.example) to `.env` and change `MAIL_DATA_DIR`. **Delete all messages** in the inbox, or deleting the folder, resets it.

## Switch to a real email service

The API reads the `Email` settings listed in the [technical reference](../reference/README.md#email). No code changes are needed for a provider that offers SMTP.

1. Copy [`.env.example`](../../.env.example) to `.env` (git-ignored) and uncomment the `EMAIL_*` lines: host, port (usually 587), `EMAIL_ENABLE_SSL=true`, the provider's SMTP username and password, and a sender address the provider allows.
2. Set `EMAIL_WEB_BASE_URL` to the public address of the web app so links in emails work.
3. Restart the API: `docker compose up -d api`. The `mail` service can keep running; the API simply stops using it.

For a manual run, use `dotnet user-secrets set "Email:Host" "<host>"` (and `Email:Port`, `Email:EnableSsl`, `Email:Username`, `Email:Password`, `Email:From`) from `api/` instead. Never put credentials in `appsettings.json` or any committed file.

Limits: STARTTLS works but implicit TLS (port 465) does not. A provider that only offers an HTTP API needs a new `IEmailSender` implementation registered in `Program.cs` in place of `SmtpEmailSender`.

## Troubleshooting

| Symptom | Check | Next step |
| --- | --- | --- |
| Invitation request returns 500 | `docker compose logs api` shows an SMTP error | Make sure the `mail` service is running, or check the real provider's host, port, and credentials. The invited account is not kept, so retry |
| Inbox is empty | `docker compose ps` shows `mail` running | Confirm `Email__Host` is `mail` inside Compose (not `localhost`) |
| Port 1025 or 8025 in use | Another process holds the port | Stop it, or change the published ports in `compose.yaml` |
| Emails disappear after `down` | `MAIL_DATA_DIR` in `.env` | Data lives in that host folder; `down` does not remove it |
