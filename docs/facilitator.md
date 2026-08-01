# CCDA Facilitator Guide

Audience: presenters running an executive or technical briefing of the CCDA AI Case Review
Platform. Includes the business story and a tight **15-minute demo flow**.

> **FOR DEMONSTRATION PURPOSES ONLY — FICTIONAL CASE DATA.** No real persons, cases, or
> records are represented. AI assists attorney review; it never replaces attorney judgment.

---

## The story

The Contoso County District Attorney's office is drowning in documents. A single case can span
hundreds of pages — police reports, witness statements, lab results, prior-custody records.
Attorneys spend hours locating the one paragraph that matters, and every conclusion must be
defensible and traceable to a source.

**The platform's promise:** an attorney asks a plain-language question about a case and gets a
grounded answer in seconds — with a citation to the exact document and page, and an honest
confidence signal. Nothing is fabricated; if the documents don't support an answer, the system
says so.

**Why it matters to leadership**

| Value | How the platform delivers |
| --- | --- |
| Faster review | Summaries, timelines, and briefings generated from the full case file in seconds. |
| Defensible AI | Every response carries page-level citations + a confidence level. |
| Trust & safety | Grounding guardrail blocks ungrounded legal claims; all demo data is watermarked fictional. |
| No lock-in to a live tenant | Runs fully offline with mock providers; flips to Azure via config. |
| Governed access | APIM (auth, rate-limit, versioning) fronts a single API consumed by the web app **and** a Copilot Studio agent. |

---

## Three ways attorneys engage

1. **Web app** — upload, search, ask, and read briefings/timelines with source pages.
2. **Copilot Studio agent** — the same capabilities inside Teams/M365 chat via the custom
   connector through APIM.
3. **Synthetic generator** — instantly fabricate realistic (fictional) cases for training,
   testing, and demos.

---

## 15-minute demo flow

> Run locally first (see [deployment.md](deployment.md) §2). Have the API + Web running, or the
> Aspire dashboard open. Use **seed 42** so the case is reproducible.

| Time | Beat | What to show / say |
| --- | --- | --- |
| 0:00 | **Frame the problem** (1 min) | "Hundreds of pages per case; every answer must cite its source." |
| 1:00 | **Generate a case** (2 min) | Seed case 42 (`generate-demo-data`). Point out the fictional watermark and the indexing report (docs / pages / chunks). |
| 3:00 | **Summarize** (3 min) | Run Summary. Highlight the key points **and** that each is backed by `Citations[]` (file + page) with a confidence level. |
| 6:00 | **Timeline** (2 min) | Run Timeline. Show the chronological events, each with its own citation — "this is where the narrative comes from." |
| 8:00 | **Ask a question** (3 min) | Ask a natural-language question. Show the grounded answer, citations, confidence, and follow-up suggestions. Then ask something the docs can't answer to show the guardrail + a **Low** confidence / "not supported" response. |
| 11:00 | **Briefing** (2 min) | Run Briefing: executive summary, key findings, risks, recommendations — a decision-ready artifact, still fully cited. |
| 13:00 | **Governance & scale** (2 min) | Open the Aspire dashboard (telemetry) and mention APIM + Copilot Studio: same API, governed access, available in Teams. Close on trust: citations + confidence + fictional data. |

---

## Talking points & FAQs

- **"Does the AI decide the case?"** No. It surfaces and cites; the attorney decides. The
  confidence signal and citations exist precisely to keep a human in control.
- **"Where does the data come from?"** 100% synthetic and watermarked. Swap in real document
  stores by pointing the config at Azure AI Search — the app code doesn't change.
- **"What if the model hallucinates?"** Answers are grounded in retrieved chunks; the grounder
  refuses to answer legal questions without supporting context, and confidence drops to Low.
- **"Is this production-ready?"** It's a reference demo. Azure resources ship as validated
  Bicep; production would add data governance, evaluation gates, and human-review workflows.

---

## Pre-flight checklist

- [ ] `dotnet build CCDA.slnx` succeeds.
- [ ] API + Web (or AppHost) running; Aspire dashboard reachable.
- [ ] Seed case 42 generated and visible in the web app.
- [ ] One "answerable" and one "unanswerable" question prepared.
- [ ] Browser zoomed for readability; citations panel visible.

See the step-by-step [demo script](demo.md) for exact clicks and payloads.
