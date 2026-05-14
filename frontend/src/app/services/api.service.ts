import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Instrument, CreateInstrument,
  Trade, CreateTrade, TradeFilter, PagedTrades,
  Note, CreateNote, NoteFilter, PagedNotes,
  DashboardSummary, CalendarDay, PerformanceMetrics, DrawdownInfo,
  AiAnalysis, RiskCalculation, RiskResult, PropRiskCalculation, PropRiskResult, Alert,
  UserInfo, AdminCreateUser, TradingAccount, CreateTradingAccount,
  Announcement, CreateAnnouncement, ForumMessage, CreateForumMessage, DirectMessage, UnreadCount,
  PropFirmStatus, PropFirmPreset,
  PlaybookRule, CreatePlaybookRule, UpdatePlaybookRule, TradeChecklistItem, SaveChecklist,
  UserAiSettings, SaveAiSettings, AiChatSession, SendAiMessage,
  BacktestRequest, BacktestResult,
  Streak, HeatmapData, ShadowProfile
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  // Account endpoints
  getAccounts(targetUserId?: string): Observable<TradingAccount[]> {
    let params = new HttpParams();
    if (targetUserId) params = params.set('targetUserId', targetUserId);
    return this.http.get<TradingAccount[]>(`${environment.apiUrl}/accounts`, { params });
  }
  
  getAccount(id: string, targetUserId?: string): Observable<TradingAccount> {
    let params = new HttpParams();
    if (targetUserId) params = params.set('targetUserId', targetUserId);
    return this.http.get<TradingAccount>(`${environment.apiUrl}/accounts/${id}`, { params });
  }

  createAccount(data: CreateTradingAccount): Observable<TradingAccount> {
    return this.http.post<TradingAccount>(`${environment.apiUrl}/accounts`, data);
  }

  updateAccount(id: string, data: CreateTradingAccount): Observable<TradingAccount> {
    return this.http.put<TradingAccount>(`${environment.apiUrl}/accounts/${id}`, data);
  }

  deleteAccount(id: string, targetUserId?: string): Observable<void> {
    let params = new HttpParams();
    if (targetUserId) params = params.set('targetUserId', targetUserId);
    return this.http.delete<void>(`${environment.apiUrl}/accounts/${id}`, { params });
  }

  // Instruments
  getInstruments(): Observable<Instrument[]> {
    return this.http.get<Instrument[]>(`${environment.apiUrl}/instruments`);
  }
  getInstrument(id: string): Observable<Instrument> {
    return this.http.get<Instrument>(`${environment.apiUrl}/instruments/${id}`);
  }
  createInstrument(data: CreateInstrument): Observable<Instrument> {
    return this.http.post<Instrument>(`${environment.apiUrl}/instruments`, data);
  }
  updateInstrument(id: string, data: CreateInstrument): Observable<Instrument> {
    return this.http.put<Instrument>(`${environment.apiUrl}/instruments/${id}`, data);
  }
  deleteInstrument(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/instruments/${id}`);
  }

  // Trades
  getTrades(filter: TradeFilter): Observable<PagedTrades> {
    let params = new HttpParams()
      .set('page', filter.page).set('pageSize', filter.pageSize);
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);
    if (filter.instrumentId) params = params.set('instrumentId', filter.instrumentId);
    if (filter.result) params = params.set('result', filter.result);
    if (filter.tradeType) params = params.set('tradeType', filter.tradeType);
    return this.http.get<PagedTrades>(`${environment.apiUrl}/trades`, { params });
  }
  getTrade(id: string): Observable<Trade> {
    return this.http.get<Trade>(`${environment.apiUrl}/trades/${id}`);
  }
  createTrade(data: CreateTrade): Observable<Trade> {
    return this.http.post<Trade>(`${environment.apiUrl}/trades`, data);
  }
  updateTrade(id: string, data: CreateTrade): Observable<Trade> {
    return this.http.put<Trade>(`${environment.apiUrl}/trades/${id}`, data);
  }
  deleteTrade(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/trades/${id}`);
  }
  getCalendarTrades(year: number, month: number): Observable<Trade[]> {
    return this.http.get<Trade[]>(`${environment.apiUrl}/trades/calendar`, {
      params: new HttpParams().set('year', year).set('month', month)
    });
  }
  exportTrades(filter: Partial<TradeFilter> = {}): string {
    const params = new URLSearchParams();
    if (filter.fromDate) params.set('fromDate', filter.fromDate);
    if (filter.toDate) params.set('toDate', filter.toDate);
    if (filter.instrumentId) params.set('instrumentId', filter.instrumentId);
    return `${environment.apiUrl}/trades/export?${params.toString()}`;
  }

  // Notes
  getNotes(filter: NoteFilter): Observable<PagedNotes> {
    let params = new HttpParams()
      .set('page', filter.page).set('pageSize', filter.pageSize);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    return this.http.get<PagedNotes>(`${environment.apiUrl}/notes`, { params });
  }
  createNote(data: CreateNote): Observable<Note> {
    return this.http.post<Note>(`${environment.apiUrl}/notes`, data);
  }
  updateNote(id: string, data: CreateNote): Observable<Note> {
    return this.http.put<Note>(`${environment.apiUrl}/notes/${id}`, data);
  }
  deleteNote(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/notes/${id}`);
  }

  // Dashboard
  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${environment.apiUrl}/dashboard/summary`);
  }
  getCalendar(year: number, month: number): Observable<CalendarDay[]> {
    return this.http.get<CalendarDay[]>(`${environment.apiUrl}/dashboard/calendar`, {
      params: new HttpParams().set('year', year).set('month', month)
    });
  }
  getPerformance(): Observable<PerformanceMetrics> {
    return this.http.get<PerformanceMetrics>(`${environment.apiUrl}/dashboard/performance`);
  }
  getDrawdown(): Observable<DrawdownInfo> {
    return this.http.get<DrawdownInfo>(`${environment.apiUrl}/dashboard/drawdown`);
  }
  getAiInsights(): Observable<AiAnalysis> {
    return this.http.get<AiAnalysis>(`${environment.apiUrl}/dashboard/ai-insights`);
  }
  calculateRisk(data: RiskCalculation): Observable<RiskResult> {
    return this.http.post<RiskResult>(`${environment.apiUrl}/dashboard/risk-calculate`, data);
  }
  calculatePropRisk(data: PropRiskCalculation): Observable<PropRiskResult> {
    return this.http.post<PropRiskResult>(`${environment.apiUrl}/dashboard/prop-risk-calculate`, data);
  }
  getAlert(): Observable<Alert> {
    return this.http.get<Alert>(`${environment.apiUrl}/dashboard/alerts`);
  }
  upsertAlert(data: Alert): Observable<Alert> {
    return this.http.post<Alert>(`${environment.apiUrl}/dashboard/alerts`, data);
  }

  // Prop Firm Rule Engine
  getPropFirmStatus(): Observable<PropFirmStatus | null> {
    return this.http.get<PropFirmStatus | null>(`${environment.apiUrl}/dashboard/prop-firm-status`);
  }

  getPropFirmPresets(): Observable<PropFirmPreset[]> {
    return this.http.get<PropFirmPreset[]>(`${environment.apiUrl}/propfirm/presets`);
  }

  getPropFirmPresetsByFirm(firmName: string): Observable<PropFirmPreset[]> {
    return this.http.get<PropFirmPreset[]>(`${environment.apiUrl}/propfirm/presets/${encodeURIComponent(firmName)}`);
  }

  getPropFirmFirmNames(): Observable<string[]> {
    return this.http.get<string[]>(`${environment.apiUrl}/propfirm/firm-names`);
  }

  // Admin
  getAdminUsers(): Observable<UserInfo[]> {
    return this.http.get<UserInfo[]>(`${environment.apiUrl}/admin/users`);
  }
  adminCreateUser(data: AdminCreateUser): Observable<UserInfo> {
    return this.http.post<UserInfo>(`${environment.apiUrl}/admin/users`, data);
  }
  adminUpdateUser(id: string, data: any): Observable<UserInfo> {
    return this.http.put<UserInfo>(`${environment.apiUrl}/admin/users/${id}`, data);
  }

  // ----------------------------------------------------
  // Chat Forum (Announcements & Public/DM)
  // ----------------------------------------------------
  getAnnouncements(page: number = 1, pageSize: number = 50): Observable<{ announcements: Announcement[], totalCount: number }> {
    return this.http.get<{ announcements: Announcement[], totalCount: number }>(`${environment.apiUrl}/announcements?page=${page}&pageSize=${pageSize}`);
  }

  createAnnouncement(data: CreateAnnouncement): Observable<Announcement> {
    return this.http.post<Announcement>(`${environment.apiUrl}/announcements`, data);
  }

  deleteAnnouncement(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/announcements/${id}`);
  }

  getPublicForumMessages(page: number = 1, pageSize: number = 50): Observable<{ messages: ForumMessage[], totalCount: number }> {
    return this.http.get<{ messages: ForumMessage[], totalCount: number }>(`${environment.apiUrl}/forum/public?page=${page}&pageSize=${pageSize}`);
  }

  postForumMessage(data: CreateForumMessage): Observable<ForumMessage> {
    return this.http.post<ForumMessage>(`${environment.apiUrl}/forum/public`, data);
  }

  deleteForumMessage(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/forum/public/${id}`);
  }

  getDirectMessages(otherUserId: string): Observable<DirectMessage[]> {
    return this.http.get<DirectMessage[]>(`${environment.apiUrl}/forum/dm/${otherUserId}`);
  }

  sendDirectMessage(data: CreateForumMessage): Observable<DirectMessage> {
    return this.http.post<DirectMessage>(`${environment.apiUrl}/forum/dm`, data);
  }

  markAsRead(senderId: string): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/forum/dm/read/${senderId}`, {});
  }

  getUnreadCount(): Observable<UnreadCount> {
    return this.http.get<UnreadCount>(`${environment.apiUrl}/forum/dm/unread`);
  }

  getForumUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/forum/users`);
  }

  // Streak & Discipline
  getStreak(): Observable<Streak> {
    return this.http.get<Streak>(`${environment.apiUrl}/dashboard/streak`);
  }
  getHeatmap(): Observable<HeatmapData> {
    return this.http.get<HeatmapData>(`${environment.apiUrl}/dashboard/heatmap`);
  }
  getShadowProfile(): Observable<ShadowProfile> {
    return this.http.get<ShadowProfile>(`${environment.apiUrl}/dashboard/shadow-profile`);
  }

  // Playbook
  getPlaybookRules(): Observable<PlaybookRule[]> {
    return this.http.get<PlaybookRule[]>(`${environment.apiUrl}/playbook`);
  }
  createPlaybookRule(data: CreatePlaybookRule): Observable<PlaybookRule> {
    return this.http.post<PlaybookRule>(`${environment.apiUrl}/playbook`, data);
  }
  updatePlaybookRule(id: string, data: UpdatePlaybookRule): Observable<PlaybookRule> {
    return this.http.put<PlaybookRule>(`${environment.apiUrl}/playbook/${id}`, data);
  }
  deletePlaybookRule(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/playbook/${id}`);
  }
  reorderPlaybookRules(orderedIds: string[]): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/playbook/reorder`, orderedIds);
  }
  getChecklist(tradeId: string): Observable<TradeChecklistItem[]> {
    return this.http.get<TradeChecklistItem[]>(`${environment.apiUrl}/playbook/checklist/${tradeId}`);
  }
  saveChecklist(data: SaveChecklist): Observable<{ compliancePercent: number }> {
    return this.http.post<{ compliancePercent: number }>(`${environment.apiUrl}/playbook/checklist`, data);
  }

  // Chart Upload
  uploadChart(tradeId: string, file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${environment.apiUrl}/upload/chart?tradeId=${tradeId}`, formData);
  }
  getChartUrl(tradeId: string): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`${environment.apiUrl}/upload/chart/${tradeId}`);
  }
  deleteChart(tradeId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/upload/chart/${tradeId}`);
  }

  // AI Chat
  getAiSettings(): Observable<UserAiSettings> {
    return this.http.get<UserAiSettings>(`${environment.apiUrl}/ai/settings`);
  }
  saveAiSettings(data: SaveAiSettings): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/ai/settings`, data);
  }
  getAiSessions(): Observable<AiChatSession[]> {
    return this.http.get<AiChatSession[]>(`${environment.apiUrl}/ai/sessions`);
  }
  getAiSession(id: string): Observable<AiChatSession> {
    return this.http.get<AiChatSession>(`${environment.apiUrl}/ai/sessions/${id}`);
  }
  deleteAiSession(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/ai/sessions/${id}`);
  }

  // Backtest
  runBacktest(data: BacktestRequest): Observable<BacktestResult> {
    return this.http.post<BacktestResult>(`${environment.apiUrl}/backtest/run`, data);
  }
  getBacktestHistory(): Observable<BacktestResult[]> {
    return this.http.get<BacktestResult[]>(`${environment.apiUrl}/backtest`);
  }
  getBacktest(id: string): Observable<BacktestResult> {
    return this.http.get<BacktestResult>(`${environment.apiUrl}/backtest/${id}`);
  }
  deleteBacktest(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/backtest/${id}`);
  }
}
