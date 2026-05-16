export interface TradingTerm {
  key: string;
  name: string;
  category: string;
  definition: string;
  formula?: string;
  goodValue?: string;
  actionableTip: string;
}

export const TRADING_TERMS: Record<string, { term: string; definition: string; formula?: string; goodValue?: string; tip: string }> = {
  'max-drawdown': {
    term: 'Max Drawdown',
    definition: 'The largest peak-to-trough decline in your account balance. It shows the worst losing streak your strategy has experienced.',
    formula: 'Peak Balance − Lowest Balance',
    goodValue: 'Below 5% for prop firms',
    tip: 'For Funding Pips, your max drawdown must stay below 8% of your starting balance — this is your absolute stop line.'
  },
  'current-drawdown': {
    term: 'Current Drawdown',
    definition: 'How far your account is currently down from its highest ever point. Updates after every trade.',
    formula: 'Peak Balance − Current Balance',
    goodValue: 'As close to 0% as possible',
    tip: 'If your current drawdown hits 70% of the firm limit, stop trading for the day and review your positions.'
  },
  'win-rate': {
    term: 'Win Rate',
    definition: 'The percentage of your trades that close in profit. A 50% win rate means 1 in 2 trades wins.',
    formula: 'Winning Trades ÷ Total Trades × 100',
    goodValue: 'Above 50% is profitable (with good RRR)',
    tip: 'Win rate alone doesn\'t tell the full story. A 40% win rate with 1:3 RRR is more profitable than 60% with 1:1.'
  },
  'risk-reward-ratio': {
    term: 'Risk-Reward Ratio (RRR)',
    definition: 'How much you stand to make compared to how much you risk. An RRR of 1:2 means you risk $50 to potentially make $100.',
    formula: '|Take Profit − Entry| ÷ |Entry − Stop Loss|',
    goodValue: 'Minimum 1:1.5, ideally 1:2 or above',
    tip: 'With a 1:2 RRR, you only need to win 34% of trades to break even. Higher RRR gives you more margin for error.'
  },
  'profit-factor': {
    term: 'Profit Factor',
    definition: 'Total profit from winning trades divided by total loss from losing trades. Tells you if your system has a mathematical edge.',
    formula: 'Sum of all wins ÷ Sum of all losses',
    goodValue: 'Above 1.5 is solid. Above 2.0 is excellent.',
    tip: 'A profit factor below 1.0 means you are losing money overall regardless of win rate. Target 1.5+ consistently.'
  },
  'sharpe-ratio': {
    term: 'Sharpe Ratio',
    definition: 'Measures your returns relative to the risk you took. A higher Sharpe means you earned more for each unit of risk.',
    formula: 'Mean Daily Return ÷ Std Dev of Returns × √252',
    goodValue: 'Above 1.0 is good, above 2.0 is excellent',
    tip: 'Two traders with the same total P&L can have very different Sharpe ratios. High Sharpe = more consistent, less lucky.'
  },
  'sortino-ratio': {
    term: 'Sortino Ratio',
    definition: 'Like Sharpe Ratio but only penalises downside (losing) days. A better measure for traders because it ignores upside volatility.',
    formula: 'Mean Daily Return ÷ Std Dev of Losing Days × √252',
    goodValue: 'Above 1.0 is good, above 2.0 is excellent',
    tip: 'If your Sortino is much higher than your Sharpe, your losing days are very consistent — which is actually good for prop trading.'
  },
  'expected-value': {
    term: 'Expected Value (EV)',
    definition: 'The average P&L you can expect per trade, accounting for both win rate and average win/loss size. Positive EV = profitable system.',
    formula: '(Win Rate × Avg Win) − (Loss Rate × Avg Loss)',
    goodValue: 'Any positive number',
    tip: 'If your EV per trade is $15 and you take 100 trades, you\'d statistically expect $1,500 profit — before variance.'
  },
  'daily-loss-limit': {
    term: 'Daily Loss Limit',
    definition: 'The maximum amount you are allowed to lose in a single trading day. Breaching this on prop firm accounts causes immediate account termination.',
    formula: 'Account Balance × Daily Drawdown %',
    goodValue: 'Never exceed it. Set alert at 70% of limit.',
    tip: 'For a $10,000 Funding Pips account (4% daily limit), your hard limit is $400/day. Stop trading the moment you hit $280 (70%).'
  },
  'profit-target': {
    term: 'Profit Target',
    definition: 'The profit amount you need to reach to pass a prop firm challenge or qualify for a payout on a funded account.',
    formula: 'Account Size × Profit Target %',
    goodValue: 'Reach it slowly — 1-2% per week is sustainable',
    tip: 'Never rush the profit target. Traders who try to hit it fast in 1 week blow their accounts. Slow and steady wins the funded account.'
  },
  'lot-size': {
    term: 'Lot Size',
    definition: 'The volume of a trade. 1.00 lot = 100,000 units of the base currency. Larger lot size = bigger profit AND bigger loss per pip.',
    formula: 'Micro lot = 0.01 | Mini lot = 0.10 | Standard lot = 1.00',
    goodValue: 'Keep within your Safe Lot Size per instrument',
    tip: 'For a $5,000 prop account, most traders use 0.01–0.05 lots per trade. Never use more than your instrument\'s Max Lot setting.'
  },
  'equity-curve': {
    term: 'Equity Curve',
    definition: 'A line graph showing how your account balance has changed over time after each trade. A rising, smooth curve indicates a consistent strategy.',
    formula: 'Running total of all P&L added to starting balance',
    goodValue: 'Steadily rising with small dips',
    tip: 'If your equity curve has sharp drops followed by big jumps, you are inconsistent. Consistency is more important than big wins for prop firms.'
  },
  'consecutive-losses': {
    term: 'Max Consecutive Losses',
    definition: 'The longest streak of losing trades in a row. Tells you the worst cold streak your strategy has experienced.',
    formula: 'Count of back-to-back losing trades',
    goodValue: 'Below 5 for most strategies',
    tip: 'If you hit 3 consecutive losses in a day, stop trading and review. Revenge trading after losses is the #1 account killer.'
  },
  'trading-score': {
    term: 'Trading Score',
    definition: 'Your personal discipline score (0–100) calculated from win rate, RRR, daily trade count, and lot size consistency. Higher = more disciplined.',
    formula: 'Win Rate (25pts) + RRR (25pts) + Trade Count (25pts) + Lot Consistency (25pts)',
    goodValue: 'A grade (90+) is excellent. B grade (70+) is good.',
    tip: 'Focus on improving the lowest-scoring component first. Usually trade count (overtrading) and lot consistency are the easiest quick wins.'
  },
  'monte-carlo': {
    term: 'Monte Carlo Simulation',
    definition: 'A method that runs your strategy 1,000 times in random order to test if your results were skill or luck. If most simulations are profitable, your edge is real.',
    formula: '1,000 random shuffles of your trades → median final P&L',
    goodValue: 'Median positive + Ruin probability below 5%',
    tip: 'If your Monte Carlo P5 (worst 5% case) is still positive, your strategy has a genuine edge that survives bad luck. This is what prop firms look for.'
  },
  'ruin-probability': {
    term: 'Ruin Probability',
    definition: 'From Monte Carlo simulation: the percentage of scenarios where your account would have been fully blown (hit max drawdown limit).',
    formula: 'Simulations hitting max drawdown ÷ Total simulations × 100',
    goodValue: 'Below 5% is safe. Below 1% is excellent.',
    tip: 'Even a 10% ruin probability means 1 in 10 traders with your exact strategy would blow their account through bad luck alone.'
  },
  'discipline-streak': {
    term: 'Discipline Streak',
    definition: 'The number of consecutive days where you logged at least one trade without breaching your daily loss limit. Measures trading consistency.',
    formula: 'Count of consecutive trading days without limit breach',
    goodValue: 'Aim for 30+ day streaks',
    tip: 'Your streak resets if you breach your daily loss limit. This is intentional — the goal is to make protecting your account feel like a game.'
  },
  'checklist-compliance': {
    term: 'Checklist Compliance',
    definition: 'The percentage of your Playbook rules you followed before taking a trade. 100% means you verified every rule before entering.',
    formula: 'Rules checked ÷ Total active rules × 100',
    goodValue: '80%+ consistently. 100% is the goal.',
    tip: 'Traders who follow their checklist 80%+ of the time have significantly higher win rates than those who skip it. Track your compliance score per trade.'
  },
  'prop-firm': {
    term: 'Prop Firm (Proprietary Trading Firm)',
    definition: 'A company that funds traders with their own capital after passing a challenge. Traders keep 70–100% of profits but must follow strict risk rules.',
    formula: 'Pass challenge → Get funded → Trade firm capital → Split profits',
    goodValue: 'Choose firms with low daily limits (4-5%) and good payout history',
    tip: 'The challenge is designed to be hard to prevent reckless traders. Treat the challenge exactly as you would a funded account — same risk rules.'
  },
  'dynamic-equity': {
    term: 'Dynamic Equity',
    definition: 'A prop firm rule where your daily loss limit is calculated from your CURRENT account balance, not the starting balance. As you profit, your limit increases.',
    formula: 'Daily Limit = Current Equity × Daily Drawdown %',
    goodValue: 'Understand which type your firm uses before trading',
    tip: 'Funding Pips uses dynamic equity. If you start at $10K and grow to $11K, your daily limit increases from $400 to $440. This rewards consistency.'
  },
  '5x-lot-rule': {
    term: '5x Lot Rule',
    definition: 'A Funding Pips risk management rule. Your maximum lot size on any trade cannot exceed 5 times your smallest lot size on the first trade of the day.',
    formula: 'Max Lot ≤ First Trade Lot × 5',
    goodValue: 'Always check before scaling up lot size',
    tip: 'If you open your first trade at 0.02 lots, your maximum for any trade that day is 0.10 lots. The Risk Calculator enforces this automatically.'
  },
  'london-session': {
    term: 'London Session',
    definition: 'The most liquid and volatile trading session, running from 07:00–16:00 UTC. Overlaps with New York from 13:00–16:00 UTC, creating the highest volume period.',
    formula: '07:00–16:00 UTC | 08:00–17:00 BST',
    goodValue: 'Best for GBP, EUR, Gold pairs',
    tip: 'Most professional traders focus on the London open (07:00–09:00 UTC) and London/NY overlap (13:00–16:00 UTC). Check your Heatmap to see your best hours.'
  },
  'new-york-session': {
    term: 'New York Session',
    definition: 'The second most active trading session. Runs 13:00–22:00 UTC. Overlaps with London for 3 hours, creating the highest volume window of the day.',
    formula: '13:00–22:00 UTC | 08:00–17:00 EST',
    goodValue: 'Best for USD pairs, Gold, S&P500',
    tip: 'The London/NY overlap (13:00–16:00 UTC) is statistically the most profitable time for most traders. Your Heatmap will confirm if this is true for you.'
  },
  'asia-session': {
    term: 'Asia / Tokyo Session',
    definition: 'The quietest major trading session. Runs 00:00–09:00 UTC. Lower volatility but can create important range levels that London and NY break.',
    formula: '00:00–09:00 UTC | 09:00–18:00 JST',
    goodValue: 'Best for JPY pairs. Avoid for Gold/GBP in most cases.',
    tip: 'Trading during low-liquidity Asia session with tight stops often results in stop hunts. Check your Heatmap — most traders do poorly in Asia.'
  },
  'stop-loss': {
    term: 'Stop Loss (SL)',
    definition: 'A pre-set price level where your trade automatically closes to limit your loss. The most important risk management tool in trading.',
    formula: 'Risk $ = |Entry − SL| × Lot Size × Pip Value',
    goodValue: 'Always set before entering. Never remove it once set.',
    tip: 'Never move your stop loss further away to avoid being stopped out. This is revenge trading and destroys accounts. Only move SL to protect profits.'
  },
  'take-profit': {
    term: 'Take Profit (TP)',
    definition: 'A pre-set price level where your trade automatically closes to lock in your profit. Placing TP before entry forces you to plan your trade.',
    formula: 'Profit $ = |TP − Entry| × Lot Size × Pip Value',
    goodValue: 'Set at 1.5x–3x your risk distance',
    tip: 'Set TP at the NEXT key level of support or resistance — not a random price. Random TP placement is the difference between professional and amateur trading.'
  },
  'profit-and-loss': {
    term: 'Profit and Loss (P&L)',
    definition: 'The net amount of money you have made or lost on a trade or over a period of time. Includes commission and swap fees.',
    formula: 'Profit = (Exit Price − Entry Price) × Lot Size × Pip Value',
    goodValue: 'Positive is the goal!',
    tip: 'Don\'t focus on the P&L of a single trade. Focus on your cumulative P&L over 20+ trades to see if you have a real edge.'
  },
  'risk-per-trade': {
    term: 'Risk Amount',
    definition: 'The dollar amount you will lose if your stop loss is hit. This should be a small percentage of your total account.',
    formula: 'Risk = Account Balance × Risk %',
    goodValue: 'Ideally 1% or less of balance',
    tip: 'Standardizing your risk amount (e.g. always risking $100) helps remove emotion from trading and makes your results more predictable.'
  },
  'pip-value': {
    term: 'Pip Value',
    definition: 'The value of a single pip (point in percentage) for a specific lot size. Varies by currency pair and your account currency.',
    formula: 'Lot Size × Pip size (e.g. 0.0001) × Base/Account rate',
    goodValue: 'Depends on instrument volatility',
    tip: 'Gold (XAUUSD) has a much higher pip value than most currency pairs. Always use the Risk Calculator to avoid being "over-leveraged".'
  },
  'profit-split': {
    term: 'Profit Split',
    definition: 'The percentage of profits you get to keep from a funded prop firm account. Typical splits range from 70/30 to 90/10.',
    formula: 'Trader Profit = Net Profit × Profit Split %',
    goodValue: '80% or higher is industry standard',
    tip: 'Some firms allow you to increase your split by reaching milestones or buying "add-ons". Focus on the 80% first — it\'s life-changing.'
  },
  'prop-firm-account': {
    term: 'Prop Firm Account',
    definition: 'An account provided by a proprietary trading firm after you pass their evaluation. You trade their capital and split the profits.',
    formula: 'Challenge → Verification → Funded Account',
    goodValue: 'Choose reputable firms with transparent rules',
    tip: 'Treat a prop account like a business. The firm is your partner, providing the capital while you provide the skill.'
  },
  'sortino-vs-sharpe': {
    term: 'Sortino vs Sharpe',
    definition: 'Sharpe penalises all volatility (good and bad days). Sortino only penalises bad days. For traders, Sortino is a better measure of skill.',
    formula: 'Sharpe: returns ÷ all-day std dev | Sortino: returns ÷ losing-day std dev',
    goodValue: 'Sortino higher than Sharpe = your winning days are bigger/more volatile than your losing days. Good sign.',
    tip: 'A Sortino ratio significantly higher than your Sharpe ratio is actually a positive signal — it means your wins are larger and more variable than your losses.'
  }
};

