using QueryFlow.AspNetCore;
using QueryFlow.Core;
using QueryFlow.EFCore;
using QueryFlow.Samples.WebApi.Data;
using QueryFlow.Samples.WebApi.Queries;

namespace QueryFlow.Samples.WebApi.Endpoints;

public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/authors").WithTags("Authors");

        // GET /authors?search=austen&include=books&sort=name
        group.MapGet("/", async (QueryRequestParameter q, LibraryDbContext db) =>
        {
            var request = q.Value;
            var query = db.Authors.ApplyQuery(request, AuthorQueries.Config);
            var result = await query.PaginateAsync(request);
            return Results.Ok(result);
        })
        .WithName("GetAuthors");

        return app;
    }
}
