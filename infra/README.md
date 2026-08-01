# CCDA Infrastructure (Bicep)

Deploy-ready Azure infrastructure for the **Contoso County District Attorney (CCDA)**
AI Case Review Platform demo. Everything here is validated offline with the Bicep
compiler; it is **not** auto-deployed. Provision it into a demo subscription when you
want to run the platform against real Azure services instead of the local mock providers.

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.**
> AI assists attorney review; it never replaces attorney judgment.

## What gets deployed

| Module | Resource | Purpose |
| --- | --- | --- |
| `modules/monitoring.bicep` | Log Analytics + Application Insights | Central telemetry for API, Web, and APIM (OpenTelemetry sink). |
| `modules/identity.bicep` | User-assigned managed identity | One keyless workload identity for the API/Web to reach Search + Foundry. |
| `modules/search.bicep` | Azure AI Search (`standard`) | Hybrid + semantic case index. RBAC-only; local keys disabled. |
| `modules/foundry.bicep` | Azure OpenAI + 2 model deployments | Chat (`gpt-4o-mini`) + embeddings (`text-embedding-3-small`). RBAC-only. |
| `modules/apim.bicep` | API Management (Developer) | Auth, rate-limit, versioning, App Insights monitoring; imports the OpenAPI. |

The workload identity is granted least-privilege **data-plane** roles:

- **Search Index Data Contributor** + **Search Service Contributor** on AI Search.
- **Cognitive Services OpenAI User** on Azure OpenAI.

No account keys are placed in application configuration — both services set
`disableLocalAuth: true` and the app authenticates with Entra ID via the managed identity.

## Validate offline

```bash
az bicep build --file infra/main.bicep
```

A zero exit code with no `BCP` warnings means the templates are well-formed. Every
module is also individually buildable (`az bicep build --file infra/modules/<name>.bicep`).

## Deploy

```bash
# 1. Create a resource group
az group create --name rg-ccda-demo --location eastus2

# 2. (Optional) Preview changes
az deployment group what-if \
  --resource-group rg-ccda-demo \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json

# 3. Deploy
az deployment group create \
  --resource-group rg-ccda-demo \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json
```

> **APIM is slow.** The Developer SKU takes ~30-45 minutes to provision. For a quick
> infra-only demo, pass `deployApim=false` and add APIM later.

## Wire the outputs into the app

`main.bicep` emits everything the API/Web need. Map the outputs to configuration
(`appsettings.json`, environment variables, or an App Service/Container App setting).
Setting these switches the app from the local mock providers to the Azure-backed ones.

| Deployment output | App configuration key |
| --- | --- |
| `searchEndpoint` | `Azure:Search:Endpoint` (Search auto-switches to Azure when set) |
| `foundryEndpoint` | `Azure:Foundry:Endpoint` **and** `Ccda:Embeddings:Endpoint` |
| `chatDeploymentName` | `Azure:Foundry:ChatDeployment` |
| `embeddingDeploymentName` | `Ccda:Embeddings:Deployment` |
| `appInsightsConnectionString` | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| `workloadClientId` | `AZURE_CLIENT_ID` (user-assigned identity for `DefaultAzureCredential`) |
| `apimApiUrl` | `ApiBaseUrl` for the web app (route through APIM) |

Also flip the provider switches so the app leaves the local mocks:
`Azure:Foundry:Provider=AzureOpenAI` and `Ccda:Embeddings:Provider=AzureOpenAI`. Leave the
`ApiKey` values empty so both services authenticate with the managed identity via
`DefaultAzureCredential` (the deployed resources have local auth disabled).

Assign the user-assigned managed identity (`identity.bicep`) to whatever compute hosts
the API and Web (App Service or Container Apps) so `DefaultAzureCredential` picks it up.

## API Management

- **`apim/ccda-openapi.yaml`** — the OpenAPI contract imported as the `ccda` API. It is
  also the basis for the Copilot Studio custom connector (see `docs/copilot-studio`).
- **`apim/policies/global.xml`** — gateway-wide CORS, correlation-id, coarse rate limit.
- **`apim/policies/api.xml`** — per-subscription rate limit + daily quota, backend
  routing, and a ready-to-enable **Microsoft Entra ID `validate-jwt`** block (fill in your
  tenant id + `api://<app-id>` audience to require OAuth2 for the web app and Copilot Studio).

Versioning uses the `Api-Version` header so the OpenAPI operation paths pass through
unchanged to the backend.

## Files

```
infra/
  main.bicep                 # resource-group orchestration
  main.parameters.json       # example parameters
  modules/
    monitoring.bicep
    identity.bicep
    search.bicep
    foundry.bicep
    apim.bicep
  apim/
    ccda-openapi.yaml        # imported API contract
    policies/
      global.xml
      api.xml
```
