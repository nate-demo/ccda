---
mode: agent
description: Generate and index a batch of fictional CCDA legal cases (bulk, deterministic).
---

# /generate-cases

Generate **multiple** fictional, watermarked legal cases in one run and index them all into
the local search index. Per-case seeds are derived from the base `--seed`, so the whole batch
is reproducible.

Run from the repository root:

```bash
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- \
  generate-cases --count ${input:count:5} --seed ${input:seed:42} --complexity ${input:complexity:Medium}
```

Optional: `--type <CaseType>`, `--pages <n>`, `--output report.json`.

Report the total cases/documents/pages/chunks indexed and the list of generated **case IDs**,
and confirm the **Retrieval check** and **Index status**. All output is
**FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA**.
