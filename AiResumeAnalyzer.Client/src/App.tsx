import { useState } from 'react';
import { Layout } from './components/Layout';
import { JobInput } from './components/features/JobInput';
import { FileUpload } from './components/features/FileUpload';
import { Button } from './components/ui/Button';
import { CandidateList } from './components/features/CandidateList';
import { CandidateDetail } from './components/features/CandidateDetail';
import { type AnalyzeResponse, type MatchResult } from './types/api';
import { Search, Download, Settings, ChevronLeft, ChevronRight, FileSearch } from 'lucide-react';
import { cn } from './lib/utils';
import { motion, AnimatePresence } from 'framer-motion';

function App() {
  const [jobDescription, setJobDescription] = useState('');
  const [files, setFiles] = useState<File[]>([]);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [results, setResults] = useState<MatchResult[] | null>(null);
  const [selectedIndex, setSelectedIndex] = useState<number>(0);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);

  const handleAnalyze = async () => {
    if (!jobDescription || files.length === 0) return;
    setIsAnalyzing(true);
    try {
      const formData = new FormData();
      formData.append('JobDescription', jobDescription);
      files.forEach(file => formData.append('UploadFiles', file));

      const response = await fetch('/api/analyze', { method: 'POST', body: formData });
      if (!response.ok) throw new Error('Analysis failed');

      const data: AnalyzeResponse = await response.json();
      setResults(data.results.sort((a, b) => (b.matchScore || 0) - (a.matchScore || 0)));
      setSelectedIndex(0);
    } catch (error) {
      console.error(error);
      alert('Analysis failed. Please check the backend connection.');
    } finally {
      setIsAnalyzing(false);
    }
  };

  const handleExportPdf = async () => {
    if (!results) return;
    try {
      const response = await fetch('/api/export/pdf', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ results, meta: { processedResumes: results.length, failedResumes: 0 } }),
      });
      if (!response.ok) throw new Error('Export failed');
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Analysis_Report_${new Date().toISOString().split('T')[0]}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error(error);
    }
  };

  const hasResults = results !== null && results.length > 0;

  return (
    <Layout>
      {/* SIDEBAR: Configuration */}
      <motion.aside
        animate={{ width: isSidebarOpen ? 400 : 0, opacity: isSidebarOpen ? 1 : 0 }}
        className={cn(
          "h-full flex flex-col bg-[var(--bg-sidebar)] border-r border-[var(--border-subtle)] relative z-40 overflow-hidden",
          !isSidebarOpen && "pointer-events-none"
        )}
      >
        <div className="p-8 pb-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center text-white font-black text-xl">R</div>
            <h1 className="font-bold tracking-tight text-lg">Analyzer Pro</h1>
          </div>
          <Button variant="ghost" size="sm" className="w-8 h-8 p-0 border border-[var(--border-subtle)]">
            <Settings className="w-4 h-4" />
          </Button>
        </div>

        <div className="flex-1 p-8 space-y-10 overflow-y-auto">
          <JobInput value={jobDescription} onChange={setJobDescription} />
          <FileUpload
            files={files}
            onFilesSelected={(newFiles) => setFiles(prev => [...prev, ...newFiles])}
            onRemoveFile={(idx) => setFiles(prev => prev.filter((_, i) => i !== idx))}
          />
        </div>

        <div className="p-8 border-t border-[var(--border-subtle)] bg-[var(--bg-main)]">
          <Button
            className="w-full h-12"
            size="lg"
            disabled={!jobDescription || files.length === 0}
            isLoading={isAnalyzing}
            onClick={handleAnalyze}
          >
            Run Matching Analysis
          </Button>
        </div>

        {/* Sidebar Toggle */}
        <button
          onClick={() => setIsSidebarOpen(!isSidebarOpen)}
          className="absolute -right-3 top-10 w-6 h-6 bg-white border border-[var(--border-subtle)] flex items-center justify-center rounded-full shadow-sm hover:bg-slate-50 transition-colors z-50 text-[var(--text-secondary)]"
        >
          {isSidebarOpen ? <ChevronLeft className="w-3 h-3" /> : <ChevronRight className="w-3 h-3" />}
        </button>
      </motion.aside>

      {/* MAIN CONTENT Area */}
      <main className="flex-1 flex flex-col relative bg-[var(--bg-main)]">

        <AnimatePresence mode="wait">
          {!hasResults && !isAnalyzing && (
            <motion.div
              key="idle"
              className="flex-1 flex flex-col items-center justify-center p-12 text-center"
            >
              <div className="w-20 h-20 bg-slate-100 rounded-3xl flex items-center justify-center mb-6">
                <Search className="w-10 h-10 text-slate-300" />
              </div>
              <h2 className="text-2xl font-bold mb-2">Awaiting Matching Data</h2>
              <p className="text-[var(--text-secondary)] max-w-sm">Provide a job description and upload resumes to start the matching process.</p>
            </motion.div>
          )}

          {isAnalyzing && (
            <motion.div
              key="analyzing"
              className="flex-1 flex flex-col items-center justify-center p-12"
            >
              <div className="w-16 h-16 border-4 border-slate-100 border-t-blue-600 rounded-full animate-spin mb-6" />
              <h3 className="text-xl font-bold mb-2">Analyzing Candidates</h3>
              <p className="text-[var(--text-secondary)]">Extracting data and running AI matching algorithms...</p>
            </motion.div>
          )}

          {hasResults && !isAnalyzing && (
            <motion.div
              key="results"
              initial={{ opacity: 0 }} animate={{ opacity: 1 }}
              className="flex-1 flex flex-col md:flex-row h-full"
            >
              {/* Leaderboard */}
              <div className="w-full md:w-80 flex flex-col border-r border-[var(--border-subtle)]">
                <div className="p-6 pb-2 flex items-center justify-between">
                  <span className="section-title">Candidate Rankings</span>
                  <Button variant="ghost" size="sm" onClick={handleExportPdf} title="Export Report">
                    <Download className="w-4 h-4" />
                  </Button>
                </div>
                <div className="flex-1 p-4 overflow-y-auto">
                  <CandidateList
                    candidates={results!}
                    selectedIndex={selectedIndex}
                    onSelect={setSelectedIndex}
                  />
                </div>
              </div>

              {/* Detail Canvas */}
              <div className="flex-1 overflow-y-auto p-8 md:p-12">
                <CandidateDetail result={results![selectedIndex]} />
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </main>
    </Layout>
  );
}

export default App;
