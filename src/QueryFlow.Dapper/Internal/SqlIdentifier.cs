using System.Text.RegularExpressions;

namespace QueryFlow.Dapper.Internal;

/// <summary>
/// Defense-in-depth beyond the field allowlist: a column name is only ever spliced into raw SQL
/// text after matching this pattern, so even a misconfigured column map can't introduce SQL
/// injection via an identifier.
/// </summary>
internal static partial class SqlIdentifier
{
    public static string Validate(string identifier)
    {
        if (!IdentifierPattern().IsMatch(identifier))
        {
            throw new ArgumentException($"'{identifier}' is not a valid SQL identifier.", nameof(identifier));
        }

        return identifier;
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?$")]
    private static partial Regex IdentifierPattern();
}
