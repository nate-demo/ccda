# `.github` — automation & Copilot assets

## Continuous integration

[`workflows/ci.yml`](workflows/ci.yml) runs on pushes and pull requests to `main` (and on
demand). It has three jobs:

| Job | What it does |
| --- | --- |
| **Build & test** | Restores, builds (`Release`), and tests the `CCDA.slnx` solution on .NET 10 (pinned via `global.json`); uploads the test `.trx`. |
| **Generator smoke** | Runs the synthetic generator at **seed 42** and asserts the output matches the committed `sample-data/case-seed42-report.json` (determinism gate). |
| **Bicep lint** | `az bicep build` over `infra/main.bicep` to validate the infrastructure templates. |

Security/efficiency practices applied: least-privilege `permissions: contents: read`,
`concurrency` with cancel-in-progress, per-job `timeout-minutes`, NuGet caching, shallow
checkout, and **all third-party actions pinned to immutable commit SHAs** with version
comments.

## Copilot Synthetic Case Generator

- [`agents/ccda-synthetic-case-generator.agent.md`](agents/ccda-synthetic-case-generator.agent.md)
  — a custom GitHub Copilot agent that turns natural-language requests into runs of the
  `CCDA.DataGen.Cli` generator, then reports the citation-ready indexing result.
- [`prompts/`](prompts) — reusable prompt files for each slash-command:
  `/generate-case`, `/generate-cases`, `/generate-investigation`, `/generate-inmate-history`,
  `/generate-rag-eval`.

All generated data is fictional — **FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA**.
