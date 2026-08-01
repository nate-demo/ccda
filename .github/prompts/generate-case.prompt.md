---
mode: agent
description: Generate and index a single fictional CCDA legal case (deterministic by seed).
---

# /generate-case

Generate **one** fictional, watermarked legal case for the CCDA AI Case Review demo, chunk
and index it into the local search index, validate retrieval, and report the result.

Run from the repository root:

```bash
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- \
  generate-case --seed ${input:seed:42} --complexity ${input:complexity:Medium}
```

Optional: `--type <Fraud|Burglary|OrganizedCrime|FinancialCrime|Assault|Theft|DrugOffense|Homicide|Cybercrime|Other>`,
`--pages <n>`, `--output report.json`.

After running, report the generated **case ID**, the document/page/chunk counts, and confirm
the **Retrieval check** passed and **Index status** is `Success`. All output is
**FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA**.