export const TERM_CATEGORIES: Record<string, string[]> = {
  'Risk': ['max-drawdown', 'current-drawdown', 'daily-loss-limit', 'stop-loss', 'take-profit', 'lot-size', '5x-lot-rule', 'risk-per-trade', 'pip-value'],
  'Performance': ['win-rate', 'risk-reward-ratio', 'profit-factor', 'sharpe-ratio', 'sortino-ratio', 'expected-value', 'consecutive-losses', 'equity-curve', 'monte-carlo', 'ruin-probability', 'sortino-vs-sharpe', 'profit-and-loss'],
  'Psychology': ['trading-score', 'discipline-streak', 'checklist-compliance', 'consecutive-losses'],
  'Prop Firm': ['prop-firm', 'dynamic-equity', '5x-lot-rule', 'profit-target', 'daily-loss-limit', 'profit-split', 'prop-firm-account'],
  'Sessions': ['london-session', 'new-york-session', 'asia-session']
};

/** Flattened array for easy consumption in components */
export const TRADING_TERMS_ARRAY: TradingTerm[] = Object.keys(TRADING_TERMS).map(key => {
  const info = TRADING_TERMS[key];
  const category = Object.keys(TERM_CATEGORIES).find(cat => TERM_CATEGORIES[cat].includes(key)) || 'Other';
  return {
    key,
    name: info.term,
    definition: info.definition,
    formula: info.formula,
    goodValue: info.goodValue,
    actionableTip: info.tip,
    category
  };
});
