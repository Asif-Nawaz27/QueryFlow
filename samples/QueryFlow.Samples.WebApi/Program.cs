using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QueryFlow.AspNetCore;
using QueryFlow.OpenApi;
using QueryFlow.Samples.WebApi.Data;
using QueryFlow.Samples.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(options => options.UseInMemoryDatabase("QueryFlowSampleLibrary"));

// Author <-> Book is a two-way navigation; without this, serializing an included
// Author.Books (each Book.Author pointing back) recurses forever.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddQueryFlow(options =>
{
    options.DefaultPageSize = 10;
    options.MaxPageSize = 50;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.AddQueryFlow());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    SeedData.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseQueryFlowExceptionHandling();
app.UseHttpsRedirection();

app.MapBookEndpoints();
app.MapAuthorEndpoints();

app.Run();

public partial class Program;
