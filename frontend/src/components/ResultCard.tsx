import { motion } from 'framer-motion';
import {
  Mail,
  Briefcase,
  CheckCircle,
  XCircle,
  AlertTriangle,
  ChevronDown,
  ChevronUp,
  Sparkles,
} from 'lucide-react';
import { useState } from 'react';
import type { AnalyzeResultItem } from '../types';
import './ResultCard.css';

interface ResultCardProps {
  result: AnalyzeResultItem;
  index: number;
}

function getScoreColor(score: number): string {
  if (score >= 85) return 'var(--color-score-excellent)';
  if (score >= 70) return 'var(--color-score-good)';
  if (score >= 55) return 'var(--color-score-medium)';
  return 'var(--color-score-low)';
}

function getScoreGradient(score: number): string {
  if (score >= 85) return 'var(--gradient-success)';
  if (score >= 70) return 'linear-gradient(135deg, #34d399 0%, #6ee7b7 100%)';
  if (score >= 55) return 'var(--gradient-warning)';
  return 'var(--gradient-danger)';
}

function getRecommendationBadge(result: AnalyzeResultItem) {
  if (!result.success) {
    return { icon: XCircle, text: 'Failed', className: 'badge-danger' };
  }
  if (result.isRecommended) {
    return { icon: CheckCircle, text: 'Recommended', className: 'badge-success' };
  }
  if ((result.matchScore ?? 0) >= 55) {
    return { icon: AlertTriangle, text: 'Maybe', className: 'badge-warning' };
  }
  return { icon: XCircle, text: 'Not Recommended', className: 'badge-danger' };
}

export function ResultCard({ result, index }: ResultCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const badge = getRecommendationBadge(result);
  const BadgeIcon = badge.icon;

  return (
    <motion.div
      className={`result-card ${!result.success ? 'result-card--error' : ''}`}
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.1, type: 'spring', stiffness: 300 }}
      layout
    >
      {/* Header */}
      <div className="result-card__header">
        <div className="result-card__rank">#{index + 1}</div>
        <div className="result-card__title">
          <h3 className="result-card__name">
            {result.candidate?.name || result.sourceName}
          </h3>
          {result.candidate?.email && (
            <span className="result-card__email">
              <Mail size={14} />
              {result.candidate.email}
            </span>
          )}
        </div>
        <div className={`badge ${badge.className}`}>
          <BadgeIcon size={12} />
          {badge.text}
        </div>
      </div>

      {/* Score Section */}
      {result.success && result.matchScore !== undefined && (
        <div className="result-card__score-section">
          <div className="result-card__score-ring">
            <svg viewBox="0 0 100 100">
              <circle
                className="score-ring__bg"
                cx="50"
                cy="50"
                r="45"
                fill="none"
                strokeWidth="8"
              />
              <motion.circle
                className="score-ring__fill"
                cx="50"
                cy="50"
                r="45"
                fill="none"
                strokeWidth="8"
                strokeLinecap="round"
                stroke={getScoreColor(result.matchScore)}
                strokeDasharray={`${2 * Math.PI * 45}`}
                initial={{ strokeDashoffset: 2 * Math.PI * 45 }}
                animate={{
                  strokeDashoffset:
                    2 * Math.PI * 45 * (1 - result.matchScore / 100),
                }}
                transition={{ duration: 1, delay: index * 0.1 + 0.3 }}
                style={{
                  transformOrigin: 'center',
                  transform: 'rotate(-90deg)',
                }}
              />
            </svg>
            <div className="score-ring__value">
              <motion.span
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: index * 0.1 + 0.5 }}
              >
                {result.matchScore}
              </motion.span>
              <small>%</small>
            </div>
          </div>
          <div className="result-card__match-info">
            <span
              className="result-card__match-level"
              style={{ background: getScoreGradient(result.matchScore) }}
            >
              <Sparkles size={14} />
              {result.matchLevel || 'Match Score'}
            </span>
            {result.candidate?.yearsExperience !== null && result.candidate?.yearsExperience !== undefined && (
              <span className="result-card__experience">
                <Briefcase size={14} />
                {result.candidate.yearsExperience} years experience
              </span>
            )}
          </div>
        </div>
      )}

      {/* Error State */}
      {!result.success && result.error && (
        <div className="result-card__error">
          <AlertTriangle size={16} />
          <span>{result.error}</span>
        </div>
      )}

      {/* Skills */}
      {result.candidate?.skills && result.candidate.skills.length > 0 && (
        <div className="result-card__skills">
          <h4>Skills</h4>
          <div className="skill-tags">
            {result.candidate.skills.slice(0, 6).map((skill, i) => (
              <span key={i} className="skill-tag">
                {skill}
              </span>
            ))}
            {result.candidate.skills.length > 6 && (
              <span className="skill-tag skill-tag--more">
                +{result.candidate.skills.length - 6} more
              </span>
            )}
          </div>
        </div>
      )}

      {/* Missing Skills */}
      {result.missingSkills && result.missingSkills.length > 0 && (
        <div className="result-card__missing">
          <h4>Skill Gaps</h4>
          <div className="skill-tags skill-tags--missing">
            {result.missingSkills.map((skill, i) => (
              <span key={i} className="skill-tag skill-tag--missing">
                {skill}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Expandable Analysis */}
      {result.analysisSummary && (
        <div className="result-card__analysis">
          <button
            className="result-card__expand-btn"
            onClick={() => setIsExpanded(!isExpanded)}
          >
            <span>AI Analysis</span>
            {isExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
          </button>
          <motion.div
            className="result-card__analysis-content"
            initial={false}
            animate={{
              height: isExpanded ? 'auto' : 0,
              opacity: isExpanded ? 1 : 0,
            }}
            transition={{ duration: 0.25 }}
          >
            <p>{result.analysisSummary}</p>
          </motion.div>
        </div>
      )}
    </motion.div>
  );
}
