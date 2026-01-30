import { type ReactNode } from 'react';

interface LayoutProps {
    children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
    return (
        <div className="min-h-screen w-full bg-[var(--bg-main)] text-[var(--text-primary)] antialiased">
            <main className="flex h-screen flex-col md:flex-row overflow-hidden">
                {children}
            </main>
        </div>
    );
}
