using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//создание подключения к Нюгет фиид

var repository = Repository.Factory.GetCoreV3(
    "https://localhost:7221/v3/index.json");

//проверка ответа

using var client = new HttpClient();

using var response = await client.GetAsync(
    "https://localhost:7221/v3/index.json");

//вивод
/* Что мы выяснили сервер доступен и возвращает JSON*/
System.Diagnostics.Debug.WriteLine(
    $"STATUS: {(int)response.StatusCode}");

System.Diagnostics.Debug.WriteLine(
    $"CONTENT TYPE: {response.Content.Headers.ContentType}");

var json = await client.GetStringAsync(
    "https://localhost:7221/v3/index.json");

System.Diagnostics.Debug.WriteLine($"ACTUAL JSON: {json}");

System.Diagnostics.Debug.WriteLine("BEFORE INDEX");

var index = await repository.GetResourceAsync<ServiceIndexResourceV3>();
System.Diagnostics.Debug.WriteLine("AFTER INDEX");
foreach (var type in new[]
{
    "SearchQueryService/3.0.0-beta",
    "SearchQueryService/3.0.0-rc",
    "SearchQueryService/3.0.0",
    "SearchQueryService/3.5.0"
})
{
    var uris = index.GetServiceEntryUris(type);

    System.Diagnostics.Debug.WriteLine(
        $"{type}: {uris.Count()}");
}

foreach (var uri in index.GetServiceEntryUris(
    "SearchQueryService/3.5.0"))
{
    System.Diagnostics.Debug.WriteLine($"SEARCH URI: {uri}");
}

var searchEntries = index.GetServiceEntryUris(
    "SearchQueryService/3.5.0");

System.Diagnostics.Debug.WriteLine(
    $"SEARCH URI COUNT: {searchEntries.Count()}");

System.Diagnostics.Debug.WriteLine(
    $"INDEX TYPE: {index.GetType().FullName}");

System.Diagnostics.Debug.WriteLine(
    $"INDEX SOURCE: {repository.PackageSource.Source}");

var entries = index.GetServiceEntries().ToList();

System.Diagnostics.Debug.WriteLine($"RESOURCE COUNT: {entries.Count}");

foreach (var entry in entries)
{
    System.Diagnostics.Debug.WriteLine(
        $"TYPE: {entry.Type} | URL: {entry.Uri}");
}


try
{
    var searchResource =
    await repository.GetResourceAsync<PackageSearchResource>();

    System.Diagnostics.Debug.WriteLine(
        $"SEARCH RESOURCE TYPE: {searchResource.GetType().FullName}");

    var fields = searchResource.GetType().GetFields(
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Public);

    foreach (var field in fields)
    {
        var value = field.GetValue(searchResource);

        if (value is IEnumerable<Uri> uris)
        {
            System.Diagnostics.Debug.WriteLine(
                $"FIELD: {field.Name}, COUNT: {uris.Count()}");

            foreach (var uri in uris)
            {
                System.Diagnostics.Debug.WriteLine($"URI: {uri}");
            }
        }
    }

    var results = await searchResource.SearchAsync(
    "FileSizeFormatter",
    new SearchFilter(false),
    0,
    20,
    NuGet.Common.NullLogger.Instance,
    CancellationToken.None
);

    foreach (var package in results)
    {
        System.Diagnostics.Debug.WriteLine(
            $"PACKAGE FOUND: {package.Identity.Id} {package.Identity.Version}");
    }

    System.Diagnostics.Debug.WriteLine(searchResource == null
        ? "Search service NOT FOUND"
        : "Search service FOUND");
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"NuGet error: {ex}");
}

app.Run();
