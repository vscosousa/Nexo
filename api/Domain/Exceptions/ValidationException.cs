namespace Nexo.Api.Domain.Exceptions;

/// <summary>Input failed validation; <see cref="Errors"/> maps each field to its messages.</summary>
public class ValidationException(IDictionary<string, string[]> errors) : Exception("Validation failed.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
