using System.Text.Json.Serialization;
using CCDA.AI.DependencyInjection;
using CCDA.Api.Endpoints;
using CCDA.DataGen.DependencyInjection;
using CCDA.DataGen.Ingestion;
using CCDA.Search.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Shared OpenTelemetry, health checks, resilience, and service discovery.
builder.AddServiceDefaults();

// CCDA pillars. Order matters: DataGen depends on the Search registrations (CaseIndexer,
// ICaseCatalog). All three default to fully offline providers so the API runs with no live
// Azure subscription; configure the Azure:* / Ccda:* settings to switch to Azure-backed services.
builder.Services.AddCcdaSearch(builder.Configuration);
builder.Services.AddCcdaAi(builder.Configuration);
builder.Services.AddCcdaDataGen();

// Serialize enums as strings for a friendlier, APIM/Copilot-Studio-consumable contract.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapCaseEndpoints();

// Aggregate synthetic-data statistics for the Aspire "Synthetic Data Generation" dashboard widget.
app.MapGet("/api/v1/datagen/stats",
        async (SyntheticDataService dataService, CancellationToken cancellationToken) =>
            TypedResults.Ok(await dataService.GetStatsAsync(cancellationToken)))
    .WithName("GetSyntheticDataStats")
    .WithTags("Diagnostics")
    .WithSummary("Aggregate synthetic-data generation statistics for the dashboard.");

// Root: point callers at the API surface without exposing anything sensitive.
app.MapGet("/", () => Results.Ok(new
    {
        service = "CCDA Case Review API",
        classification = "FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA",
        docs = "/openapi/v1.json",
        cases = "/api/v1/cases",
    }))
    .ExcludeFromDescription();

app.Run();

/// <summary>Exposed so integration tests can boot the API via <c>WebApplicationFactory</c>.</summary>
public partial class Program;
