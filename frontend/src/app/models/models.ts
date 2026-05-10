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
}
export interface CreateTrade {
  instrumentId: string; lotSize: number; entryPrice: number; exitPrice: number;
  stopLoss: number; takeProfit: number; riskPercentage: number; tradeDate: string;
  tradeDurationMinutes: number; tradeType: number; notes?: string; tags?: string;
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
