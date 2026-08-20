# CCDA Deployment Guide

Two paths: **run locally** (no Azure needed — the default) and **deploy to Azure** (Bicep).

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.**

---

## 1. Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | **10.0** (LTS) | `dotnet --version` — pinned in `global.json`. |
| Azure Developer CLI (`azd`) | latest | Turnkey `azd up` deploy (§4.1). Install: `winget install microsoft.azd`. |
| Azure CLI | latest | Only for the manual Bicep path / offline validation. |
| Bicep | 0.43+ | `az bicep version`; used to validate `infra/`. |
| Docker | latest | Only for `azd up` — builds the API/Web container images. |
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

There are two ways to get to Azure:

- **§4.1 Turnkey — `azd up`** (recommended): one command provisions **everything**
  (Container Apps for the API + Web, Azure AI Search, Azure OpenAI, monitoring, a managed
  identity) and deploys the running apps. No Dockerfiles, no config hand-mapping.
- **§4.2 Manual — Bicep only**: provision just the dependency resources with `az deployment
  group create` and host the API/Web yourself (App Service / Container Apps). Use this if you
  can't run `azd`/Docker or want to deploy compute separately.

### 4.1 Turnkey deploy with `azd up` (recommended)

The .NET Aspire AppHost (`src/CCDA.AppHost`) is the deployment model. `azd` reads it,
**containerizes `ccda-api` + `ccda-web` with the .NET SDK container build (no Dockerfiles)**,
provisions an Azure Container Apps environment + registry + managed identity, reuses the
validated dependency Bicep in `infra/` (Search, Foundry, monitoring), and wires the deployed
API to Azure via the managed identity — **no keys in configuration**.

```powershell
# from the repo root — first run prompts for subscription, region, and env name
azd up
```

That's it. `azd` prints the API and Web URLs when it finishes. Because the AppHost injects the
Azure endpoints + provider switches into the API container app on publish, the deployed app
uses Azure AI Search + Azure OpenAI automatically (the offline mock providers are only used
when you run locally).

**Notes**

- **APIM is intentionally OFF** in `azd up` (the Developer SKU adds ~30-45 min). Deploy the API
  Management front door separately with the manual Bicep path below when you need it.
- **Managed identity, not keys.** A single user-assigned identity is attached to both container
  apps and granted the data-plane roles (Search Index Data Contributor + Service Contributor,
  Cognitive Services OpenAI User); `DefaultAzureCredential` uses it at runtime.
- **Validate offline** before deploying (no subscription needed):
  ```powershell
  az bicep build --file infra/main.bicep                 # dependency templates
  dotnet run --project src/CCDA.AppHost -- `             # azd deployment manifest
    --publisher manifest --output-path ./artifacts/manifest.json
  ```
- **Tear down:** `azd down --purge`.

### 4.2 Manual — provision dependencies with Bicep

Use this when you want to host the API/Web yourself or deploy the resources without `azd`.

#### 4.2.1 Validate the templates (offline)

```powershell
az bicep build --file infra/main.bicep
```

#### 4.2.2 Provision

```powershell
az group create --name rg-ccda-demo --location eastus2

az deployment group create `
  --resource-group rg-ccda-demo `
  --template-file infra/main.bicep `
  --parameters infra/main.parameters.json
```

> APIM (Developer SKU) takes ~30-45 minutes. For infra-only smoke tests pass
> `deployApim=false`.

#### 4.2.3 Point the app at Azure

Map the deployment outputs to configuration and flip the provider switches. A copy-paste
starting point is in [`docs/appsettings.Azure.sample.json`](appsettings.Azure.sample.json)
(appsettings form) and [`.env.example`](../.env.example) (environment-variable form). Full
mapping table + provider notes: [`infra/README.md`](../infra/README.md).

- `Azure:Search:Endpoint` ← `searchEndpoint`
- `Azure:Foundry:Provider=AzureOpenAI`, `Azure:Foundry:Endpoint` ← `foundryEndpoint`,
  `Azure:Foundry:ChatDeployment` ← `chatDeploymentName`
- `Ccda:Embeddings:Provider=AzureOpenAI`, `Ccda:Embeddings:Endpoint` ← `foundryEndpoint`,
  `Ccda:Embeddings:Deployment` ← `embeddingDeploymentName`
- `APPLICATIONINSIGHTS_CONNECTION_STRING` ← `appInsightsConnectionString`
- `AZURE_CLIENT_ID` ← `workloadClientId` (assign the user-assigned identity to the compute)

Leave the `ApiKey` values empty so both services authenticate with the managed identity
(`DefaultAzureCredential`) — the deployed Search + OpenAI resources have local auth disabled.

#### 4.2.4 Wire APIM to the backend (if deployed)

APIM routes to the URL in the `apiBackendUrl` parameter (default placeholder
`https://ccda-api.azurewebsites.net`). After the API is hosted, redeploy with
`apiBackendUrl` set to its real base URL — for a Container App:

```powershell
az containerapp show -g rg-ccda-demo -n <api-app> --query properties.configuration.ingress.fqdn -o tsv
# then re-run the deployment with --parameters apiBackendUrl=https://<fqdn>
```

### 4.3 Copilot Studio

Import the custom connector and reproduce the topics per
[`docs/copilot-studio/README.md`](copilot-studio/README.md). Set the connector host to the
APIM gateway and supply a `ccda`-product subscription key (see that guide §4.0 for creating the
product + key).

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
