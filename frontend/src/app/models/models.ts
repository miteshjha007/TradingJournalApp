// Auth models
export interface RegisterRequest { firstName: string; lastName: string; email: string; password: string; }
export interface LoginRequest { email: string; password: string; }
export interface AuthResponse { accessToken: string; refreshToken: string; expiresAt: string; user: UserInfo; }
export interface UserInfo { id: string; firstName: string; lastName: string; email: string; role: string; accountBalance: number; allowedSections: string[]; }
export interface AdminCreateUser { firstName: string; lastName: string; email: string; password?: string; role: string; allowedSections: string[]; }
export interface AdminUpdateUser { firstName: string; lastName: string; email: string; role: string; accountBalance: number; allowedSections: string[]; }

// Instrument models
export interface Instrument {
  id: string; name: string; safeLotSize: number; maxLot: number;
  volatilityLevel: string; notes?: string; description?: string; symbol?: string;
  createdAt: string; totalTrades: number; totalPL: number; winRate: number;
}
export interface CreateInstrument { name: string; safeLotSize: number; maxLot: number; volatilityLevel: number; notes?: string; description?: string; symbol?: string; }

// Trade models
export interface Trade {
  id?: string;
  instrumentId: string;
  instrumentName?: string;
  lotSize: number;
  entryPrice: number;
  exitPrice: number;
  stopLoss: number;
  takeProfit: number;
  profitLoss?: number;
  riskPercentage: number;
  riskRewardRatio?: number;
  tradeDate: string;
  tradeDurationMinutes: number;
  tradeType: string;
  result?: string;
  notes?: string;
  tags?: string;
  ruleViolations?: string[];
  createdAt: string;
  checklistCompliancePercent?: number;
  chartImageUrl?: string;
  tradingAccountId?: string;
}
export interface CreateTrade {
  instrumentId: string; lotSize: number; entryPrice: number; exitPrice: number;
  stopLoss: number; takeProfit: number; riskPercentage: number; tradeDate: string;
  tradeDurationMinutes: number; tradeType: number; notes?: string; tags?: string;
  checkedRuleIds?: string[];
  chartImageUrl?: string;
}
export interface TradeFilter { fromDate?: string; toDate?: string; instrumentId?: string; result?: string; tradeType?: string; page: number; pageSize: number; }
export interface PagedTrades { trades: Trade[]; totalCount: number; page: number; pageSize: number; totalPages: number; }

// Note models
export interface Note { id: string; title: string; content: string; tags?: string; isPinned: boolean; createdAt: string; updatedAt: string; }
export interface CreateNote { title: string; content: string; tags?: string; isPinned: boolean; }
export interface NoteFilter { searchTerm?: string; page: number; pageSize: number; }
export interface PagedNotes { notes: Note[]; totalCount: number; page: number; pageSize: number; totalPages: number; }

// Dashboard models
export interface DashboardSummary {
  totalProfitLoss: number;
  winRate: number;
  totalTrades: number;
  winCount: number;
  lossCount: number;
  averageRiskRewardRatio: number;
  maxDrawdown: number;
  currentDrawdown: number;
  todayPL: number;
  weekPL: number;
  monthPL: number;
  todayTradeCount: number;
  dailyLossLimitBreached: boolean;
  dailyLossLimit: number;
  accountBalance: number;
  isPropFirm: boolean;
  profitTarget: number;
  profitSplit: number;
  monthlyPL: MonthlyPL[];
  equityCurve: EquityCurvePoint[];
  instrumentPerformance: InstrumentPerformance[];
}
export interface MonthlyPL { month: string; profitLoss: number; tradeCount: number; }
export interface EquityCurvePoint { date: string; balance: number; pl: number; }
export interface InstrumentPerformance { instrumentName: string; totalPL: number; totalTrades: number; winRate: number; }
export interface CalendarDay { date: string; totalPL: number; tradeCount: number; isProfit: boolean; isLoss: boolean; }
export interface PerformanceMetrics {
  sharpeRatio: number; averageWin: number; averageLoss: number; largestWin: number; largestLoss: number;
  maxConsecutiveWins: number; maxConsecutiveLosses: number; profitFactor: number; expectedValue: number;
}
export interface DrawdownInfo {
  maxDrawdown: number; currentDrawdown: number; maxDrawdownPercent: number; currentDrawdownPercent: number;
  isWarning: boolean; isCritical: boolean; accountBalance: number;
}

