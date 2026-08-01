# Topic — Case Summary

Produces a grounded, citation-backed summary of a single case by calling `SummarizeCase`.

## Trigger phrases
- "Summarize case {CaseId}"
- "What is this case about"
- "Give me a summary of the case"
- "Case overview"

## Input variables
| Variable | Type | Source | Required |
| --- | --- | --- | --- |
| `CaseId` | Text | Entity from trigger, else **Question** node: *"Which case? (e.g. CCDA-2024-761250)"* | Yes |

If `CaseId` is unknown, branch to the **Find a Case** topic (`ListCases`) to let the user pick.

## Action
- **Connector operation:** `SummarizeCase`
- **Request body:**
  ```json
  { "caseId": "{CaseId}" }
  ```

## Response rendering
Render, in order:
1. The `summary` text.
2. **Key points** — bullet each `keyPoints[]` item.
3. **Sources** — for each `citations[]` item, show `reference` (e.g. `police_report.pdf (p.3)`)
   and the `snippet`.
4. **Confidence** — show `confidence.level` (High/Medium/Low) and, when Low/Medium, the
   `confidence.rationale`.

Message template:

> **Summary of {CaseId}**
> {summary}
>
> **Key points**
> - {keyPoints}
>
> **Sources**
> - {citations.reference} — "{citations.snippet}"
>
> _Confidence: {confidence.level}. This assists attorney review and does not replace attorney judgment._

## Error handling
- **404** → *"I couldn't find case {CaseId}. Try 'list cases' to see what's available."*
- **429 / 5xx** → generic retry message.
