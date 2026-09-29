using System.Text;

namespace Nexo.Api.Services;

/// <summary>How hard a password is to guess; <see cref="PasswordStrength.Weak"/> is refused.</summary>
public enum PasswordStrength
{
    Weak,
    Reasonable,
    Strong,
    VeryStrong,
}

/// <summary>
/// Password rules, mirrored in the web app (<c>web/src/auth/passwordStrength.ts</c>) so its strength bar matches what
/// the API accepts. The kinds of character are uppercase, lowercase, digits, and symbols (anything else, spaces
/// included).
/// <para>
/// A password is <see cref="PasswordStrength.Weak"/>, and refused, when it is under 8 characters, contains a personal
/// or organization name, or mixes fewer than 3 kinds without being a passphrase of 16+ characters. Otherwise it scores
/// length points (0 from 8 characters, 1 from 12, 2 from 16, 3 from 20, 4 from 24) plus (kinds - 3), so each kind of
/// character is worth about four characters of length: <see cref="PasswordStrength.Reasonable"/> up to 1,
/// <see cref="PasswordStrength.Strong"/> at 2, <see cref="PasswordStrength.VeryStrong"/> from 3.
/// </para>
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const int MaxLength = 128;

    private static readonly int[] LengthSteps = [12, 16, 20, 24];

    private const int PassphraseLength = 16;

    private const int MinKinds = 3;

    private const int MinTermLength = 3;

    /// <summary>Rates the password.</summary>
    /// <param name="forbiddenTerms">
    /// Names the password must not contain. Each term and each of its words is compared ignoring case, spaces,
    /// punctuation, and look-alike characters (<c>0/o</c>, <c>1/i/l</c>, <c>3/e</c>, <c>4/@/a</c>, <c>5/$/s</c>, <c>7/t</c>).
    /// </param>
    public static PasswordStrength Measure(string password, params string?[] forbiddenTerms)
    {
        var kinds = Kinds(password);
        if (password.Length < MinLength || ContainsForbiddenTerm(password, forbiddenTerms)
            || (kinds < MinKinds && password.Length < PassphraseLength))
            return PasswordStrength.Weak;
        var score = LengthSteps.Count(step => password.Length >= step) + kinds - MinKinds;
        return score >= 3 ? PasswordStrength.VeryStrong
            : score == 2 ? PasswordStrength.Strong
            : PasswordStrength.Reasonable;
    }

    /// <summary>Returns why the password is refused, empty when it is at least <see cref="PasswordStrength.Reasonable"/>.</summary>
    /// <param name="forbiddenTerms">As in <see cref="Measure"/>.</param>
    public static IReadOnlyList<string> Check(string password, params string?[] forbiddenTerms)
    {
        if (password.Length > MaxLength)
            return [$"The password must be at most {MaxLength} characters."];
        if (password.Length < MinLength)
            return [$"The password must be at least {MinLength} characters."];
        if (ContainsForbiddenTerm(password, forbiddenTerms))
            return ["The password must not contain the organization name or your name."];
        if (Measure(password) == PasswordStrength.Weak)
            return ["The password is too weak: mix at least three of uppercase, lowercase, digits, and symbols, or use a passphrase of 16+ characters."];
        return [];
    }

    private static int Kinds(string password) =>
        new[]
        {
            password.Any(char.IsUpper),
            password.Any(char.IsLower),
            password.Any(char.IsDigit),
            !password.All(char.IsLetterOrDigit),
        }.Count(present => present);

    private static bool ContainsForbiddenTerm(string password, string?[] forbiddenTerms)
    {
        var folded = Fold(password);
        return forbiddenTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .SelectMany(t => t!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Append(t))
            .Select(Fold)
            .Where(t => t.Length >= MinTermLength)
            .Any(folded.Contains);
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