// Analytics models
export interface TradingAccount {
  id: string;
  userId: string;
  name: string;
  balance: number;
  currency: string;
  broker?: string;
  isDefault: boolean;
  isPropFirm: boolean;
  propFirmName?: string;
  propFirmPlan?: string;
  minTradingDays: number;
  newsTradeAllowed: boolean;
  weekendHoldingAllowed: boolean;
  dailyDrawdownLimitPct: number;
  maxOverallLossPct: number;
  profitTargetPct: number;
  profitSplitPct: number;
  maxRiskPerTradePctOfDailyLimit: number;
  maxAllowedLotSize: number;
  useDynamicEquity: boolean;
  has5xLotRule: boolean;
  maxDailyLossType?: number; // 1=BalanceBased, 2=EquityBased
  createdAt: string;
}

export interface CreateTradingAccount {
  userId?: string; // For admins
  name: string;
  balance: number;
  currency: string;
  broker?: string;
  isDefault: boolean;
  isPropFirm: boolean;
  propFirmName?: string;
  propFirmPlan?: string;
  minTradingDays: number;
  newsTradeAllowed: boolean;
  weekendHoldingAllowed: boolean;
  dailyDrawdownLimitPct: number;
  maxOverallLossPct: number;
  profitTargetPct: number;
  profitSplitPct: number;
  maxRiskPerTradePctOfDailyLimit: number;
  maxAllowedLotSize: number;
  useDynamicEquity: boolean;
  has5xLotRule: boolean;
  maxDailyLossType?: number;
}

// Prop Firm Rule Engine Models
export interface PropFirmStatus {
  firmName: string;
  planName: string;
  accountBalance: number;
  // Daily Drawdown
  dailyLossUsed: number;
  dailyLossLimit: number;
  dailyLossUsedPct: number;
  remainingDailyBudget: number;
  // Overall Drawdown
  totalDrawdown: number;
  maxDrawdownLimit: number;
  totalDrawdownPct: number;
  remainingOverallBudget: number;
  // Profit Target
  profitEarned: number;
  profitTarget: number;
  profitEarnedPct: number;
  estimatedPayout: number;
  // Trading Days
  tradingDaysCompleted: number;
  minTradingDaysRequired: number;
  // Rule Flags
  newsTradeAllowed: boolean;
  weekendHoldingAllowed: boolean;
  has5xLotRule: boolean;
  useDynamicEquity: boolean;
  // Status
  accountStatus: 'SAFE' | 'WARNING' | 'CRITICAL' | 'BREACHED_DAILY' | 'BREACHED_OVERALL' | 'PASSED';
  dailyLimitBreached: boolean;
  overallLimitBreached: boolean;
  profitTargetReached: boolean;
  activeWarnings: string[];
  statusColor: string;
}

export interface PropFirmPreset {
  firmName: string;
  planName: string;
  accountSize: number;
  dailyDrawdownLimitPct: number;
  maxOverallLossPct: number;
  profitTargetPct: number;
  profitSplitPct: number;
  minTradingDays: number;
  maxAllowedLotSize: number;
  has5xLotRule: boolean;
  useDynamicEquity: boolean;
  newsTradeAllowed: boolean;
  weekendHoldingAllowed: boolean;
  maxRiskPerTradePctOfDailyLimit: number;
}

