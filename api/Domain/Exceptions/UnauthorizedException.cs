namespace Nexo.Api.Domain.Exceptions;

/// <summary>Authentication failed; the message must not reveal whether the account exists.</summary>
public class UnauthorizedException(string message) : Exception(message);
