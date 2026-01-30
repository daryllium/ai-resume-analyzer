import { motion } from 'framer-motion';
import { Download, BarChart3, CheckCircle, AlertTriangle, XCircle } from 'lucide-react';
import type { AnalyzeResponse } from '../types';
import { ResultCard } from './ResultCard';
import { exportToPdf, downloadBlob } from '../api';
import './ResultsDashboard.css';
import { useState } from 'react';

interface ResultsDashboardProps {
  results: AnalyzeResponse;
  onReset: () => void;
}

export function ResultsDashboard({ results, onReset }: ResultsDashboardProps) {
  const [isExporting, setIsExporting] = useState(false);

  const sortedResults = [...results.results].sort(
    (a, b) => (b.matchScore ?? 0) - (a.matchScore ?? 0)
  );

  const recommended = results.results.filter((r) => r.isRecommended).length;
  const maybe = results.results.filter(
    (r) => r.success && !r.isRecommended && (r.matchScore ?? 0) >= 55
  ).length;
  const notRecommended = results.results.filter(
    (r) => r.success && (r.matchScore ?? 0) < 55
  ).length;

  const averageScore =
    results.results.filter((r) => r.success && r.matchScore !== undefined).length > 0
      ? Math.round(
          results.results
            .filter((r) => r.success && r.matchScore !== undefined)
            .reduce((sum, r) => sum + (r.matchScore ?? 0), 0) /
            results.results.filter((r) => r.success && r.matchScore !== undefined).length
        )
      : 0;

  const handleExport = async () => {
    setIsExporting(true);
    try {
      const blob = await exportToPdf(results);
      downloadBlob(blob, `Resume-Analysis-${new Date().toISOString().split('T')[0]}.pdf`);
    } catch (error) {
      console.error('Export failed:', error);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <motion.div
      className="results-dashboard"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.5 }}
    >
      {/* Header */}
      <div className="dashboard__header">
        <div className="dashboard__title">
          <h2>Analysis Complete</h2>
          <p>
            {results.meta.processedResumes} resume{results.meta.processedResumes !== 1 ? 's' : ''} analyzed
            {results.meta.failedResumes > 0 && ` • ${results.meta.failedResumes} failed`}
          </p>
        </div>
        <div className="dashboard__actions">
          <button className="btn btn-secondary" onClick={onReset}>
            Analyze More
          </button>
          <button
            className="btn btn-primary"
            onClick={handleExport}
            disabled={isExporting}
          >
            {isExporting ? (
              <>
                <span className="spinner" />
                Exporting...
              </>
            ) : (
              <>
                <Download size={18} />
                Export PDF
              </>
            )}
          </button>
        </div>
      </div>

      {/* Stats Grid */}
      <motion.div
        className="stats-grid"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.1 }}
      >
        <div className="stat-card stat-card--primary">
          <div className="stat-card__icon">
            <BarChart3 size={20} />
          </div>
          <div className="stat-card__content">
            <span className="stat-card__value">{averageScore}%</span>
            <span className="stat-card__label">Average Score</span>
          </div>
        </div>

        <div className="stat-card stat-card--success">
          <div className="stat-card__icon">
            <CheckCircle size={20} />
          </div>
          <div className="stat-card__content">
            <span className="stat-card__value">{recommended}</span>
            <span className="stat-card__label">Recommended</span>
          </div>
        </div>

        <div className="stat-card stat-card--warning">
          <div className="stat-card__icon">
            <AlertTriangle size={20} />
          </div>
          <div className="stat-card__content">
            <span className="stat-card__value">{maybe}</span>
            <span className="stat-card__label">Maybe</span>
          </div>
        </div>

        <div className="stat-card stat-card--danger">
          <div className="stat-card__icon">
            <XCircle size={20} />
          </div>
          <div className="stat-card__content">
            <span className="stat-card__value">{notRecommended}</span>
            <span className="stat-card__label">Not Recommended</span>
          </div>
        </div>
      </motion.div>

      {/* Results List */}
      <div className="results-list">
        {sortedResults.map((result, index) => (
          <ResultCard key={result.sourceName + index} result={result} index={index} />
        ))}
      </div>
    </motion.div>
  );
}
