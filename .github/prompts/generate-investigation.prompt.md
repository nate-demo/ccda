---
mode: agent
description: Generate a fictional CCDA case with a full investigation package.
---

# /generate-investigation

Generate one fictional, watermarked case **plus an investigation package** — detective
reports, an evidence log, and witness interviews — and index everything into the local search
index.

Run from the repository root:

```bash
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- \
  generate-investigation --seed ${input:seed:42} --complexity ${input:complexity:High}
```

Optional: `--type <CaseType>`, `--pages <n>`, `--output report.json`.

Report the generated **case ID**, the investigation documents produced, the chunk count, and
confirm the **Retrieval check** and **Index status**. All output is
**FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA**.
