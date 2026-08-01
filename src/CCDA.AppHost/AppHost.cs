using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// The CCDA API. Defaults to fully offline providers (in-memory search + local embeddings +
// local chat), so the whole demo runs with no live Azure subscription. Set the Azure:* settings
// (e.g. via user-secrets or environment) to switch to Azure AI Search / Foundry.
var api = builder.AddProject<Projects.CCDA_Api>("ccda-api")
    .WithExternalHttpEndpoints();

// "Synthetic Data Generation" dashboard surface: surface the live aggregate stats endpoint
// (cases / documents / pages / chunks / retrieval score / index health / last-generation time)
// and the OpenAPI document as clickable links on the API resource in the Aspire dashboard.
api.WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation
{
    Url = "/api/v1/datagen/stats",
    DisplayText = "Synthetic Data Generation",
});

api.WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation
{
    Url = "/openapi/v1.json",
    DisplayText = "OpenAPI",
});

// The web experience talks to the API through service discovery (WithReference), and waits for
// the API to be ready before starting so the first request never races startup.
builder.AddProject<Projects.CCDA_Web>("ccda-web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
