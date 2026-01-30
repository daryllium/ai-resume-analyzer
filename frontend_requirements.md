# Frontend Specification: AI Resume Analyzer

## 1. Product Vision
Create a high-utility, professional SaaS dashboard for recruiters to analyze how well a batch of resumes (PDF/DOCX/TXT/ZIP) matches a specific Job Description. The interface should prioritize **clarity, speed, and decision-ready data**.

**Aesthetic Goal:** "Sophisticated Professionalism." Think high-end productivity tools like Linear, Framer, or Stripe. High contrast, clean typography, and a distraction-free environment.

---

## 2. Core Functional Requirements

### A. Configuration Sidebar
*   **Job Context:** A robust, auto-expanding text area for pasting detailed job descriptions.
*   **Source Upload:** A dedicated drag-and-drop zone supporting batch uploads.
    *   *Formats:* .pdf, .docx, .txt, .zip.
    *   *Feedback:* Real-time list of staged files with "Remove" functionality.

### B. Analytical Leaderboard
*   **Rankings:** A vertical list of candidates sorted by match score.
*   **Pillars of Data:** Each list item must clearly show:
    *   Match Score (0-100%) with color-coded sentiment.
    *   Candidate Name.
    *   High-level metrics (Years of Experience, Total Skills detected).

### C. Evaluation Canvas (Detail View)
*   **The Intelligence Report:** The main area for the selected candidate.
    *   **Hero Section:** Candidate name, contact info, and the massive Match Score.
    *   **AI Verdict:** A natural language summary explaining the reasoning behind the match.
    *   **The Gap Analysis:** A clear, visual breakdown of missing skills or experience deficiencies.
    *   **Skill Matrix:** A cloud or grid of detected skills, allowing for quick scanning.
*   **Action Bar:** Access to PDF Export for the current report.

---

## 3. Technical Stack & Constraints

*   **Core:** React 19 + Vite (for lightning-fast HMR).
*   **CSS:** Tailwind CSS v4 (Modern, utility-first).
*   **Icons:** Lucide-React (Consistent, geometric icon set).
*   **Animations:** Framer Motion (Subtle state transitions; no gimmicks).
*   **API Protocol:**
    *   `POST /api/analyze`: Multi-part form-data (`JobDescription` + `UploadFiles`).
    *   `POST /api/export/pdf`: JSON body (the analysis results) returns a PDF Blob.

---

## 4. UI/UX Guidelines

*   **Typography:** Primary use of high-legibility sans-serif (e.g., *Inter* or *Inter Display*). Mono fonts for metadata and technical metrics.
*   **Color System:**
    *   **Foundation:** Neutral Slate/Gray/White (High contrast).
    *   **Semantic Accents:** 
        *   `Success (85%+ score)`: Emerald
        *   `Primary (Match/Selection)`: Blue/Indigo
        *   `Gaps/Deficiencies`: Red/Amber
*   **Responsive Architecture:**
    *   **Desktop:** Master-Detail split view.
    *   **Mobile:** Hybrid navigation using tabs (Inputs vs. Results).

---

## 5. Data Architecture (API Contracts)

The frontend must map components to this backend interface:

```typescript
interface MatchResult {
  candidate: {
    name: string;
    email: string;
    skills: string[];
    yearsExperience: number;
    qualificationSummary: string;
  } | null;
  matchScore: number;       // Range: 0 - 100
  matchLevel: string;       // e.g. "STRONG_MATCH", "POTENTIAL_MATCH"
  analysisSummary: string;  // Detailed AI reasoning
  missingSkills: string[];
  isRecommended: boolean;
  success: boolean;         // Error handling at candidate level
  error?: string;           // Failure reason if extraction/matching failed
  sourceName: string;       // Original filename
}

interface AnalyzeResponse {
  results: MatchResult[];
  meta: {
    processedCount: number;
    durationMs: number;
  };
}
```

---

## 6. Interaction Model
1.  **Stage:** User pastes job description and drops files.
2.  **Process:** User clicks "Analyze." A clear "Processing" state (with meaningful feedback) appears.
3.  **Explore:** Results pop into the leaderboard. The first candidate is auto-selected.
4.  **Refine:** User can switch between candidates to view detailed dossier reports.
5.  **Export:** User clicks "Download PDF" to take the analysis offline.
