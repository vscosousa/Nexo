namespace Nexo.Api.Domain.Exceptions;

/// <summary>The caller is not allowed to perform the action, e.g. a non-admin inviting a member.</summary>
public class ForbiddenException(string message) : Exception(message);
