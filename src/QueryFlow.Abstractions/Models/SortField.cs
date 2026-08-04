using QueryFlow.Abstractions.Enums;

namespace QueryFlow.Abstractions.Models;

/// <summary>
/// A single field to order results by.
/// </summary>
/// <param name="Field">The logical field name as exposed to clients.</param>
/// <param name="Direction">The sort direction.</param>
public sealed record SortField(string Field, SortDirection Direction = SortDirection.Ascending)
{
    /// <summary>
    /// Parses a single sort token, e.g. <c>"LastName"</c> (ascending) or <c>"-CreatedDate"</c> (descending).
    /// </summary>
    public static SortField Parse(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        token = token.Trim();

        return token[0] switch
        {
            '-' => new SortField(token[1..].Trim(), SortDirection.Descending),
            '+' => new SortField(token[1..].Trim(), SortDirection.Ascending),
            _ => new SortField(token, SortDirection.Ascending)
        };
    }
}
