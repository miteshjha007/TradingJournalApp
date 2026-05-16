import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TRADING_TERMS_ARRAY, TradingTerm } from '../../data/trading-terms.data';
import { ApiService } from '../../services/api.service';
import { PerformanceMetrics, AiAnalysis } from '../../models/models';

@Component({
  selector: 'app-learn',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="learn-page">
      <div class="page-header">
        <h1>Knowledge Hub</h1>
        <p class="subtitle">Master trading concepts and get personalized performance insights</p>
      </div>

      <div class="tabs-nav">
        <button [class.active]="activeTab() === 'glossary'" (click)="activeTab.set('glossary')">📖 Glossary</button>
        <button [class.active]="activeTab() === 'concepts'" (click)="activeTab.set('concepts')">💡 Concepts</button>
        <button [class.active]="activeTab() === 'insights'" (click)="activeTab.set('insights')">🤖 Personalized Insights</button>
        <button [class.active]="activeTab() === 'faq'" (click)="activeTab.set('faq')">❓ FAQ</button>
      </div>

      <div class="tab-content">
        <!-- Glossary Tab -->
        @if (activeTab() === 'glossary') {
          <div class="glossary-tab">
            <div class="search-bar">
              <input type="text" placeholder="Search trading terms..." (input)="onSearch($event)" class="search-input" />
            </div>
            <div class="terms-grid">
              @for (term of filteredTerms(); track term.key) {
                <div class="term-card">
                  <div class="term-header">
                    <h3>{{ term.name }}</h3>
                    <span class="category-tag">{{ term.category }}</span>
                  </div>
                  <p class="term-def">{{ term.definition }}</p>
                  @if (term.formula) {
                    <div class="term-formula">
                      <code>{{ term.formula }}</code>
                    </div>
                  }
                  <div class="term-footer">
                    <span class="benchmark">Benchmark: <strong>{{ term.goodValue }}</strong></span>
                    <p class="tip">💡 {{ term.actionableTip }}</p>
                  </div>
                </div>
              }
            </div>
          </div>
        }

        <!-- Concepts Tab -->
        @if (activeTab() === 'concepts') {
          <div class="concepts-tab">
            <div class="concept-section">
              <h2>Risk Management 101</h2>
              <div class="concept-content">
                <p>The number one rule in trading is preservation of capital. Without capital, you cannot play the game. Proper risk management ensures that no single trade, or string of trades, can blow your account.</p>
                <ul class="concept-list">
                  <li><strong>The 1% Rule:</strong> Never risk more than 1% of your account balance on a single trade.</li>
                  <li><strong>Risk/Reward Ratio:</strong> Aim for at least 1:2. This means you only need to be right 34% of the time to break even.</li>
                  <li><strong>Stop Losses:</strong> Always use them. They are your seatbelt in the market.</li>
                </ul>
              </div>
            </div>
            <div class="concept-section">
              <h2>Trading Psychology</h2>
              <div class="concept-content">
                <p>Trading is 20% strategy and 80% psychology. Your mind is often your biggest enemy.</p>
                <div class="psych-grid">
                  <div class="psych-card">
                    <h4>FOMO</h4>
                    <p>Fear Of Missing Out leads to chasing trades at bad prices. Stick to your plan.</p>
                  </div>
                  <div class="psych-card">
                    <h4>Revenge Trading</h4>
                    <p>Trying to "win back" losses immediately. This usually leads to bigger losses.</p>
                  </div>
                  <div class="psych-card">
                    <h4>Overtrading</h4>
                    <p>Trading too much due to boredom or greed. Quality over quantity.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        }

        <!-- Insights Tab -->
        @if (activeTab() === 'insights') {
          <div class="insights-tab">
            @if (loadingInsights()) {
              <div class="skeleton-container">
                <div class="skeleton title"></div>
                <div class="skeleton text"></div>
                <div class="skeleton text"></div>
                <div class="skeleton-grid">
                  <div class="skeleton box"></div>
                  <div class="skeleton box"></div>
                  <div class="skeleton box"></div>
                </div>
                <div class="skeleton text large"></div>
              </div>
            } @else {
              <div class="ai-insight-card">
                <div class="insight-header">
                  <span class="ai-badge">AI ANALYZER</span>
                  <h2>Personalized Performance Review</h2>
                </div>
                
                <div class="insight-summary">
                  <p>{{ aiAnalysis()?.overallScore ? 'Your trading performance shows a score of ' + aiAnalysis()?.overallScore : 'Start trading to get personalized AI insights!' }}</p>
                </div>

                <div class="metrics-highlight">
                  <div class="m-box">
                    <span class="m-label">Trading Score</span>
                    <span class="m-val">{{ aiAnalysis()?.overallScore || 'N/A' }}</span>
                  </div>
                  <div class="m-box">
                    <span class="m-label">Best Instrument</span>
                    <span class="m-val">{{ aiAnalysis()?.bestInstrument || 'N/A' }}</span>
                  </div>
                  <div class="m-box">
                    <span class="m-label">Sharpe Ratio</span>
                    <span class="m-val">{{ metrics()?.sharpeRatio | number:'1.2-2' }}</span>
                  </div>
                </div>

                <div class="insight-sections">
                  <div class="i-section">
                    <h4>✅ Strengths</h4>
                    <ul>
                      @for (s of aiAnalysis()?.insights; track s.title) {
                        @if (s.severity === 'Success') { <li><strong>{{ s.title }}:</strong> {{ s.message }}</li> }
                      } @empty { <li>No data yet</li> }
                    </ul>
                  </div>
                  <div class="i-section">
                    <h4>⚠️ Areas for Improvement</h4>
                    <ul>
                      @for (s of aiAnalysis()?.insights; track s.title) {
                        @if (s.severity === 'Warning' || s.severity === 'Danger') { <li><strong>{{ s.title }}:</strong> {{ s.message }}</li> }
                      } @empty { <li>No data yet</li> }
                    </ul>
                  </div>
                </div>
              </div>
            }
          </div>
        }

        <!-- FAQ Tab -->
        @if (activeTab() === 'faq') {
          <div class="faq-tab">
            <div class="faq-item">
              <h3>What is the most important trading metric?</h3>
              <p>While many focus on Win Rate, <strong>Expectancy</strong> and <strong>Profit Factor</strong> are more important. A trader with a 30% win rate can be highly profitable if their average win is 5x larger than their average loss.</p>
            </div>
            <div class="faq-item">
              <h3>How much should I risk per trade?</h3>
              <p>Most professional traders risk between 0.5% and 2.0% of their account balance per trade. This allows you to survive a "losing streak" (which will happen to everyone) without ruining your account.</p>
            </div>
            <div class="faq-item">
              <h3>Why do I need a trading journal?</h3>
              <p>Because humans have selective memory. We remember big wins but forget small, repetitive mistakes. A journal provides the objective data needed to find your "edge" and eliminate bad habits.</p>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .learn-page { padding: 2rem; max-width: 1200px; margin: 0 auto; min-height: 100vh; }
    .page-header { margin-bottom: 2rem; }
    .page-header h1 { font-size: 2.5rem; margin-bottom: 0.5rem; background: linear-gradient(135deg, var(--primary), #818cf8); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
    .subtitle { color: var(--text-muted); font-size: 1.1rem; }

    .tabs-nav { display: flex; gap: 1rem; border-bottom: 1px solid var(--border-color); margin-bottom: 2rem; padding-bottom: 0.5rem; }
    .tabs-nav button { padding: 0.75rem 1.25rem; background: none; border: none; color: var(--text-muted); font-weight: 600; cursor: pointer; transition: all 0.3s; border-radius: 8px; font-size: 1rem; }
    .tabs-nav button:hover { background: var(--bg-hover); color: var(--text-main); }
    .tabs-nav button.active { color: var(--primary); background: var(--primary-light); }

    /* Glossary Tab */
    .search-bar { margin-bottom: 1.5rem; }
    .search-input { width: 100%; padding: 0.875rem 1.25rem; border-radius: 12px; border: 1px solid var(--border-color); background: var(--bg-card); color: var(--text-main); font-size: 1rem; }
    .search-input:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 2px var(--primary-light); }

    .terms-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); gap: 1.5rem; }
    .term-card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 16px; padding: 1.5rem; display: flex; flex-direction: column; transition: transform 0.3s, box-shadow 0.3s; }
    .term-card:hover { transform: translateY(-4px); box-shadow: 0 8px 24px rgba(0,0,0,0.1); border-color: var(--primary); }
    .term-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .term-header h3 { margin: 0; font-size: 1.25rem; }
    .category-tag { padding: 2px 8px; background: var(--primary-light); color: var(--primary); border-radius: 4px; font-size: 0.7rem; font-weight: 700; text-transform: uppercase; }
    .term-def { font-size: 0.95rem; color: var(--text-main); line-height: 1.6; margin-bottom: 1rem; flex: 1; }
    .term-formula { background: var(--bg-main); padding: 0.75rem; border-radius: 8px; margin-bottom: 1rem; }
    .term-formula code { font-family: 'Fira Code', monospace; font-size: 0.85rem; color: var(--primary); }
    .term-footer { border-top: 1px solid var(--border-color); padding-top: 1rem; }
    .benchmark { font-size: 0.85rem; color: var(--text-muted); display: block; margin-bottom: 0.5rem; }
    .tip { font-size: 0.85rem; color: var(--text-main); font-style: italic; margin: 0; }

    /* Concepts Tab */
    .concept-section { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 20px; padding: 2rem; margin-bottom: 2rem; }
    .concept-section h2 { margin-top: 0; margin-bottom: 1.5rem; color: var(--primary); }
    .concept-content p { font-size: 1.1rem; line-height: 1.7; margin-bottom: 1.5rem; }
    .concept-list { list-style: none; padding: 0; }
    .concept-list li { padding: 0.75rem 0; font-size: 1rem; border-bottom: 1px solid var(--border-color); display: flex; align-items: center; gap: 0.75rem; }
    .concept-list li::before { content: "✅"; }
    .psych-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1.5rem; margin-top: 1rem; }
    .psych-card { background: var(--bg-main); padding: 1.5rem; border-radius: 16px; border: 1px solid var(--border-color); }
    .psych-card h4 { margin-top: 0; color: var(--danger); margin-bottom: 0.5rem; }
    .psych-card p { font-size: 0.9rem; margin: 0; }

    /* Insights Tab */
    .ai-insight-card { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 24px; padding: 2.5rem; position: relative; overflow: hidden; }
    .ai-insight-card::before { content: ""; position: absolute; top: 0; left: 0; width: 100%; height: 6px; background: linear-gradient(to right, var(--primary), #818cf8); }
    .insight-header { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 2rem; }
    .ai-badge { width: fit-content; padding: 4px 12px; background: var(--primary); color: white; border-radius: 99px; font-size: 0.7rem; font-weight: 800; letter-spacing: 0.05em; }
    .insight-summary p { font-size: 1.2rem; line-height: 1.6; color: var(--text-main); font-weight: 500; }
    .metrics-highlight { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1.5rem; margin: 2rem 0; }
    .m-box { background: var(--bg-main); padding: 1.5rem; border-radius: 16px; display: flex; flex-direction: column; align-items: center; gap: 0.5rem; }
    .m-label { font-size: 0.8rem; color: var(--text-muted); font-weight: 600; text-transform: uppercase; }
    .m-val { font-size: 2rem; font-weight: 800; color: var(--primary); }
    .insight-sections { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; }
    .i-section h4 { font-size: 1.1rem; margin-bottom: 1rem; }
    .i-section ul { padding-left: 1.25rem; }
    .i-section li { margin-bottom: 0.75rem; line-height: 1.5; }

    /* FAQ Tab */
    .faq-tab { display: flex; flex-direction: column; gap: 1.5rem; }
    .faq-item { background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 16px; padding: 1.5rem; }
    .faq-item h3 { margin-top: 0; margin-bottom: 0.75rem; font-size: 1.1rem; color: var(--text-main); }
    .faq-item p { margin: 0; font-size: 0.95rem; color: var(--text-muted); line-height: 1.6; }

    /* Skeleton Loader */
    .skeleton-container { display: flex; flex-direction: column; gap: 1rem; padding: 1rem; }
    .skeleton { background: var(--bg-hover); border-radius: 4px; animation: pulse 1.5s infinite ease-in-out; }
    .skeleton.title { height: 32px; width: 60%; margin-bottom: 1rem; }
    .skeleton.text { height: 16px; width: 100%; }
    .skeleton.text.large { height: 200px; margin-top: 1rem; border-radius: 16px; }
    .skeleton-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin: 1rem 0; }
    .skeleton.box { height: 100px; border-radius: 12px; }
    @keyframes pulse { 0% { opacity: 0.6; } 50% { opacity: 0.3; } 100% { opacity: 0.6; } }

    @media (max-width: 768px) {
      .tabs-nav { overflow-x: auto; white-space: nowrap; }
      .psych-grid, .insight-sections, .metrics-highlight { grid-template-columns: 1fr; }
      .terms-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class LearnComponent implements OnInit {
  activeTab = signal('glossary');
  filteredTerms = signal<TradingTerm[]>(TRADING_TERMS_ARRAY);
  loadingInsights = signal(true);
  
  metrics = signal<PerformanceMetrics | null>(null);
  aiAnalysis = signal<AiAnalysis | null>(null);

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadInsights();
  }

  loadInsights() {
    this.loadingInsights.set(true);
    // Simulate loading and fetch data
    setTimeout(() => {
      this.api.getPerformanceMetrics().subscribe({
        next: (m) => this.metrics.set(m)
      });
      this.api.getAiAnalysis().subscribe({
        next: (a) => {
          this.aiAnalysis.set(a);
          this.loadingInsights.set(false);
        },
        error: () => this.loadingInsights.set(false)
      });
    }, 800); // Small artificial delay for skeleton wow effect
  }

  onSearch(event: Event) {
    const query = (event.target as HTMLInputElement).value.toLowerCase();
    if (!query) {
      this.filteredTerms.set(TRADING_TERMS_ARRAY);
      return;
    }
    this.filteredTerms.set(
      TRADING_TERMS_ARRAY.filter(t => 
        t.name.toLowerCase().includes(query) || 
        t.definition.toLowerCase().includes(query) ||
        t.category.toLowerCase().includes(query)
      )
    );
  }
}