export interface AiInsight { category: string; severity: string; title: string; message: string; recommendation: string; icon: string; }
export interface AiAnalysis { overallScore: string; bestInstrument: string; bestTimeOfDay: string; mostCommonMistake: string; insights: AiInsight[]; }
export interface RiskCalculation { accountBalance: number; riskPercent: number; instrumentId?: string; }
export interface RiskResult { riskAmount: number; suggestedLotSize: number; maxAllowedLotSize: number; maxTradesPerDay: number; riskLevel: string; warning: string; }
export interface PropRiskCalculation {
  accountBalance: number;
  riskPercent: number;
  stopLossPips: number;
  instrumentId?: string;
  instrumentSymbol?: string;
  firstTradeLotSize?: number;
  dailyDrawdownLimit: number;  // % of account, e.g. 3 for 3%
  todayLoss: number;           // already lost today in $
}
export interface PropRiskResult {
  suggestedLotSize: number;
  pipValuePer001Lot: number;
  riskAmountDollar: number;
  maxLossIfSLHit: number;
  fiveXRuleMaxLot: number;
  violatesFiveXRule: boolean;
  dailyDrawdownLimitAmount: number;
  dailyDrawdownRemaining: number;
  dailyDrawdownBreached: boolean;
  riskLevel: string;
  warning: string;
  isSafe: boolean;
  instrumentCategory: string;
}
export interface Alert { id?: string; dailyLossLimit: number; maxDrawdownPercent: number; maxTradesPerDay: number; isActive: boolean; emailAlertEnabled: boolean; email?: string; }

// Chat Forum Models
export enum AnnouncementPriority {
  Info = 1,
  Important = 2,
  Urgent = 3
}

export enum ChannelType {
  PublicForum = 1,
  DirectMessage = 2
}

export interface Announcement {
  id: string;
  title: string;
  content: string;
  priority: AnnouncementPriority;
  adminId: string;
  adminName: string;
  createdAt: string;
}

export interface CreateAnnouncement {
  title: string;
  content: string;
  priority: AnnouncementPriority;
}

export interface ForumMessage {
  id: string;
  content: string;
  authorId: string;
  authorName: string;
  authorInitials: string;
  channelType: ChannelType;
  parentMessageId?: string;
  replyCount: number;
  isEdited: boolean;
  editedAt?: string;
  createdAt: string;
}

export interface CreateForumMessage {
  content: string;
  channelType: ChannelType;
  parentMessageId?: string;
  receiverId?: string;
}

export interface DirectMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderInitials: string;
  receiverId: string;
  receiverName: string;
  content: string;
  isRead: boolean;
  createdAt: string;
}

export interface UnreadCount {
  userId: string;
  unreadCount: number;
}

// Playbook models
export enum PlaybookCategory { Entry = 1, Risk = 2, Psychology = 3, Exit = 4 }

export interface PlaybookRule {
  id: string;
  title: string;
  description?: string;
  category: PlaybookCategory;
  isActive: boolean;
  orderIndex: number;
  createdAt: string;
}
export interface CreatePlaybookRule {
  title: string;
  description?: string;
  category: PlaybookCategory;
}
export interface UpdatePlaybookRule {
  title: string;
  description?: string;
  category: PlaybookCategory;
  isActive: boolean;
}
export interface TradeChecklistItem {
  ruleId: string;
  ruleTitle: string;
  category: PlaybookCategory;
  isChecked: boolean;
}
export interface SaveChecklist {
  tradeId: string;
  checkedRuleIds: string[];
}

// AI Chat models
export enum AiProvider { OpenAI = 1, Anthropic = 2, Gemini = 3, DeepSeek = 4, Custom = 5 }

export interface UserAiSettings {
  provider: AiProvider;
  modelName?: string;
  customBaseUrl?: string;
  isConfigured: boolean;
}
export interface SaveAiSettings {
  provider: AiProvider;
  apiKey: string;
  modelName?: string;
  customBaseUrl?: string;
}
export interface AiChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
}
export interface AiChatSession {
  id: string;
  title: string;
  messages: AiChatMessage[];
  createdAt: string;
  updatedAt: string;
}
export interface SendAiMessage {
  message: string;
  sessionId?: string;
}

// Backtest models
export enum BacktestRuleType { MinRRR = 1, MaxDailyTrades = 2, ChecklistCompliance = 3, TradeType = 4, TimeOfDay = 5, MaxRiskPercent = 6 }

