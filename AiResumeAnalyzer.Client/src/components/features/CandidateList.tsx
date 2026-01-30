import { type MatchResult } from '../../types/api';
import { cn } from '../../lib/utils';
import { motion } from 'framer-motion';

interface CandidateListProps {
    candidates: MatchResult[];
    selectedIndex: number;
    onSelect: (index: number) => void;
}

export function CandidateList({ candidates, selectedIndex, onSelect }: CandidateListProps) {
    return (
        <div className="space-y-1 h-full overflow-y-auto pr-2">
            {candidates.map((result, idx) => {
                const score = result.matchScore ?? 0;
                const isSelected = selectedIndex === idx;

                const scoreClass =
                    score >= 85 ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400" :
                        score >= 70 ? "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400" :
                            score >= 55 ? "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400" :
                                "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-400";

                return (
                    <div
                        key={idx}
                        onClick={() => onSelect(idx)}
                        className={cn(
                            "group relative flex items-center gap-4 p-3 rounded-xl cursor-pointer transition-all duration-200 border",
                            isSelected
                                ? "bg-blue-50/50 border-blue-500 shadow-sm dark:bg-blue-900/10 dark:border-blue-500/50"
                                : "bg-transparent border-transparent hover:bg-slate-50 dark:hover:bg-slate-800/50"
                        )}
                    >
                        {/* Score Pill */}
                        <div className={cn(
                            "flex items-center justify-center w-12 h-8 rounded-lg text-sm font-bold tracking-tight",
                            scoreClass
                        )}>
                            {score}%
                        </div>

                        {/* Info */}
                        <div className="flex-1 min-w-0 flex flex-col">
                            <span className={cn(
                                "font-semibold text-sm truncate",
                                isSelected ? "text-blue-600 dark:text-blue-400" : "text-[var(--text-primary)]"
                            )}>
                                {result.candidate?.name || result.sourceName}
                            </span>

                            <div className="flex items-center gap-2 text-[11px] text-[var(--text-secondary)] font-medium">
                                <span>{result.candidate?.yearsExperience ?? 0}yr exp</span>
                                <span className="w-1 h-1 bg-[var(--border-subtle)] rounded-full" />
                                <span>{result.candidate?.skills.length ?? 0} skills</span>
                            </div>
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
