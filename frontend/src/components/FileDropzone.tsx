import { useCallback, useState } from 'react';
import { useDropzone, type FileRejection } from 'react-dropzone';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Upload,
  FileText,
  FileImage,
  FileArchive,
  X,
  AlertCircle,
} from 'lucide-react';
import type { FileWithPreview } from '../types';
import './FileDropzone.css';

interface FileDropzoneProps {
  files: FileWithPreview[];
  onFilesChange: (files: FileWithPreview[]) => void;
  maxFiles?: number;
  disabled?: boolean;
}

const ACCEPTED_TYPES = {
  'application/pdf': ['.pdf'],
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
  'text/plain': ['.txt'],
  'image/png': ['.png'],
  'image/jpeg': ['.jpg', '.jpeg'],
  'application/zip': ['.zip'],
};

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

function getFileIcon(file: File) {
  const ext = file.name.split('.').pop()?.toLowerCase();
  if (ext === 'zip') return <FileArchive size={20} />;
  if (['png', 'jpg', 'jpeg'].includes(ext || '')) return <FileImage size={20} />;
  return <FileText size={20} />;
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function FileDropzone({
  files,
  onFilesChange,
  maxFiles = 10,
  disabled = false,
}: FileDropzoneProps) {
  const [rejectionErrors, setRejectionErrors] = useState<string[]>([]);

  const onDrop = useCallback(
    (acceptedFiles: File[], rejections: FileRejection[]) => {
      // Clear previous errors
      setRejectionErrors([]);

      // Handle rejections
      if (rejections.length > 0) {
        const errors = rejections.map((r) => {
          const errorMessages = r.errors.map((e) => e.message).join(', ');
          return `${r.file.name}: ${errorMessages}`;
        });
        setRejectionErrors(errors);
      }

      // Add accepted files with unique IDs
      const newFiles: FileWithPreview[] = acceptedFiles.map((file) => ({
        ...file,
        id: `${file.name}-${Date.now()}-${Math.random().toString(36).slice(2)}`,
        preview: file.type.startsWith('image/') ? URL.createObjectURL(file) : undefined,
      }));

      // Merge with existing, respecting max limit
      const combined = [...files, ...newFiles].slice(0, maxFiles);
      onFilesChange(combined);
    },
    [files, maxFiles, onFilesChange]
  );

  const removeFile = useCallback(
    (id: string) => {
      const file = files.find((f) => f.id === id);
      if (file?.preview) {
        URL.revokeObjectURL(file.preview);
      }
      onFilesChange(files.filter((f) => f.id !== id));
    },
    [files, onFilesChange]
  );

  const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
    onDrop,
    accept: ACCEPTED_TYPES,
    maxSize: MAX_FILE_SIZE,
    maxFiles: maxFiles - files.length,
    disabled: disabled || files.length >= maxFiles,
  });

  const dropzoneClasses = [
    'dropzone',
    isDragActive && 'dropzone--active',
    isDragReject && 'dropzone--reject',
    disabled && 'dropzone--disabled',
    files.length >= maxFiles && 'dropzone--full',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className="file-dropzone">
      <div {...getRootProps({ className: dropzoneClasses })}>
        <input {...getInputProps()} />
        <div className="dropzone__content">
          <motion.div
            className="dropzone__icon"
            animate={{
              scale: isDragActive ? 1.1 : 1,
              y: isDragActive ? -5 : 0,
            }}
            transition={{ type: 'spring', stiffness: 400 }}
          >
            <Upload size={32} />
          </motion.div>
          <div className="dropzone__text">
            {isDragActive ? (
              <p className="dropzone__primary">Drop your files here...</p>
            ) : files.length >= maxFiles ? (
              <p className="dropzone__primary">Maximum files reached</p>
            ) : (
              <>
                <p className="dropzone__primary">
                  Drag & drop resumes here, or <span>browse</span>
                </p>
                <p className="dropzone__secondary">
                  PDF, DOCX, TXT, PNG, JPG, or ZIP • Max {maxFiles} files • 10MB each
                </p>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Rejection Errors */}
      <AnimatePresence>
        {rejectionErrors.length > 0 && (
          <motion.div
            className="dropzone__errors"
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
          >
            {rejectionErrors.map((error, i) => (
              <div key={i} className="dropzone__error">
                <AlertCircle size={14} />
                <span>{error}</span>
              </div>
            ))}
          </motion.div>
        )}
      </AnimatePresence>

      {/* File List */}
      <AnimatePresence mode="popLayout">
        {files.length > 0 && (
          <motion.div
            className="file-list"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          >
            {files.map((file) => (
              <motion.div
                key={file.id}
                className="file-item"
                layout
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.8 }}
                transition={{ type: 'spring', stiffness: 500, damping: 30 }}
              >
                <div className="file-item__icon">{getFileIcon(file)}</div>
                <div className="file-item__info">
                  <span className="file-item__name">{file.name}</span>
                  <span className="file-item__size">{formatFileSize(file.size)}</span>
                </div>
                <button
                  type="button"
                  className="file-item__remove"
                  onClick={() => removeFile(file.id)}
                  disabled={disabled}
                  aria-label={`Remove ${file.name}`}
                >
                  <X size={16} />
                </button>
              </motion.div>
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
