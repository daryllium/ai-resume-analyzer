import { Sparkles, Github } from 'lucide-react';
import './Header.css';

export function Header() {
  return (
    <header className="header glass">
      <div className="container header__container">
        <a href="/" className="header__logo">
          <div className="header__logo-icon">
            <Sparkles size={20} />
          </div>
          <span className="header__logo-text">ResumeMatch AI</span>
        </a>
        <nav className="header__nav">
          <a
            href="https://github.com"
            target="_blank"
            rel="noopener noreferrer"
            className="header__link"
          >
            <Github size={18} />
            <span>Source</span>
          </a>
        </nav>
      </div>
    </header>
  );
}
