# CCDA Deployment Guide

Two paths: **run locally** (no Azure needed — the default) and **deploy to Azure** (Bicep).

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.**

---

## 1. Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | **10.0** (LTS) | `dotnet --version` — pinned in `global.json`. |
| Azure CLI | latest | Only for Azure deployment / Bicep validation. |
| Bicep | 0.43+ | `az bicep version`; used to validate `infra/`. |
| Power Platform CLI (`pac`) | latest | Only for the Copilot Studio connector ALM. |

The .NET Aspire AppHost uses the NuGet-based Aspire SDK — **no separate workload install** is
required on .NET 10.

---

## 2. Run locally (default, offline)

Everything runs against in-memory search + a local extractive chat provider, so no Azure
resources are needed.

### Option A — Aspire AppHost (recommended)

```powershell
# from the repo root
$env:ASPIRE_ALLOW_UNSECURED_TRANSPORT = "true"   # dev-only, allows http endpoints
dotnet run --project src/CCDA.AppHost/CCDA.AppHost.csproj
```

The console prints the **Aspire dashboard** URL (OpenTelemetry traces/metrics/logs). From the
dashboard you can open the API (Swagger, `Synthetic Data Generation` stats) and the web app.

### Option B — API + Web standalone

```powershell
# terminal 1 — API on http://localhost:5144
dotnet run --project src/CCDA.Api/CCDA.Api.csproj

# terminal 2 — Web on http://localhost:5271 (uses ApiBaseUrl from appsettings.Development.json)
dotnet run --project src/CCDA.Web/CCDA.Web.csproj --launch-profile http
```

### Seed a demo case

Via the API:

```powershell
curl -X POST http://localhost:5144/api/v1/cases/generate-demo-data `
  -H "Content-Type: application/json" `
  -d '{ "complexity": "Medium", "seed": 42 }'
```

Or via the generator CLI:

```powershell
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- generate-case --seed 42
```

CLI commands: `generate-case`, `generate-cases --count N`, `generate-investigation`,
`generate-inmate-history`, `generate-rag-eval --count N`. Options: `--type`, `--complexity`,
`--pages`, `--seed`, `--output <file>`. Identical `--seed` ⇒ identical output.

---

## 3. Build & test

```powershell
dotnet build CCDA.slnx
dotnet test CCDA.slnx
```

---

## 4. Deploy to Azure

### 4.1 Validate the templates (offline)

```powershell
az bicep build --file infra/main.bicep
```

### 4.2 Provision

```powershell
az group create --name rg-ccda-demo --location eastus2

az deployment group create `
  --resource-group rg-ccda-demo `
  --template-file infra/main.bicep `
  --parameters infra/main.parameters.json
```

> APIM (Developer SKU) takes ~30-45 minutes. For infra-only smoke tests pass
> `deployApim=false`.

### 4.3 Point the app at Azure

Map the deployment outputs to configuration and flip the provider switches. See the mapping
table and provider notes in [`infra/README.md`](../infra/README.md):

- `Azure:Search:Endpoint` ← `searchEndpoint`
- `Azure:Foundry:Provider=AzureOpenAI`, `Azure:Foundry:Endpoint` ← `foundryEndpoint`,
  `Azure:Foundry:ChatDeployment` ← `chatDeploymentName`
- `Ccda:Embeddings:Provider=AzureOpenAI`, `Ccda:Embeddings:Endpoint` ← `foundryEndpoint`,
  `Ccda:Embeddings:Deployment` ← `embeddingDeploymentName`
- `APPLICATIONINSIGHTS_CONNECTION_STRING` ← `appInsightsConnectionString`
- `AZURE_CLIENT_ID` ← `workloadClientId` (assign the user-assigned identity to the compute)

Leave the `ApiKey` values empty so both services authenticate with the managed identity
(`DefaultAzureCredential`) — the deployed Search + OpenAI resources have local auth disabled.

### 4.4 Copilot Studio

Import the custom connector and reproduce the topics per
[`docs/copilot-studio/README.md`](copilot-studio/README.md). Set the connector host to the
APIM gateway and supply a `ccda`-product subscription key.

---

## 5. Environments

| Concern | Local | Azure demo |
| --- | --- | --- |
| Search | `InMemorySearchService` | Azure AI Search (RBAC) |
| Embeddings | `LocalEmbeddingService` | Azure OpenAI embeddings |
| Chat | `LocalChatCompletionService` | Azure OpenAI chat |
| Gateway | none (direct) | API Management |
| Telemetry | Aspire dashboard | Application Insights + Log Analytics |
| Identity | none | User-assigned managed identity |

---

## 6. Troubleshooting

| Symptom | Fix |
| --- | --- |
| AppHost fails on `https` endpoints | set `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` for local dev. |
| Web shows no cases | seed a case first (§2) — the in-memory store starts empty and is per-process. |
| `az bicep build` warns to upgrade | optional; 0.43+ compiles the templates fine. |
| APIM 401 | supply a valid `ccda`-product subscription key (or a token if `validate-jwt` is enabled). |
| Azure auth errors | ensure the managed identity has the RBAC roles from `infra/` and `AZURE_CLIENT_ID` is set. |
