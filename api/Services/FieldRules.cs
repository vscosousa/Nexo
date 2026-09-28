using System.ComponentModel.DataAnnotations;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Services;

/// <summary>Shared field checks that add field-level messages to a validation error map.</summary>
internal static class FieldRules
{
    public static void Email(IDictionary<string, string[]> errors, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !new EmailAddressAttribute().IsValid(value))
            errors[key] = ["A valid email is required."];
        else if (value.Trim().Length > Account.EmailMaxLength)
            errors[key] = [$"The email must be at most {Account.EmailMaxLength} characters."];
    }

    public static void Text(IDictionary<string, string[]> errors, string key, string label, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[key] = [$"{label} is required."];
        else if (value.Trim().Length > maxLength)
            errors[key] = [$"{label} must be at most {maxLength} characters."];
    }

    /// <summary>Checks presence and <see cref="PasswordPolicy"/>, treating the given names as forbidden content.</summary>
    public static void Password(
        IDictionary<string, string[]> errors, string key, string? value, params string?[] forbiddenTerms)
    {
        if (string.IsNullOrEmpty(value))
            errors[key] = ["A password is required."];
        else if (PasswordPolicy.Check(value, forbiddenTerms) is { Count: > 0 } broken)
            errors[key] = [.. broken];
    }
}
