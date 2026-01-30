# Resume–Job Match SaaS (Local-first) — Architecture & Plan

## Goal (MVP)
A local-first web app where a user provides:
- A **job description** (freeform text)
- **Up to 10 resumes** (uploaded individually and/or as a **ZIP**)

The backend:
1. Extracts text from each resume (PDF/DOCX/TXT + images)
2. Runs **OCR** for images and scanned PDFs
3. Uses a local AI model (**llama3.2 via Ollama**) to:
   - Extract **structured resume data** per candidate (name, email, skills, years of experience, qualification summary)
   - Produce a **single match score (0–100)** + a robust analysis summary + recommend/not
4. Returns results “all at once” (one response)

**Constraints / decisions**
- Start local; later host on Render with cloud AI.
- **No accounts**
- **Delete data immediately** (no persistence)
- **Sequential processing** of resumes for reliability
- ZIP extraction should be **recursive**
- Ignore legacy `.doc` for now

---

## Suggested tech stack
### Backend
- **.NET 10 Minimal API**
- Ingestion pipeline:
  - PDF text extraction (text-based PDFs)
  - DOCX extraction
  - TXT passthrough
  - ZIP recursive extraction
- AI orchestration via **Ollama HTTP API**
- Output: strict JSON schemas (job, candidate, result)

### Frontend (bare minimum)
- Any minimal UI (later); for MVP you can even use:
  - A simple HTML form page, or
  - A thin React/Vite UI
- Inputs:
  - Job description textarea
  - Multi-file upload + optional zip upload
- Output:
  - JSON (raw) initially, then nicer rendering later

---

## Processing flow (sequential)
1. **Request received**: job description + files[] + optional zip file
2. **Expand**: if zip present, unpack recursively and add supported files
3. **Filter** to supported types (PDF, DOCX, TXT, PNG/JPG/etc.)
4. For each resume (sequential):
   - Extract text (PDF/DOCX/TXT)
   - If text extraction yields insufficient text OR file is an image → OCR → text
   - Normalize whitespace, remove obvious boilerplate if desired
5. **Parse job once** → `JobProfile`
6. For each resume:
   - Parse resume → `CandidateProfile`
   - Match → `MatchResult` with **single score 0–100**, summary, recommend
7. Return:
   - Job profile
   - List of results per candidate, sorted by score desc (optional)

---



## AI design
### Model
- Local: **llama3.2** via **Ollama**

### Core calls (recommended)
1. `JobParser` (internal): JD text → internal requirements representation (not returned to client)
2. `ResumeParser`: resume text → `CandidateProfile` (returned)
3. `Matcher`: JD (or internal requirements) + candidate profile → `MatchResult` (returned)

You can optionally merge (2) + (3) into a single call per resume to reduce latency/cost, but keeping them separate is easier to debug early.

### Output policy
- Enforce strict JSON output (no markdown)
- Validate JSON against schemas
- If validation fails: retry once with a “fix JSON” prompt

---

## Scoring (single score + thresholds)
### Output
- `score`: integer 0–100
- `recommend`: boolean
- `recommendationLevel`: one of `strong_yes | yes | maybe | no` (optional but useful even with a single score)
- `analysisSummary`: robust explanation (no evidence snippets)

### Example thresholds (tweak later)
- **85–100**: recommend = true, level = strong_yes
- **70–84**: recommend = true, level = yes
- **55–69**: recommend = false, level = maybe
- **0–54**: recommend = false, level = no

Keep thresholds in config.

---

## Data retention / security
- Store uploads only in a **temp directory** for processing.
- Delete temp files immediately after response (finally block).
- Do not log extracted resume text.
- Add size limits:
  - max files: 10
  - max zip size (e.g., 25–50MB initially)
  - max individual file size (e.g., 10MB)

---

## API contract (MVP)
### `POST /api/analyze` (multipart/form-data)
**Fields**
- `jobDescription`: string (required)
- `files[]`: 0..10 files (optional)
- `zipFile`: 0..1 file (optional)
- `resumeText[]`: optional list of freeform resume text entries

**Response (JSON)**
```json
{
  "results": [
    {
      "candidate": {
        "name": "Jane Doe",
        "email": "jane@example.com",
        "skills": ["C#", ".NET", "SQL"],
        "yearsExperience": 6,
        "qualificationSummary": "Concise summary of fit and background."
      },
      "score": 82,
      "recommend": true,
      "recommendationLevel": "yes",
      "analysisSummary": "Robust explanation of the score and overall fit."
    }
  ],
  "meta": {
    "processedResumes": 1,
    "failedResumes": 0
  }
}
```

### Optional: `POST /api/export/pdf`
- Input: the analysis JSON
- Output: PDF file
(Defer until after core analysis is stable.)

- Input: the analysis JSON
- Output: PDF file
(Defer until after core analysis is stable.)

---

## JSON schemas (starter)
### JobProfile (internal only; not returned to client in MVP)
You may still parse the JD into a structured internal shape to improve consistency, but the API response does **not** include it in MVP.

### CandidateProfile (MVP structured resume data)
```json
{
  "name": "string|null",
  "email": "string|null",
  "skills": ["string"],
  "yearsExperience": "number|null",
  "qualificationSummary": "string|null"
}
```

### MatchResult (single score)
```json
{
  "score": "integer 0-100",
  "recommend": "boolean",
  "recommendationLevel": "strong_yes|yes|maybe|no",
  "analysisSummary": "string"
}
```

---

## Milestone plan
### M0 — Repo + baseline API (COMPLETED)
- .NET 10 Minimal API project
- `POST /api/analyze` stub returning mock JSON

### M1 — Ingestion + extraction (COMPLETED)
- Accept multipart with JD + files[] + zipFile
- Recursive zip expansion
- Extract text from TXT/DOCX/PDF(text)
- Return extracted text lengths + filenames (debug response)

### M2 — AI: job parsing + resume parsing (COMPLETED)
- Ollama client
- `JobParser` prompt → JobProfile JSON
- `ResumeParser` prompt → CandidateProfile JSON
- Schema validation + retry-on-invalid-json

### M3 — AI: match scoring (single score) (COMPLETED)
- `Matcher` prompt → MatchResult JSON
- Apply thresholds config
- Response includes job + results

### M4 — Hardening (COMPLETED)
- File limits, timeouts, safe temp handling, no sensitive logs
- Better error reporting per resume (failed vs processed)

### M5 — Export (COMPLETED)
- Server-side PDF generation from result JSON

---

## Implementation notes (key classes / interfaces)
- `IFileExtractor` (PDF/DOCX/TXT)
- `IModelClient` (Ollama now, cloud later)
- `IJobParser`, `IResumeParser`, `IMatcher`
- `AnalysisOrchestrator` (end-to-end coordination)

Keep providers behind interfaces to swap:
- AI provider
- PDF generator

---

## Ollama integration (notes)
- Communicate over HTTP to the local Ollama endpoint
- Use a JSON-only response instruction and validate output
- Set max tokens and temperature low for structured outputs

---

## Open decisions (later)
- Hosting details on Render (storage, scaling)
- Async job queue for larger batches
- UI/UX improvements and comparison views
