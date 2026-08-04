using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Abstractions.Options;
using QueryFlow.Core.Cursor;
using QueryFlow.Core.Internal;
using QueryFlow.Expressions;
using QueryFlow.Validation;

namespace QueryFlow.Core;

/// <summary>
/// Adds offset pagination (<c>?page=&amp;pageSize=</c>) and stable, keyset-based cursor
/// pagination (<c>?cursor=</c>) to any <see cref="IQueryable{T}"/>.
/// </summary>
public static class PaginationQueryableExtensions
{
    // ----- Offset pagination -----

    public static PagedResult<T> Paginate<T>(this IQueryable<T> source, QueryRequest request, QueryFlowOptions? options = null)
    {
        options ??= new QueryFlowOptions();
        var (page, pageSize) = ResolvePage(request, options);

        var totalCount = source.LongCount();
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return PagedResult<T>.Create(items, page, pageSize, totalCount);
    }

    public static async Task<PagedResult<T>> PaginateAsync<T>(
        this IQueryable<T> source,
        QueryRequest request,
        QueryFlowOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QueryFlowOptions();
        var (page, pageSize) = ResolvePage(request, options);

        var totalCount = await EfCoreAsyncBridge.LongCountAsync(source, cancellationToken).ConfigureAwait(false);
        var page0 = source.Skip((page - 1) * pageSize).Take(pageSize);
        var items = await EfCoreAsyncBridge.ToListAsync(page0, cancellationToken).ConfigureAwait(false);
        return PagedResult<T>.Create(items, page, pageSize, totalCount);
    }

    private static (int Page, int PageSize) ResolvePage(QueryRequest request, QueryFlowOptions options)
    {
        var pageSize = QueryRequestValidator.ClampPageSize(request.PageSize, options);
        var page = Math.Max(1, request.Page ?? 1);
        return (page, pageSize);
    }

    // ----- Cursor (keyset) pagination -----

    public static CursorPagedResult<T> CursorPaginate<T>(this IQueryable<T> source, QueryRequest request, QueryFlowOptions? options = null)
    {
        var (query, ctx) = PrepareCursorQuery(source, request, options ?? new QueryFlowOptions());
        var items = query.ToList();
        return BuildResult<T>(items, ctx);
    }

    public static async Task<CursorPagedResult<T>> CursorPaginateAsync<T>(
        this IQueryable<T> source,
        QueryRequest request,
        QueryFlowOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var (query, ctx) = PrepareCursorQuery(source, request, options ?? new QueryFlowOptions());
        var items = await EfCoreAsyncBridge.ToListAsync(query, cancellationToken).ConfigureAwait(false);
        return BuildResult<T>(items, ctx);
    }

    private readonly record struct CursorContext(
        int PageSize,
        IReadOnlyList<SortField> SortFields,
        bool Backward,
        bool HadCursor,
        byte[] Key);

    private static (IQueryable<T> Query, CursorContext Context) PrepareCursorQuery<T>(
        IQueryable<T> source,
        QueryRequest request,
        QueryFlowOptions options)
    {
        if (request.Sort.Count == 0)
        {
            throw new ArgumentException(
                "Cursor pagination requires at least one sort field for a stable order. Include 'Sort' on the request.",
                nameof(request));
        }

        var pageSize = QueryRequestValidator.ClampPageSize(request.PageSize, options);
        var key = options.CursorSigningKey ?? ProcessLocalCursorKey.Value;

        var backward = false;
        var query = source;
        var hadCursor = !string.IsNullOrEmpty(request.Cursor);

        if (hadCursor)
        {
            var payload = CursorCodec.Decode(request.Cursor!, key);
            backward = payload.Backward;
            var lastValues = CursorKeyExtractor.ToValues(payload.Keys);
            var seekPredicate = CursorSeekExpressionBuilder.Build<T>(request.Sort, lastValues, backward);
            query = query.Where(seekPredicate);
        }

        var effectiveSort = backward ? ReverseDirections(request.Sort) : request.Sort;
        var ordered = SortExpressionBuilder.Apply(query, effectiveSort).Take(pageSize + 1);

        return (ordered, new CursorContext(pageSize, request.Sort, backward, hadCursor, key));
    }

    private static CursorPagedResult<T> BuildResult<T>(List<T> items, CursorContext ctx)
    {
        var hasMore = items.Count > ctx.PageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        if (ctx.Backward)
        {
            items.Reverse();
        }

        string? nextCursor = null;
        string? previousCursor = null;

        if (items.Count > 0)
        {
            var hasNext = ctx.Backward || hasMore;
            var hasPrevious = ctx.Backward ? hasMore : ctx.HadCursor;

            if (hasNext)
            {
                var keys = CursorKeyExtractor.ExtractKeys(items[^1], ctx.SortFields);
                nextCursor = CursorCodec.Encode(new CursorPayload(keys, Backward: false), ctx.Key);
            }

            if (hasPrevious)
            {
                var keys = CursorKeyExtractor.ExtractKeys(items[0], ctx.SortFields);
                previousCursor = CursorCodec.Encode(new CursorPayload(keys, Backward: true), ctx.Key);
            }
        }

        return new CursorPagedResult<T>
        {
            Items = items,
            PageSize = ctx.PageSize,
            NextCursor = nextCursor,
            PreviousCursor = previousCursor
        };
    }

    private static IReadOnlyList<SortField> ReverseDirections(IReadOnlyList<SortField> sortFields) =>
        sortFields
            .Select(sf => sf with
            {
                Direction = sf.Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending
            })
            .ToList();
}
