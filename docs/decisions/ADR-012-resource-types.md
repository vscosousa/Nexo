# ADR-012 - Resource types: system types plus per-organization custom types

[Documentation index](../README.md) · [Architecture decisions](README.md)

**Status:** Proposed; implemented for review.
**Date:** 2026-09-29.

## Context

Every [resource](../domain-models/README.md#resources) has a type. Organizations serve different purposes, so one fixed list does not fit all: the system provides a common set that every organization can use, and organizations on some plans can add their own. Managing custom types is a later story; [US-004](../requirements/US-004-register-resource.md) only needs a resource to reference a type the caller's organization can use. Tenancy is shared-schema per [ADR-011](ADR-011-multi-tenancy.md).

## Options

- **One table with a nullable `OrganizationId`.** `ResourceTypes(Id, Name, OrganizationId?)`: null marks a system type, a value marks that organization's custom type. `Resources.TypeId` is a single foreign key. The types an organization can use are one query (`OrganizationId IS NULL OR OrganizationId = @org`). Name uniqueness needs two partial indexes, and the app must stop organizations from editing system rows.
- **Two tables** (system and organization types). Ownership is explicit in the schema, but a resource must point to one of two tables: two nullable foreign keys plus a check constraint, or a polymorphic id with no foreign key. Every "types for this organization" read becomes a union.
- **Copy system types into each organization** on creation. A non-null foreign key and organizations can rename defaults, but changes to system types never reach existing organizations and org creation gains a step.
- **A database or schema per organization** for custom types. Rejected in [ADR-011](ADR-011-multi-tenancy.md); a resource's foreign key also cannot reach another database.

## Decision

One `ResourceTypes` table with a nullable `OrganizationId`. The system types `Equipment`, `Utensil`, `Vehicle`, and `Other` are seeded with fixed ids and a null `OrganizationId`. Rooms are not a resource type: spaces are a separate concept (see the [domain model](../domain-models/README.md#spaces)). An earlier `Room` type was replaced by `Utensil` in the `ReplaceRoomWithUtensilResourceType` migration, which moved any resource of type `Room` to `Other`. Its query filter (per [ADR-011](ADR-011-multi-tenancy.md)) shows system types plus the caller's organization's custom types, so a resource can only reference a type its organization can see.

Custom types are a plan feature: `Plans.HasCustomResourceTypes` is false on Free and true on Team and Enterprise. It is pricing data until the custom-type management story enforces it.

## Consequences

- Names are unique among system types, and unique per organization among custom types (two partial unique indexes). Whether a custom type may reuse a system type's name is left to the custom-type story.
- Changing a system type affects every organization at once.
- Moving a large tenant to its own database later (hybrid, [ADR-011](ADR-011-multi-tenancy.md)) needs the system rows copied into that database.

## Related artifacts

[ADR-011](ADR-011-multi-tenancy.md), [US-004](../requirements/US-004-register-resource.md), [Domain model](../domain-models/README.md#resources), [Database design](../database/README.md).
