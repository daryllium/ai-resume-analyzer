import { useRef, useEffect } from 'react';
import { Card } from '../ui/Card';
import { cn } from '../../lib/utils';

interface JobInputProps {
    value: string;
    onChange: (value: string) => void;
}

export function JobInput({ value, onChange }: JobInputProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
            textareaRef.current.style.height = textareaRef.current.scrollHeight + 'px';
        }
    }, [value]);

    return (
        <div className="space-y-2">
            <label className="section-title">Job Description</label>
            <Card className="p-1 focus-within:ring-2 focus-within:ring-blue-500/20 transition-all">
                <textarea
                    ref={textareaRef}
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder="Paste requirements here..."
                    className="w-full bg-transparent border-0 p-3 text-sm text-[var(--text-primary)] placeholder:text-[var(--text-secondary)] resize-none min-h-[150px] focus:ring-0 leading-relaxed transition-all"
                />
            </Card>
            <div className="flex justify-end">
                <span className="text-[10px] text-[var(--text-secondary)] font-medium">
                    {value.length.toLocaleString()} characters
                </span>
            </div>
        </div>
    );
}
