import { useCallback, useState } from 'react';
import { Upload, FileText, X, Archive } from 'lucide-react';
import { cn } from '../../lib/utils';
import { motion, AnimatePresence } from 'framer-motion';
import { Card } from '../ui/Card';

interface FileUploadProps {
    files: File[];
    onFilesSelected: (files: File[]) => void;
    onRemoveFile: (index: number) => void;
}

export function FileUpload({ files, onFilesSelected, onRemoveFile }: FileUploadProps) {
    const [isDragging, setIsDragging] = useState(false);

    const handleDrag = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        if (e.type === 'dragenter' || e.type === 'dragover') setIsDragging(true);
        else if (e.type === 'dragleave') setIsDragging(false);
    }, []);

    const handleDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        setIsDragging(false);
        if (e.dataTransfer.files?.length) onFilesSelected(Array.from(e.dataTransfer.files));
    }, [onFilesSelected]);

    return (
        <div className="space-y-4">
            <label className="section-title">Upload Resumes</label>

            <div
                onDragEnter={handleDrag}
                onDragLeave={handleDrag}
                onDragOver={handleDrag}
                onDrop={handleDrop}
                className="relative group h-32"
            >
                <input
                    type="file"
                    multiple
                    onChange={(e) => e.target.files && onFilesSelected(Array.from(e.target.files))}
                    className="absolute inset-0 w-full h-full opacity-0 z-10 cursor-pointer"
                    accept=".pdf,.docx,.txt,.zip"
                />

                <div className={cn(
                    "h-full rounded-xl border-2 border-dashed flex flex-col items-center justify-center transition-all duration-200 bg-[var(--bg-sidebar)]",
                    isDragging ? "border-blue-500 bg-blue-50/10" : "border-[var(--border-subtle)] group-hover:border-[var(--text-secondary)]"
                )}>
                    <Upload className={cn("w-6 h-6 mb-2", isDragging ? "text-blue-500" : "text-[var(--text-secondary)]")} />
                    <p className="text-sm font-medium">Click or drag to upload</p>
                    <p className="text-xs text-[var(--text-secondary)] mt-1">PDF, DOCX, TXT, ZIP up to 10MB</p>
                </div>
            </div>

            <div className="space-y-2 max-h-64 overflow-y-auto pr-1">
                <AnimatePresence>
                    {files.map((file, idx) => (
                        <motion.div
                            key={`${file.name}-${idx}`}
                            initial={{ opacity: 0, scale: 0.95 }}
                            animate={{ opacity: 1, scale: 1 }}
                            exit={{ opacity: 0, scale: 0.95 }}
                        >
                            <div className="flex items-center justify-between p-3 rounded-lg border border-[var(--border-subtle)] bg-[var(--bg-main)]">
                                <div className="flex items-center gap-3 min-w-0">
                                    <div className="p-2 rounded bg-[var(--bg-sidebar)] text-[var(--text-secondary)]">
                                        {file.name.endsWith('.zip') ? <Archive className="w-4 h-4" /> : <FileText className="w-4 h-4" />}
                                    </div>
                                    <div className="min-w-0">
                                        <p className="text-sm font-medium truncate leading-none mb-1">{file.name}</p>
                                        <p className="text-[10px] text-[var(--text-secondary)] font-mono">{(file.size / 1024).toFixed(0)} KB</p>
                                    </div>
                                </div>
                                <button onClick={() => onRemoveFile(idx)} className="p-1.5 text-[var(--text-secondary)] hover:text-red-500 transition-colors">
                                    <X className="w-4 h-4" />
                                </button>
                            </div>
                        </motion.div>
                    ))}
                </AnimatePresence>
            </div>
        </div>
    );
}
