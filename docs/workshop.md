# CCDA Workshop

A hands-on lab that walks builders through the platform end to end: run it, generate data,
exercise the four AI workflows, inspect citations, and (optionally) connect Azure and Copilot
Studio. Plan for **60-90 minutes**.

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.**

## Learning objectives

By the end you will be able to:

1. Run the full platform locally with the offline mock providers.
2. Generate deterministic synthetic cases and understand the indexing report.
3. Call the four AI workflows and read the citation-first, confidence-scored responses.
4. Explain the config-driven local-vs-Azure provider selection.
5. Understand how APIM and Copilot Studio consume the same API.

## Prerequisites

See [deployment.md](deployment.md) §1. Minimum: .NET 10 SDK. Azure CLI + a subscription only
for the optional Azure module.

---

## Module 1 — Run it (10 min)

```powershell
$env:ASPIRE_ALLOW_UNSECURED_TRANSPORT = "true"
dotnet run --project src/CCDA.AppHost/CCDA.AppHost.csproj
```

Open the Aspire dashboard from the console output. Find the `ccda-api` and `ccda-web`
resources and the OpenTelemetry traces.

✅ **Checkpoint:** dashboard loads; both resources are healthy.

---

## Module 2 — Generate a case (15 min)

Seed a reproducible case:

```powershell
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- generate-case --seed 42
```

Read the report: **cases / documents / pages / chunks indexed / retrieval check / index
status / case IDs**.

**Try it:**
- Re-run with the same seed → identical output (determinism).
- `generate-cases --count 3 --complexity High`.
- `generate-investigation --seed 7` and `generate-inmate-history --seed 7`.
- `--output cases.json` to capture the payload.

✅ **Checkpoint:** you can explain what each report number means and why the seed matters.

---

## Module 3 — The four AI workflows (20 min)

With the API running (http://localhost:5144), call each workflow for your seeded case. Replace
`<caseId>` with an ID from the generation report (or `GET /api/v1/cases`).

```powershell
$api = "http://localhost:5144/api/v1/cases"
curl -X POST $api/summarize -H "Content-Type: application/json" -d '{ "caseId": "<caseId>" }'
curl -X POST $api/timeline  -H "Content-Type: application/json" -d '{ "caseId": "<caseId>" }'
curl -X POST $api/briefing  -H "Content-Type: application/json" -d '{ "caseId": "<caseId>" }'
curl -X POST $api/chat -H "Content-Type: application/json" `
  -d '{ "caseId": "<caseId>", "question": "Who are the parties involved?" }'
```

For each response, locate:
- the `citations[]` array (`sourceFile`, `pageNumber`, `snippet`),
- the `confidence` (`score` + `level`: High/Medium/Low),
- workflow-specific fields (key points, timeline events, findings/risks, follow-ups).

**Try it:** ask the chat endpoint something the documents can't answer and observe the
grounding guardrail + lower confidence.

✅ **Checkpoint:** every answer you got is traceable to a document + page.

---

## Module 4 — Provider selection (10 min)

Open `src/CCDA.AI/Options/FoundryOptions.cs`, `src/CCDA.Search/Options/SearchOptions.cs`, and
the DI extensions. Map the config keys:

| Config | Local default | Azure |
| --- | --- | --- |
| `Azure:Search:Endpoint` | empty → in-memory | set → Azure AI Search |
| `Ccda:Embeddings:Provider` | `Local` | `AzureOpenAI` |
| `Azure:Foundry:Provider` | `Local` | `AzureOpenAI` |

✅ **Checkpoint:** you can name which class serves each pillar in each mode.

---

## Module 5 (optional) — Go to Azure (20 min)

Follow [deployment.md](deployment.md) §4: validate `infra/main.bicep`, deploy to a resource
group, then map outputs to config and flip the provider switches. Re-run Module 3 and confirm
the workflows now use Azure AI Search + Azure OpenAI (watch the Application Insights traces).

✅ **Checkpoint:** the same requests now run on Azure with no app code changes.

---

## Module 6 (optional) — Copilot Studio (15 min)

Import the custom connector and rebuild the topics from
[docs/copilot-studio/README.md](copilot-studio/README.md). Point the connector at your APIM
gateway and test "summarize case", "build a timeline", and "ask about a case" in the test
pane.

✅ **Checkpoint:** a conversational agent returns the same cited answers as the web app.

---

## Stretch goals

- Add a fifth workflow (e.g., "compare two cases") in `CCDA.AI` and expose it in `CCDA.Api`.
- Add an evaluation gate using `generate-rag-eval` output and assert retrieval precision.
- Extend the web app citations panel to open the exact source page.

## Where to go next

- [Architecture](architecture.md) · [Deployment](deployment.md) · [Demo script](demo.md)
- [Infrastructure](../infra/README.md) · [Copilot Studio](copilot-studio/README.md)
