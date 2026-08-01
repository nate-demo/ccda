# Topic — Ask About a Case

Answers a grounded legal question with page-level citations via `AskCase`. This is the agent's
general-purpose Q&A fallback topic.

## Trigger phrases
- "Ask about {CaseId}"
- "I have a question about the case"
- Any unrecognized legal question (set as the **fallback** topic)

## Input variables
| Variable | Type | Source | Required |
| --- | --- | --- | --- |
| `Question` | Text | Trigger utterance, else **Question** node: *"What would you like to know?"* | Yes |
| `CaseId` | Text | Entity from trigger or prior turn (optional — omit to search across all cases) | No |

## Action
- **Connector operation:** `AskCase`
- **Request body:**
  ```json
  { "caseId": "{CaseId}", "question": "{Question}" }
  ```
  Omit `caseId` (or send empty) to answer across the whole indexed corpus.

## Response rendering
1. The `answer` text.
2. **Sources** — each `citations[]` `reference` + `snippet`.
3. **Confidence** — `confidence.level`; when Low, show `confidence.rationale` and encourage
   the user to consult the cited documents.
4. **Follow-ups** — render `followUpSuggestions[]` as quick-reply chips.

Message template:

> {answer}
>
> **Sources**
> - {citations.reference} — "{citations.snippet}"
>
> _Confidence: {confidence.level}. AI assists attorney review; it does not replace attorney judgment._
>
> You might also ask: {followUpSuggestions}

## Grounding guardrail
Never answer legal questions from the model's own knowledge. If the API returns no citations,
tell the user the corpus doesn't cover the question rather than speculating.

## Error handling
- **404** (unknown `CaseId`) → offer to list cases.
- **429 / 5xx** → generic retry message.
