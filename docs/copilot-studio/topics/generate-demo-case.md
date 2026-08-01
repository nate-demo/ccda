# Topic — Generate Demo Case

Creates, indexes, and validates a **synthetic** demo case via `GenerateDemoData`, so a
facilitator can produce fresh (but reproducible) material on demand. All output is fictional
and watermarked.

## Trigger phrases
- "Generate a demo case"
- "Create a sample case"
- "Make me a new case for the demo"
- "Generate demo data"

## Input variables
| Variable | Type | Source | Required | Notes |
| --- | --- | --- | --- | --- |
| `CaseType` | Choice | **Question** node | No | Fraud, Burglary, OrganizedCrime, FinancialCrime, Assault, Theft, DrugOffense, Homicide, Cybercrime, Other. Omit to pick from the seed. |
| `Complexity` | Choice | **Question** node | No | Low, Medium, High. Default Medium. |
| `Pages` | Number | **Question** node | No | Target document pages. Omit to let complexity decide. |
| `Seed` | Number | **Question** node | No | Deterministic seed — same seed reproduces the same case. |

Keep all inputs optional so "generate a demo case" works with zero prompts.

## Action
- **Connector operation:** `GenerateDemoData`
- **Request body (all fields optional):**
  ```json
  {
    "caseType": "{CaseType}",
    "complexity": "{Complexity}",
    "pages": {Pages},
    "seed": {Seed}
  }
  ```

## Response rendering
> **Demo case created: {caseId}**
> - Documents: {documentsGenerated}
> - Pages: {pagesGenerated}
> - Chunks indexed: {chunksIndexed}
> - Index status: {indexStatus}
>
> This is **fictional demonstration data**. You can now try "summarize {caseId}",
> "timeline for {caseId}", or "brief me on {caseId}".

Offer the follow-up actions as quick replies that pass `{caseId}` straight into the
Summary / Timeline / Briefing topics.

## Error handling
- **400** (e.g. `Pages < 1`) → *"Please give a page count of at least 1, or leave it blank."*
- **429 / 5xx** → generic retry message.

> **Safety:** the generator only uses synthetic name/place pools and stamps every document
> with "FOR DEMONSTRATION PURPOSES ONLY / FICTIONAL CASE DATA". No real persons or records.
