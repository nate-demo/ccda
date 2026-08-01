# CCDA Demo Script

A precise, copy-paste walkthrough: seed case **42**, then Summary → Timeline → Briefing → Ask,
highlighting **citations** and **confidence** at every step. Runs fully offline.

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.**

## Setup

Start the API and web app (see [deployment.md](deployment.md) §2). This script uses the API
directly so it works from any terminal; the web app mirrors the same results visually.

```powershell
$api = "http://localhost:5144/api/v1/cases"
```

---

## Step 0 — Seed the case (reproducible)

```powershell
curl -X POST $api/generate-demo-data -H "Content-Type: application/json" `
  -d '{ "complexity": "Medium", "seed": 42 }'
```

**Point out:** the fictional watermark and the indexing report (documents, pages, chunks).
Grab a `caseId` from the response (or `GET $api`) and store it:

```powershell
$caseId = "<paste caseId here>"
```

**Say:** "Everything from here is grounded in the documents we just indexed."

---

## Step 1 — Summary

```powershell
curl -X POST $api/summarize -H "Content-Type: application/json" -d (@{ caseId = $caseId } | ConvertTo-Json)
```

**Show:**
- `summary` + `keyPoints[]` — the case in a paragraph and bullets.
- `citations[]` — each with `sourceFile` and `pageNumber`.
- `confidence.level` — High/Medium/Low.

**Say:** "Fast, but never unsourced — every claim points to a page."

---

## Step 2 — Timeline

```powershell
curl -X POST $api/timeline -H "Content-Type: application/json" -d (@{ caseId = $caseId } | ConvertTo-Json)
```

**Show:** `events[]` in chronological order; each event has `dateText`, a `title` (the event
sentence), and its own `citations[]`.

**Say:** "The narrative arc of the case — and I can prove where each moment comes from."

---

## Step 3 — Briefing

```powershell
curl -X POST $api/briefing -H "Content-Type: application/json" -d (@{ caseId = $caseId } | ConvertTo-Json)
```

**Show:** `executiveSummary`, `keyFindings[]`, `risks[]`, `recommendations[]`, all with
`citations[]` and an overall `confidence`.

**Say:** "A decision-ready briefing an attorney can act on — still fully cited."

---

## Step 4 — Ask about the case (grounded Q&A)

Answerable question:

```powershell
$body = @{ caseId = $caseId; question = "What evidence supports the primary charge?" } | ConvertTo-Json
curl -X POST $api/chat -H "Content-Type: application/json" -d $body
```

**Show:** `answer`, `citations[]`, `confidence`, and `followUpSuggestions[]`.

Now an **unanswerable** question to demonstrate the guardrail:

```powershell
$body = @{ caseId = $caseId; question = "What is the defendant's blood type?" } | ConvertTo-Json
curl -X POST $api/chat -H "Content-Type: application/json" -d $body
```

**Show:** the system declines / low `confidence` because the documents don't support it.

**Say:** "It won't invent an answer. No support in the record → it tells you so. That's how you
keep a human in control."

---

## Closing

- **Citations everywhere** → defensible AI.
- **Confidence signal** → honest uncertainty.
- **Fictional, watermarked data** → safe to demo anywhere.
- **Same API** powers the web app, APIM, and Copilot Studio.

## Reset / re-run

The in-memory store is per-process — restart the API to clear, then re-seed with the same
`seed 42` for an identical run. See [facilitator.md](facilitator.md) for the timed 15-minute
version and talking points.
