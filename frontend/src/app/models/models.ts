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
  id: string; instrumentId: string; instrumentName: string; lotSize: number;
  entryPrice: number; exitPrice: number; stopLoss: number; takeProfit: number;
  profitLoss: number; riskPercentage: number; riskRewardRatio: number;
  tradeDate: string; tradeDurationMinutes: number; tradeType: string; result: string;
  notes?: string; tags?: string; createdAt: string;
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
  totalProfitLoss: number; winRate: number; totalTrades: number; winCount: number; lossCount: number;
  averageRiskRewardRatio: number; maxDrawdown: number; currentDrawdown: number;
  todayPL: number; weekPL: number; monthPL: number; todayTradeCount: number;
  dailyLossLimitBreached: boolean; dailyLossLimit: number; accountBalance: number;
  monthlyPL: MonthlyPL[]; equityCurve: EquityCurvePoint[]; instrumentPerformance: InstrumentPerformance[];
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
export interface AiInsight { category: string; severity: string; title: string; message: string; recommendation: string; icon: string; }
export interface AiAnalysis { overallScore: string; bestInstrument: string; bestTimeOfDay: string; mostCommonMistake: string; insights: AiInsight[]; }
export interface RiskCalculation { accountBalance: number; riskPercent: number; instrumentId?: string; }
export interface RiskResult { riskAmount: number; suggestedLotSize: number; maxAllowedLotSize: number; maxTradesPerDay: number; riskLevel: string; warning: string; }
export interface Alert { id?: string; dailyLossLimit: number; maxDrawdownPercent: number; maxTradesPerDay: number; isActive: boolean; emailAlertEnabled: boolean; email?: string; }
