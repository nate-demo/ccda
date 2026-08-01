using CCDA.Web.Components;
using CCDA.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, resilience, and service discovery
// (which resolves the "ccda-api" logical name below when running under the AppHost).
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed client for the CCDA API. Under the Aspire AppHost the "http://ccda-api" address is
// resolved via service discovery (WithReference). When the web app is run on its own, set
// "ApiBaseUrl" (e.g. in appsettings.Development.json) to point at the API directly.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://ccda-api";
builder.Services.AddHttpClient<CcdaApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    // AI case-review calls (summaries, briefings, timelines) can be slower than a default request.
    client.Timeout = TimeSpan.FromSeconds(120);
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
