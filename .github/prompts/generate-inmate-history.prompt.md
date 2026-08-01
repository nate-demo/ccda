---
mode: agent
description: Generate a fictional CCDA case with a custody / inmate-history package.
---

# /generate-inmate-history

Generate one fictional, watermarked case **plus a custody / inmate-history package** — intake
record, facility transfers, disciplinary actions, and program participation — and index
everything into the local search index.

Run from the repository root:

```bash
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- \
  generate-inmate-history --seed ${input:seed:42} --complexity ${input:complexity:Medium}
```

Optional: `--type <CaseType>`, `--pages <n>`, `--output report.json`.

Report the generated **case ID**, the custody documents produced, the chunk count, and confirm
the **Retrieval check** and **Index status**. All output is
**FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA**.
