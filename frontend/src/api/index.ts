import type { AnalyzeResponse } from '../types';

const API_BASE = '/api';

export async function analyzeResumes(
  jobDescription: string,
  files: File[],
  textResumes: string[] = []
): Promise<AnalyzeResponse> {
  const formData = new FormData();
  formData.append('JobDescription', jobDescription);

  files.forEach((file) => {
    formData.append('UploadFiles', file);
  });

  textResumes.forEach((text) => {
    formData.append('UploadText', text);
  });

  const response = await fetch(`${API_BASE}/analyze`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.error || `Analysis failed: ${response.statusText}`);
  }

  return response.json();
}

export async function exportToPdf(results: AnalyzeResponse): Promise<Blob> {
  const response = await fetch(`${API_BASE}/export/pdf`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(results),
  });

  if (!response.ok) {
    throw new Error('PDF export failed');
  }

  return response.blob();
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
