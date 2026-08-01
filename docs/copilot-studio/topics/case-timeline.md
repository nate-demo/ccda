# Topic — Case Timeline

Extracts a grounded, citation-backed chronological timeline by calling `BuildCaseTimeline`.

## Trigger phrases
- "Build a timeline for {CaseId}"
- "What happened when"
- "Show me the sequence of events"
- "Case chronology"

## Input variables
| Variable | Type | Source | Required |
| --- | --- | --- | --- |
| `CaseId` | Text | Entity from trigger, else **Question** node | Yes |

## Action
- **Connector operation:** `BuildCaseTimeline`
- **Request body:**
  ```json
  { "caseId": "{CaseId}" }
  ```

## Response rendering
Iterate `events[]`; for each event show its `dateText` and `title`, followed by that event's
own `citations[]`. Close with the overall `confidence.level`.

Message template:

> **Timeline for {CaseId}**
>
> For each event:
> - **{events.dateText}** — {events.title}
>   Sources: {events.citations.reference}
>
> _Confidence: {confidence.level}. AI assists attorney review; verify against source documents._

> **Note:** each event's `title` carries the full event sentence; `description` is intentionally
> empty. Do not expect a separate description field.

## Error handling
- **404** → not-found message with a prompt to list cases.
- **429 / 5xx** → generic retry message.
