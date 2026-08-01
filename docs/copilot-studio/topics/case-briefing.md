# Topic — Case Briefing

Produces a grounded prosecutorial briefing (findings, risks, recommendations) via
`BuildCaseBriefing`.

## Trigger phrases
- "Brief me on {CaseId}"
- "Prepare a case briefing"
- "What are the risks in this case"
- "Give me recommendations for the case"

## Input variables
| Variable | Type | Source | Required |
| --- | --- | --- | --- |
| `CaseId` | Text | Entity from trigger, else **Question** node | Yes |

## Action
- **Connector operation:** `BuildCaseBriefing`
- **Request body:**
  ```json
  { "caseId": "{CaseId}" }
  ```

## Response rendering
Render the briefing sections in order, each as a labeled list, then the sources and confidence.

Message template:

> **Briefing — {CaseId}**
> {executiveSummary}
>
> **Key findings**
> - {keyFindings}
>
> **Risks**
> - {risks}
>
> **Recommendations**
> - {recommendations}
>
> **Sources**
> - {citations.reference}
>
> _Confidence: {confidence.level}. This briefing supports — and does not replace — attorney judgment._

## Error handling
- **404** → not-found message.
- **429 / 5xx** → generic retry message.

> **Reviewer reminder:** recommendations are AI-generated decision support. A licensed
> attorney must review before any action.
