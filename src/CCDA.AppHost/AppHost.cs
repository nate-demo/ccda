using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

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
var web = builder.AddProject<Projects.CCDA_Web>("ccda-web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

// -----------------------------------------------------------------------------
// Azure deployment wiring — publish/azd only.
//
// This entire block is skipped when you run the AppHost locally (F5 / `dotnet
// run`), so the demo stays 100% offline: in-memory search + local embeddings +
// local chat, no Azure subscription required. It runs only during `azd up`
// (publish mode), where it:
//   1. creates a single user-assigned managed identity shared by both apps;
//   2. provisions the validated dependency Bicep (infra/main.bicep) verbatim,
//      granting that identity the data-plane RBAC roles it needs; and
//   3. points the deployed API at Azure AI Search + Azure OpenAI (Foundry) via
//      the identity — no keys ever land in configuration.
// -----------------------------------------------------------------------------
if (builder.ExecutionContext.IsPublishMode)
{
    // Explicit Azure Container Apps environment. Required so the app model can
    // attach an explicit user-assigned identity + role assignments to the apps.
    builder.AddAzureContainerAppEnvironment("ccda-env");

    // Shared workload identity. azd attaches it to the api/web container apps and
    // injects AZURE_CLIENT_ID, so DefaultAzureCredential authenticates as this
    // identity at runtime (keyless data-plane access to Search + Foundry).
    var identity = builder.AddAzureUserAssignedIdentity("ccda-identity");

    // Reuse the validated dependency Bicep as-is. The identity's principal is
    // passed in so the module grants RBAC to it instead of creating its own
    // identity. APIM is off by default here (Developer SKU ~30-45 min) — deploy
    // the API Management front door separately from infra/ when needed.
    var deps = builder.AddBicepTemplate("ccda-deps", "../../infra/main.bicep")
        .WithParameter("workloadPrincipalId", identity.Resource.PrincipalId)
        .WithParameter("deployApim", false);

    var appInsights = deps.GetOutput("appInsightsConnectionString");

    api
        .WithAzureUserAssignedIdentity(identity)
        .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsights)
        .WithEnvironment("Azure__Search__Endpoint", deps.GetOutput("searchEndpoint"))
        .WithEnvironment("Azure__Foundry__Provider", "AzureOpenAI")
        .WithEnvironment("Azure__Foundry__Endpoint", deps.GetOutput("foundryEndpoint"))
        .WithEnvironment("Azure__Foundry__ChatDeployment", deps.GetOutput("chatDeploymentName"))
        .WithEnvironment("Ccda__Embeddings__Provider", "AzureOpenAI")
        .WithEnvironment("Ccda__Embeddings__Endpoint", deps.GetOutput("foundryEndpoint"))
        .WithEnvironment("Ccda__Embeddings__Deployment", deps.GetOutput("embeddingDeploymentName"));

    web
        .WithAzureUserAssignedIdentity(identity)
        .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsights);
}

builder.Build().Run();
