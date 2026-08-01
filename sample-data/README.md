# Sample data

Committed, **deterministic** synthetic artifacts for the CCDA demo. Everything here is produced
from a fixed generator seed, so it is fully reproducible and safe to commit.

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.** No real persons, cases, or
> records are represented.

## Files

| File | What it is |
| --- | --- |
| `case-seed42-report.json` | Generation report for the canonical demo case (**seed 42**): case ID `CCDA-2024-761250`, 6 documents, 20 pages, 20 indexed chunks, retrieval gate **Passed**. |
| `rag-eval-seed42.json` | A 4-question RAG evaluation set targeting that case, each with the expected source file — used to validate retrieval quality. |

> Note: `case-seed42-report.json` includes a wall-clock `GeneratedAt` field that changes per
> run; every other value (case ID, document/page/chunk counts) is seed-deterministic.

## Regenerate

The full document text is intentionally **not** committed (it is large and 100% reproducible
from the seed). Regenerate the canonical case — including all document pages — locally:

```powershell
# Full case, indexed into the local in-memory search index (prints the report)
dotnet run --project ../src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- generate-case --seed 42 --complexity Medium

# Re-create the committed artifacts exactly
dotnet run --project ../src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- `
  generate-case --seed 42 --complexity Medium --output case-seed42-report.json
dotnet run --project ../src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- `
  generate-rag-eval --seed 42 --count 1 --complexity Medium --output rag-eval-seed42.json
```

Or seed the running API with the same case:

```powershell
curl -X POST http://localhost:5144/api/v1/cases/generate-demo-data `
  -H "Content-Type: application/json" -d '{ "complexity": "Medium", "seed": 42 }'
```

Identical seed ⇒ identical case. See [`docs/demo.md`](../docs/demo.md) for the scripted
walkthrough that uses this case.
