using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Abstractions.Options;

namespace QueryFlow.Validation;

/// <summary>
/// Validates a <see cref="QueryRequest"/> against a <see cref="QueryableConfig{T}"/> allowlist
/// and the process-wide <see cref="QueryFlowOptions"/> guardrails (max page size, max clause
/// counts). This is the single security choke point QueryFlow.Core calls before touching any
/// user-supplied field name, operator or page size.
/// </summary>
public static class QueryRequestValidator
{
    /// <summary>
    /// Validates <paramref name="request"/>, throwing a <see cref="QueryFlowException"/> subtype
    /// on the first violation found.
    /// </summary>
    public static void Validate<T>(QueryRequest request, QueryableConfig<T> config, QueryFlowOptions options)
    {
        ValidateFilters(request.Filters, config);
        if (request.FilterGroup is not null)
        {
            ValidateGroup(request.FilterGroup, config, options);
        }
        else if (request.Filters.Count > options.MaxFilterConditions)
        {
            throw new TooManyClausesException("filter conditions", request.Filters.Count, options.MaxFilterConditions);
        }

        if (request.Sort.Count > options.MaxSortFields)
        {
            throw new TooManyClausesException("sort fields", request.Sort.Count, options.MaxSortFields);
        }

        foreach (var sort in request.Sort)
        {
            if (!config.IsSortAllowed(sort.Field))
            {
                throw new InvalidFieldException(sort.Field);
            }
        }

        foreach (var field in request.Fields)
        {
            if (!config.IsSelectAllowed(field))
            {
                throw new InvalidFieldException(field);
            }
        }

        foreach (var include in request.Includes)
        {
            if (!config.IsIncludeAllowed(include))
            {
                throw new InvalidFieldException(include);
            }
        }

        if (request.Includes.Count > options.MaxIncludeDepth)
        {
            throw new TooManyClausesException("includes", request.Includes.Count, options.MaxIncludeDepth);
        }

        var effectivePageSize = request.PageSize ?? options.DefaultPageSize;
        if (effectivePageSize > options.MaxPageSize)
        {
            throw new PageSizeExceededException(effectivePageSize, options.MaxPageSize);
        }
    }

    private static void ValidateFilters<T>(IReadOnlyList<FilterCondition> conditions, QueryableConfig<T> config)
    {
        foreach (var condition in conditions)
        {
            condition.EnsureWellFormed();
            if (!config.IsFilterAllowed(condition.Field, condition.Operator))
            {
                throw config.IsKnownField(condition.Field)
                    ? new InvalidOperatorException(condition.Field, condition.Operator)
                    : new InvalidFieldException(condition.Field);
            }
        }
    }

    private static void ValidateGroup<T>(FilterGroup group, QueryableConfig<T> config, QueryFlowOptions options)
    {
        if (group.Conditions.Count > options.MaxFilterConditions)
        {
            throw new TooManyClausesException("filter conditions", group.Conditions.Count, options.MaxFilterConditions);
        }

        ValidateFilters(group.Conditions, config);

        foreach (var nested in group.Groups)
        {
            ValidateGroup(nested, config, options);
        }
    }

    /// <summary>
    /// Clamps the effective page size to <see cref="QueryFlowOptions.MaxPageSize"/> instead of
    /// throwing. Useful when <see cref="QueryFlowOptions.ThrowOnInvalidField"/>-style leniency is
    /// preferred over hard failures for this one guardrail.
    /// </summary>
    public static int ClampPageSize(int? requestedPageSize, QueryFlowOptions options)
    {
        var pageSize = requestedPageSize ?? options.DefaultPageSize;
        return Math.Clamp(pageSize, 1, options.MaxPageSize);
    }
}