export interface BacktestRuleFilter {
  ruleType: BacktestRuleType;
  minValue?: number;
  maxValue?: number;
  stringValue?: string;
}
export interface BacktestRequest {
  name: string;
  fromDate?: string;
  toDate?: string;
  rules: BacktestRuleFilter[];
  initialBalance: number;
  runMonteCarlo: boolean;
}
export interface MonteCarloResult {
  p5FinalBalance: number;
  medianFinalBalance: number;
  p95FinalBalance: number;
  ruinProbability: number;
  p5Drawdown: number;
  medianDrawdown: number;
  p95Drawdown: number;
}
export interface BacktestResult {
  id: string;
  name: string;
  fromDate?: string;
  toDate?: string;
  tradeCount: number;
  filteredTradeCount: number;
  totalPL: number;
  winRate: number;
  profitFactor: number;
  sharpeRatio: number;
  sortinoRatio: number;
  maxDrawdown: number;
  maxDrawdownPercent: number;
  averageRRR: number;
  bestDay: number;
  worstDay: number;
  equityCurve: EquityCurvePoint[];
  monteCarlo?: MonteCarloResult;
  createdAt: string;
}

// Streak & Discipline models
export interface Streak {
  currentStreak: number;
  longestStreak: number;
  disciplineScore: number;
  disciplineGrade: string;
  tradedToday: boolean;
  checklistAvgComplianceToday?: number;
  streakBrokenReason?: string;
}

// Heatmap models
export interface HeatmapCell {
  dayOfWeek: number;
  hour: number;
  totalPL: number;
  tradeCount: number;
  winRate: number;
  avgPL: number;
  intensity: number;
}
export interface SessionStats {
  name: string;
  totalPL: number;
  tradeCount: number;
  winRate: number;
}
export interface HeatmapData {
  cells: HeatmapCell[];
  sessions: SessionStats[];
  bestSlot?: HeatmapCell;
  worstSlot?: HeatmapCell;
}

// Shadow Account / Journal DNA models
export interface ShadowRule {
  description: string;
  impact: number;
}
export interface Pattern {
  label: string;
  value: string;
  avgPL: number;
  tradeCount: number;
}
export interface ShadowProfile {
  winningRules: ShadowRule[];
  losingRules: ShadowRule[];
  bestPatterns: Pattern[];
  worstPatterns: Pattern[];
  dna: string;
  consistencyScore: number;
}

// Strategy Analyzer models
export interface StrategyQuery {
  userMessage: string;
  daysBack: number;
}

export interface ExtractedStrategyFilters {
  instrumentName?: string;
  fromHour?: number;
  toHour?: number;
  dayOfWeek?: number;
  minRRR?: number;
  maxRRR?: number;
  minLotSize?: number;
  maxLotSize?: number;
  minRiskPercent?: number;
  maxRiskPercent?: number;
  result?: string;
  tradeType?: string;
  minChecklistCompliance?: number;
  session?: string;
  filterSummary: string;
}

export interface StrategyTradePreview {
  tradeDate: string;
  instrumentName: string;
  tradeType: string;
  lotSize: number;
  profitLoss: number;
  riskRewardRatio: number;
  result: string;
}

export interface StrategyAnalysisResult {
  filters: ExtractedStrategyFilters;
  matchedTrades: number;
  totalTradesInPeriod: number;
  winRate: number;
  totalPL: number;
  averageRRR: number;
  averagePL: number;
  maxWin: number;
  maxLoss: number;
  profitFactor: number;
  sharpeRatio: number;
  winCount: number;
  lossCount: number;
  averageLotSize: number;
  averageDurationMinutes: number;
  bestInstrument?: string;
  hasData: boolean;
  aiSummary: string;
  tradePreview: StrategyTradePreview[];
}

export interface StrategyTemplate {
  id: string;
  name: string;
  description: string;
  methodology: string;
  instrument: string;
  rules: string[];
  defaultFilters: string; // JSON string
  sessionBadge: string;
  timeframeBadge: string;
  minRRR: number;
  isSystemTemplate: boolean;
  isActive: boolean;
}

export interface CreateStrategyTemplate {
  name: string;
  description: string;
  methodology: string;
  instrument: string;
  rules: string[];
  defaultFilters: string;
  sessionBadge: string;
  timeframeBadge: string;
  minRRR: number;
}
