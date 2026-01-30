export interface CandidateProfile {
    name?: string;
    email?: string;
    skills: string[];
    yearsExperience?: number;
    qualificationSummary?: string;
}

export interface MatchResult {
    sourceName: string;
    candidate?: CandidateProfile;
    matchScore?: number;
    matchLevel?: string;
    isRecommended?: boolean;
    analysisSummary?: string;
    missingSkills?: string[];
    success: boolean;
    error?: string;
}

export interface AnalyzeResponse {
    results: MatchResult[];
    meta: {
        processedResumes: number;
        failedResumes: number;
    };
}
