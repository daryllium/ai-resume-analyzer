import { type MatchResult } from '../../types/api';
import { Card } from '../ui/Card';
import { Mail, Briefcase, FileText, AlertCircle, CheckCircle2 } from 'lucide-react';
import { cn } from '../../lib/utils';

interface CandidateDetailProps {
    result: MatchResult;
}

export function CandidateDetail({ result }: CandidateDetailProps) {
    const candidate = result.candidate;

    if (!result.success) return (
        <div className="h-full flex flex-col items-center justify-center text-[var(--text-secondary)]">
            <AlertCircle className="w-12 h-12 mb-4 text-red-500" />
            <h3 className="text-lg font-bold">Analysis Protocol Exception</h3>
            <p className="text-sm mt-2">{result.error}</p>
        </div>
    );

    const score = result.matchScore ?? 0;

    return (
        <div className="max-w-4xl mx-auto space-y-12">

            {/* Profile Header */}
            <header className="flex flex-col md:flex-row justify-between items-start gap-6 border-b border-[var(--border-subtle)] pb-10">
                <div className="space-y-4">
                    <div className="flex items-center gap-3">
                        <div className={cn(
                            "px-3 py-1 text-[11px] font-bold uppercase tracking-widest rounded-full",
                            result.isRecommended
                                ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400"
                                : "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-400"
                        )}>
                            {result.isRecommended ? 'Highly Recommended' : 'Standard Evaluation'}
                        </div>
                    </div>

                    <h1 className="text-5xl font-extrabold tracking-tight">
                        {candidate?.name || result.sourceName}
                    </h1>

                    <div className="flex flex-wrap items-center gap-6 text-sm text-[var(--text-secondary)] font-medium">
                        {candidate?.email && <span className="flex items-center gap-2"><Mail className="w-4 h-4" /> {candidate.email}</span>}
                        {candidate?.yearsExperience !== undefined && <span className="flex items-center gap-2 pl-6 border-l border-[var(--border-subtle)]"><Briefcase className="w-4 h-4" /> {candidate.yearsExperience} Years Exp</span>}
                    </div>
                </div>

                <div className="p-6 bg-[var(--bg-sidebar)] rounded-2xl border border-[var(--border-subtle)] text-center min-w-[140px]">
                    <div className="text-[11px] font-bold text-[var(--text-secondary)] uppercase tracking-widest mb-1">Match Score</div>
                    <div className={cn(
                        "text-6xl font-black leading-none",
                        score >= 80 ? "text-blue-600 dark:text-blue-400" : "text-[var(--text-primary)]"
                    )}>
                        {score}%
                    </div>
                </div>
            </header>

            {/* Report Sections */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-12">
                <div className="lg:col-span-8 space-y-10">

                    {/* Analysis */}
                    <section className="space-y-4">
                        <h3 className="section-title">Analysis Summary</h3>
                        <p className="text-xl text-[var(--text-primary)] leading-relaxed font-light">
                            {result.analysisSummary}
                        </p>
                    </section>

                    {candidate?.qualificationSummary && (
                        <section className="space-y-4 bg-slate-50 dark:bg-slate-900/50 p-6 rounded-xl border border-[var(--border-subtle)]">
                            <h4 className="section-title">Key Highlights</h4>
                            <p className="text-[var(--text-primary)] text-sm leading-relaxed">
                                {candidate.qualificationSummary}
                            </p>
                        </section>
                    )}

                    {/* Core Strengths */}
                    <section className="grid grid-cols-2 gap-4 pt-10 border-t border-[var(--border-subtle)]">
                        <div className="space-y-2">
                            <span className="section-title">Status</span>
                            <p className="font-bold flex items-center gap-2">
                                <div className="w-2 h-2 rounded-full bg-emerald-500" />
                                Verified Extraction
                            </p>
                        </div>
                        <div className="space-y-2">
                            <span className="section-title">Ranking</span>
                            <p className="font-bold">{result.matchLevel || 'N/A'}</p>
                        </div>
                    </section>
                </div>

                {/* Sidebar: Skill Matrix */}
                <div className="lg:col-span-4 space-y-10">
                    <section className="space-y-4">
                        <h3 className="section-title">Detected Skills</h3>
                        <div className="flex flex-wrap gap-2">
                            {candidate?.skills.map((skill, i) => (
                                <span
                                    key={i}
                                    className="px-3 py-1.5 bg-[var(--bg-main)] border border-[var(--border-subtle)] rounded-lg text-xs font-semibold hover:border-blue-500 transition-colors"
                                >
                                    {skill}
                                </span>
                            ))}
                        </div>
                    </section>

                    {result.missingSkills && result.missingSkills.length > 0 && (
                        <section className="space-y-4">
                            <h3 className="section-title text-red-500">Gap Detection</h3>
                            <div className="space-y-3">
                                {result.missingSkills.map((skill, i) => (
                                    <div key={i} className="flex items-start gap-2">
                                        <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-red-400 shrink-0" />
                                        <span className="text-sm text-[var(--text-secondary)] font-medium leading-tight">{skill}</span>
                                    </div>
                                ))}
                            </div>
                        </section>
                    )}

                </div>
            </div>
        </div>
    );
}
