// API Types matching the backend contracts

export interface CandidateProfile {
  name: string | null;
  email: string | null;
  skills: string[];
  yearsExperience: number | null;
  education: string[];
  certifications: string[];
  summary: string | null;
}

export interface AnalyzeResultItem {
  sourceName: string;
  success: boolean;
  candidate?: CandidateProfile;
  matchScore?: number;
  matchLevel?: string;
  missingSkills?: string[];
  isRecommended?: boolean;
  analysisSummary?: string;
  error?: string;
}

export interface AnalyzeMeta {
  processedResumes: number;
  failedResumes: number;
}

export interface AnalyzeResponse {
  results: AnalyzeResultItem[];
  meta: AnalyzeMeta;
}

// UI State Types
export type UploadStatus = 'idle' | 'uploading' | 'processing' | 'success' | 'error';

export interface FileWithPreview extends File {
  preview?: string;
  id: string;
}

export interface UploadState {
  files: FileWithPreview[];
  jobDescription: string;
  status: UploadStatus;
  progress: number;
  error: string | null;
}
