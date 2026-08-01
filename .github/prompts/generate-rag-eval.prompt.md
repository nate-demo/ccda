---
mode: agent
description: Generate fictional CCDA cases plus a paired RAG evaluation set.
---

# /generate-rag-eval

Generate fictional, watermarked cases together with a **RAG evaluation set** — a list of
questions each paired with the **expected source file** that should ground the answer. Use
this to smoke-test retrieval quality and citation coverage.

Run from the repository root:

```bash
dotnet run --project src/CCDA.DataGen.Cli/CCDA.DataGen.Cli.csproj -- \
  generate-rag-eval --count ${input:count:1} --seed ${input:seed:42} --complexity ${input:complexity:Medium} \
  --output rag-eval.json
```

Optional: `--type <CaseType>`.

Report how many cases and evaluation questions were produced, and show a few
`question -> expected source file` pairs. The committed reference set lives at
`sample-data/rag-eval-seed42.json`. All output is
**FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA**.
