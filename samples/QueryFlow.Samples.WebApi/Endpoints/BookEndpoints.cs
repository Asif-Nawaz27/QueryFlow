using Microsoft.EntityFrameworkCore;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.AspNetCore;
using QueryFlow.Core;
using QueryFlow.EFCore;
using QueryFlow.Samples.WebApi.Data;
using QueryFlow.Samples.WebApi.Queries;

namespace QueryFlow.Samples.WebApi.Endpoints;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books").WithTags("Books");

        // GET /books?filter=genre eq Romance&sort=-price&search=matchmaker&fields=id,title,price&page=1&pageSize=10
        group.MapGet("/", async (QueryRequestParameter q, LibraryDbContext db) =>
        {
            var request = q.Value;
            var query = db.Books.ApplyQuery(request, BookQueries.Config);

            if (request.Fields.Count == 0)
            {
                var page = await query.PaginateAsync(request);
                return Results.Ok(page);
            }

            var projected = query.SelectFields(request.Fields, BookQueries.Config);
            var projectedPage = await projected.PaginateAsync(request);
            return Results.Ok(new
            {
                projectedPage.Page,
                projectedPage.PageSize,
                projectedPage.TotalCount,
                projectedPage.TotalPages,
                Items = projectedPage.Items.Select(i => i.ToFieldDictionary())
            });
        })
        .WithName("GetBooks")
        .WithOpenApi();

        // GET /books/cursor?sort=id&pageSize=2  ->  response.nextCursor  ->  GET /books/cursor?sort=id&pageSize=2&cursor=<token>
        group.MapGet("/cursor", async (QueryRequestParameter q, LibraryDbContext db) =>
        {
            var request = q.Value;
            var query = db.Books.ApplyQuery(request, BookQueries.Config);
            var page = await query.CursorPaginateAsync(request);
            return Results.Ok(page);
        })
        .WithName("GetBooksCursor")
        .WithOpenApi();

        group.MapGet("/{id:int}", async (int id, LibraryDbContext db) =>
            await db.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id) is { } book
                ? Results.Ok(book)
                : Results.NotFound())
        .WithName("GetBookById")
        .WithOpenApi();

        // GET /books/stats?filter=genre eq Mystery
        group.MapGet("/stats", async (QueryRequestParameter q, LibraryDbContext db) =>
        {
            var request = q.Value;
            var query = db.Books.Filter(request, BookQueries.Config).Search(request.Search, BookQueries.Config);

            var aggregates = request.Aggregates.Count > 0
                ? request.Aggregates
                : (IReadOnlyList<AggregateRequest>)
                [
                    new AggregateRequest(AggregateFunction.Count),
                    new AggregateRequest(AggregateFunction.Sum, "Stock"),
                    new AggregateRequest(AggregateFunction.Average, "Price")
                ];

            var results = await query.AggregateAsync(aggregates, BookQueries.Config);
            return Results.Ok(results);
        })
        .WithName("GetBookStats")
        .WithOpenApi();

        return app;
    }
}
