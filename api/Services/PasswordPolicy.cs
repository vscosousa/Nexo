using System.Text;

namespace Nexo.Api.Services;

/// <summary>Password rules: length, character classes, and no personal or organization terms.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const int MaxLength = 128;

    private const int MinTermLength = 3;

    /// <summary>Returns the rules the password breaks, empty when it is acceptable.</summary>
    /// <param name="forbiddenTerms">
    /// Names the password must not contain. Each term and each of its words is compared ignoring case, spaces,
    /// punctuation, and look-alike characters (<c>0/o</c>, <c>1/i/l</c>, <c>3/e</c>, <c>4/@/a</c>, <c>5/$/s</c>, <c>7/t</c>).
    /// </param>
    public static IReadOnlyList<string> Check(string password, params string?[] forbiddenTerms)
    {
        var errors = new List<string>();
        if (password.Length < MinLength)
            errors.Add($"The password must be at least {MinLength} characters.");
        if (password.Length > MaxLength)
            errors.Add($"The password must be at most {MaxLength} characters.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)
            || password.All(char.IsLetterOrDigit))
            errors.Add("The password must include an uppercase letter, a lowercase letter, a digit, and a symbol.");

        var folded = Fold(password);
        var forbidden = forbiddenTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .SelectMany(t => t!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Append(t))
            .Select(Fold)
            .Where(t => t.Length >= MinTermLength);
        if (forbidden.Any(folded.Contains))
            errors.Add("The password must not contain the organization name or your name.");

        return errors;
    }

    /// <summary>Lowercases, maps look-alike characters onto one letter, and drops everything but letters and digits.</summary>
    private static string Fold(string value)
    {
        var folded = new StringBuilder(value.Length);
        foreach (var c in value.ToLowerInvariant())
        {
            var mapped = c switch
            {
                '0' => 'o',
                '1' or 'i' or '|' => 'l',
                '3' => 'e',
                '4' or '@' => 'a',
                '5' or '$' => 's',
                '7' => 't',
                _ => c,
            };
            if (char.IsLetterOrDigit(mapped)) folded.Append(mapped);
        }
        return folded.ToString();
    }
}
