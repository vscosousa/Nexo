namespace Nexo.Api.Domain.Exceptions;

/// <summary>The request conflicts with existing state, e.g. an email that already has an account.</summary>
public class ConflictException(string message) : Exception(message);
