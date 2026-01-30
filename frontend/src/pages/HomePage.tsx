import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Sparkles, FileSearch, Zap, ArrowRight, Loader2 } from 'lucide-react';
import { FileDropzone } from '../components/FileDropzone';
import { ResultsDashboard } from '../components/ResultsDashboard';
import { analyzeResumes } from '../api';
import type { FileWithPreview, AnalyzeResponse, UploadStatus } from '../types';
import './HomePage.css';

export function HomePage() {
  const [files, setFiles] = useState<FileWithPreview[]>([]);
  const [jobDescription, setJobDescription] = useState('');
  const [status, setStatus] = useState<UploadStatus>('idle');
  const [error, setError] = useState<string | null>(null);
  const [results, setResults] = useState<AnalyzeResponse | null>(null);

  const isProcessing = status === 'processing' || status === 'uploading';
  const canSubmit = jobDescription.trim().length > 0 && files.length > 0 && !isProcessing;

  const handleSubmit = async () => {
    if (!canSubmit) return;

    setStatus('uploading');
    setError(null);

    try {
      setStatus('processing');
      const response = await analyzeResumes(jobDescription, files);
      setResults(response);
      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Analysis failed. Please try again.');
      setStatus('error');
    }
  };

  const handleReset = () => {
    setFiles([]);
    setJobDescription('');
    setStatus('idle');
    setError(null);
    setResults(null);
  };

  // Show results dashboard if analysis is complete
  if (results && status === 'success') {
    return (
      <div className="page">
        <div className="container">
          <ResultsDashboard results={results} onReset={handleReset} />
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      {/* Hero Section */}
      <section className="hero">
        <div className="container">
          <motion.div
            className="hero__content"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
          >
            <div className="hero__badge">
              <Sparkles size={14} />
              <span>AI-Powered Resume Analysis</span>
            </div>
            <h1 className="hero__title">
              Find the <span className="gradient-text">Perfect Candidate</span> in Seconds
            </h1>
            <p className="hero__subtitle">
              Upload resumes and a job description. Our AI analyzes candidates instantly,
              scoring them on skills, experience, and fit — so you can hire smarter, faster.
            </p>
          </motion.div>

          {/* Features */}
          <motion.div
            className="features"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.2 }}
          >
            <div className="feature">
              <div className="feature__icon">
                <FileSearch size={20} />
              </div>
              <div className="feature__text">
                <strong>Smart Extraction</strong>
                <span>PDF, DOCX, images & ZIPs</span>
              </div>
            </div>
            <div className="feature">
              <div className="feature__icon">
                <Zap size={20} />
              </div>
              <div className="feature__text">
                <strong>Instant Analysis</strong>
                <span>Skills, experience & fit scoring</span>
              </div>
            </div>
            <div className="feature">
              <div className="feature__icon">
                <Sparkles size={20} />
              </div>
              <div className="feature__text">
                <strong>AI Recommendations</strong>
                <span>Clear hire/no-hire suggestions</span>
              </div>
            </div>
          </motion.div>
        </div>
      </section>

      {/* Main Form */}
      <section className="upload-section">
        <div className="container">
          <motion.div
            className="upload-card glass"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.3 }}
          >
            {/* Job Description */}
            <div className="form-group">
              <label htmlFor="job-description" className="form-label">
                Job Description
              </label>
              <textarea
                id="job-description"
                className="textarea"
                placeholder="Paste the job description here. Include required skills, experience level, and any specific qualifications..."
                value={jobDescription}
                onChange={(e) => setJobDescription(e.target.value)}
                disabled={status === 'processing'}
              />
              <span className="form-hint">
                Be specific about requirements for more accurate matching
              </span>
            </div>

            {/* File Upload */}
            <div className="form-group">
              <label className="form-label">Resumes</label>
              <FileDropzone
                files={files}
                onFilesChange={setFiles}
                maxFiles={10}
                disabled={status === 'processing'}
              />
            </div>

            {/* Error Message */}
            <AnimatePresence>
              {error && (
                <motion.div
                  className="error-message"
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  exit={{ opacity: 0, height: 0 }}
                >
                  {error}
                </motion.div>
              )}
            </AnimatePresence>

            {/* Submit Button */}
            <button
              className="btn btn-primary btn-lg"
              onClick={handleSubmit}
              disabled={!canSubmit || isProcessing}
            >
              {isProcessing ? (
                <>
                  <Loader2 size={20} className="spin" />
                  Analyzing Resumes...
                </>
              ) : (
                <>
                  Analyze Candidates
                  <ArrowRight size={20} />
                </>
              )}
            </button>
          </motion.div>
        </div>
      </section>
    </div>
  );
}
