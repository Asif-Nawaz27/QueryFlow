using QueryFlow.Abstractions.Enums;
using QueryFlow.Samples.WebApi.Models;
using QueryFlow.Validation;

namespace QueryFlow.Samples.WebApi.Queries;

/// <summary>
/// The allowlist for querying <see cref="Book"/>: which fields can be filtered (and with which
/// operators), sorted, searched, selected and eagerly included. This is what stands between a
/// client's query string and your database — see QueryFlow.Validation for the underlying model.
/// </summary>
public static class BookQueries
{
    public static readonly QueryableConfig<Book> Config = new QueryableConfig<Book>()
        .AllowFilter(b => b.Title, FilterOperator.Equals, FilterOperator.Contains, FilterOperator.StartsWith)
        .AllowFilter(b => b.Genre, FilterOperator.Equals, FilterOperator.In, FilterOperator.NotIn)
        .AllowFilter(b => b.Price, FilterOperator.Equals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.Between)
        .AllowFilter(b => b.Stock, FilterOperator.Equals, FilterOperator.GreaterThan, FilterOperator.LessThan)
        .AllowFilter(b => b.IsAvailable, FilterOperator.Equals)
        .AllowFilter(b => b.PublishedAt, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.Between)
        .AllowSort(b => b.Title)
        .AllowSort(b => b.Price)
        .AllowSort(b => b.Stock)
        .AllowSort(b => b.PublishedAt)
        .AllowSort(b => b.Id)
        .Searchable(b => b.Title)
        .Searchable(b => b.Description)
        .AllowSelect(b => b.Id)
        .AllowSelect(b => b.Title)
        .AllowSelect(b => b.Genre)
        .AllowSelect(b => b.Price)
        .AllowSelect(b => b.Stock)
        .AllowSelect(b => b.PublishedAt)
        .AllowInclude("Author");
}
