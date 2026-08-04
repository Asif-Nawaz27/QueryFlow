using QueryFlow.Abstractions.Enums;
using QueryFlow.Samples.WebApi.Models;
using QueryFlow.Validation;

namespace QueryFlow.Samples.WebApi.Queries;

public static class AuthorQueries
{
    public static readonly QueryableConfig<Author> Config = new QueryableConfig<Author>()
        .AllowFilter(a => a.Name, FilterOperator.Equals, FilterOperator.Contains)
        .AllowFilter(a => a.Country, FilterOperator.Equals)
        .AllowSort(a => a.Name)
        .AllowSort(a => a.Id)
        .Searchable(a => a.Name)
        .AllowSelect(a => a.Id)
        .AllowSelect(a => a.Name)
        .AllowSelect(a => a.Country)
        .AllowInclude("Books");
}
