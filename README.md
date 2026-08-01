# Contoso County District Attorney (CCDA) — AI Case Review Platform

A complete **demonstration repository** for a fictional District Attorney's office that
showcases a **citation-first, RAG-based legal case-review platform** built on Microsoft AI
technologies: **Azure AI Search**, **Azure AI Foundry**, **Azure API Management**,
**Microsoft Copilot Studio**, **.NET Aspire**, an **ASP.NET Core** web app, and a
**synthetic legal-data generator**.

> ⚠️ **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.**
> Every case, document, person, and record in this repository is synthetic and watermarked.
> Nothing here represents real persons, real cases, or real legal records.

## Guiding principles

- **AI assists, it never replaces attorney judgment.**
- **Every AI response is grounded** — it carries source **citations** with page-level
  document references and a **confidence** score.
- **All generated data is clearly fictional** and watermarked
  (`FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA`).

## Runs locally with no Azure subscription

Every external dependency (AI Search, Foundry/OpenAI embeddings + chat) sits behind an
interface with an **in-memory / local fallback selected by configuration**. With empty
configuration the whole demo runs **offline**; set the `Azure:*` settings to switch to
Azure-backed services. Azure resources ship as deploy-ready **Bicep + APIM policy** templates.

## Quick start

Prerequisites: **.NET 10 SDK** (pinned in [`global.json`](global.json)).

```powershell
# 1. Build & test the whole solution
dotnet build CCDA.slnx -c Release
dotnet test  CCDA.slnx -c Release

# 2. Generate a deterministic, fully-cited demo case (seed 42)
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- `
  generate-case --seed 42 --complexity Medium

# 3. Boot the full experience via the Aspire AppHost (dashboard + API + Web)
$env:ASPIRE_ALLOW_UNSECURED_TRANSPORT = "true"
dotnet run --project src/CCDA.AppHost/CCDA.AppHost.csproj
```

See **[`docs/demo.md`](docs/demo.md)** for the scripted walkthrough,
**[`docs/deployment.md`](docs/deployment.md)** for local + Azure deployment and config mapping,
and **[`docs/architecture.md`](docs/architecture.md)** for the Mermaid architecture diagrams.

## Architecture

```
Copilot Studio Agent ─┐
                      ├─► APIM (auth, rate-limit, versioning) ─► CCDA.Api
ASP.NET Core Web App ─┘                                           │
                                          ┌───────────────────────┼───────────────────────┐
                                          ▼                       ▼                       ▼
                                     CCDA.AI                 CCDA.Search             CCDA.DataGen
                              (Foundry: summary,        (AI Search: hybrid/         (synthetic cases +
                               briefing, timeline,       vector/semantic,            RAG eval +
                               legal Q&A + grounding)     chunking, citations)       auto-ingestion)
Aspire AppHost orchestrates everything + OpenTelemetry dashboard.
```

## Repository structure

| Path | Contents |
| --- | --- |
| [`src/`](src) | The 9 .NET projects (see status table below). |
| [`infra/`](infra) | Bicep for AI Search, Foundry, APIM + core, and APIM policies / OpenAPI. |
| [`docs/`](docs) | Architecture, deployment, facilitator, workshop, demo, and Copilot Studio guides. |
| [`sample-data/`](sample-data) | Committed deterministic seed-42 artifacts + regeneration guide. |
| [`tests/`](tests) | xUnit suite: determinism, search, AI citations, RAG eval, API contract. |
| [`.github/`](.github) | CI workflow + the CCDA Synthetic Case Generator Copilot agent & prompts. |

## Component status

All projects target **.NET 10** (`net10.0`). Validated end-to-end: solution builds with
**0 warnings / 0 errors**, **19/19** tests pass, the generator CLI is deterministic, and the
Aspire AppHost boots the dashboard + local demo path.

| Project | Type | Target | Builds | Tests | Notes |
| --- | --- | --- | --- | --- | --- |
| `CCDA.Shared` | Class library | net10.0 | ✅ | ✅ | Models, DTOs, citation & confidence types, fictional-data watermark. |
| `CCDA.ServiceDefaults` | Class library | net10.0 | ✅ | — | OpenTelemetry, health checks, resilience, service discovery. |
| `CCDA.Search` | Class library | net10.0 | ✅ | ✅ | `ISearchService` / `IEmbeddingService`; in-memory hybrid + Azure AI Search impls; chunking & page-level citations. |
| `CCDA.AI` | Class library | net10.0 | ✅ | ✅ | `IAiOrchestrator` — summary, briefing, timeline, legal Q&A; grounding + citation builder; local + Foundry impls. |
| `CCDA.Api` | ASP.NET Core (minimal API) | net10.0 | ✅ | ✅ | 8 endpoints; OpenAPI; versioned; wired to Search + AI + DataGen. |
| `CCDA.Web` | ASP.NET Core (Blazor) | net10.0 | ✅ | — | Upload, search, ask, briefings, timelines, citations, source pages, confidence. |
| `CCDA.DataGen` | Class library | net10.0 | ✅ | ✅ | Deterministic generators + auto-ingestion + indexing report + RAG eval set. |
| `CCDA.DataGen.Cli` | Console (`ccda-datagen`) | net10.0 | ✅ | ✅ | CLI mapping the `/generate-*` slash-commands; `--seed` reproducibility. |
| `CCDA.AppHost` | .NET Aspire AppHost | net10.0 | ✅ | — | Orchestrates Api/Web/deps + OpenTelemetry dashboard. |
| `CCDA.Tests` | xUnit test project | net10.0 | ✅ | ✅ | 19 tests across determinism, search, AI citations, RAG eval, API contract. |

> ✅ = verified · — = not applicable (no dedicated test project / component)

## Documentation

| Doc | Purpose |
| --- | --- |
| [`docs/architecture.md`](docs/architecture.md) | Logical, provider-selection, data-flow, physical, and security diagrams (Mermaid). |
| [`docs/deployment.md`](docs/deployment.md) | Local run, Azure deploy, deployment-output → app-config mapping, troubleshooting. |
| [`docs/facilitator.md`](docs/facilitator.md) | Executive story + a timed 15-minute demo flow. |
| [`docs/workshop.md`](docs/workshop.md) | Six-module hands-on lab. |
| [`docs/demo.md`](docs/demo.md) | Copy-paste seed-42 walkthrough. |
| [`docs/copilot-studio/`](docs/copilot-studio) | Copilot Studio topics, actions, knowledge sources, ALM, security + connector definition. |

## Out of scope

- Deploying to a live Azure subscription or publishing a live Copilot Studio agent.
- Real legal data of any kind.
