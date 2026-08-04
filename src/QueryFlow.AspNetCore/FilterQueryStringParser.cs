using System.Text.RegularExpressions;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;

namespace QueryFlow.AspNetCore;

/// <summary>
/// Parses the compact <c>?filter=age gt 18 and city eq "New York"</c> query-string DSL into
/// <see cref="FilterCondition"/> objects. For richer nesting (parenthesized AND/OR groups), post
/// a JSON body with a <c>filters</c> array or <c>filterGroup</c> tree instead — the DSL is
/// intentionally flat.
/// </summary>
public static partial class FilterQueryStringParser
{
    private static readonly Dictionary<string, FilterOperator> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eq"] = FilterOperator.Equals,
        ["ne"] = FilterOperator.NotEquals,
        ["gt"] = FilterOperator.GreaterThan,
        ["gte"] = FilterOperator.GreaterThanOrEqual,
        ["lt"] = FilterOperator.LessThan,
        ["lte"] = FilterOperator.LessThanOrEqual,
        ["between"] = FilterOperator.Between,
        ["in"] = FilterOperator.In,
        ["nin"] = FilterOperator.NotIn,
        ["contains"] = FilterOperator.Contains,
        ["startswith"] = FilterOperator.StartsWith,
        ["endswith"] = FilterOperator.EndsWith,
        ["isnull"] = FilterOperator.IsNull,
        ["isnotnull"] = FilterOperator.IsNotNull
    };

    public static IReadOnlyList<FilterCondition> Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Array.Empty<FilterCondition>();
        }

        var tokens = Tokenize(expression);
        var items = new List<(FilterCondition Condition, LogicalOperator? ConnectorToNext)>();
        var i = 0;

        while (i < tokens.Count)
        {
            if (i + 1 >= tokens.Count)
            {
                throw new FormatException($"Incomplete filter expression near '{tokens[i]}'.");
            }

            var field = Unquote(tokens[i++]);
            var opToken = Unquote(tokens[i++]);
            if (!Operators.TryGetValue(opToken, out var op))
            {
                throw new FormatException($"Unknown filter operator '{opToken}'.");
            }

            object? value = null;
            IReadOnlyList<object?>? values = null;

            if (op is not (FilterOperator.IsNull or FilterOperator.IsNotNull))
            {
                if (i >= tokens.Count)
                {
                    throw new FormatException($"Filter operator '{opToken}' on field '{field}' requires a value.");
                }

                var rawValue = Unquote(tokens[i++]);
                if (op is FilterOperator.Between or FilterOperator.In or FilterOperator.NotIn)
                {
                    values = rawValue.Split(',', StringSplitOptions.TrimEntries).Select(ParseScalar).ToArray();
                }
                else
                {
                    value = ParseScalar(rawValue);
                }
            }

            var condition = new FilterCondition(field, op, value, values);

            LogicalOperator? connector = null;
            if (i < tokens.Count)
            {
                var next = tokens[i].ToLowerInvariant();
                if (next is "and" or "or")
                {
                    connector = next == "or" ? LogicalOperator.Or : LogicalOperator.And;
                    i++;
                }
            }

            items.Add((condition, connector));
        }

        return items
            .Select(item => item.Condition with { Connector = item.ConnectorToNext ?? LogicalOperator.And })
            .ToList();
    }

    private static object? ParseScalar(string token)
    {
        if (bool.TryParse(token, out var b))
        {
            return b;
        }

        if (long.TryParse(token, out var l))
        {
            return l;
        }

        if (double.TryParse(token, System.Globalization.CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        return token;
    }

    private static string Unquote(string token)
    {
        if (token.Length >= 2 && ((token[0] == '"' && token[^1] == '"') || (token[0] == '\'' && token[^1] == '\'')))
        {
            return token[1..^1];
        }

        return token;
    }

    private static List<string> Tokenize(string expression) =>
        TokenPattern().Matches(expression).Select(m => m.Value).ToList();

    [GeneratedRegex("""("[^"]*"|'[^']*'|\S+)""")]
    private static partial Regex TokenPattern();
}
