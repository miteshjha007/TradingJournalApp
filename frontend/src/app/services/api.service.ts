import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Instrument, CreateInstrument,
  Trade, CreateTrade, TradeFilter, PagedTrades,
  Note, CreateNote, NoteFilter, PagedNotes,
  DashboardSummary, CalendarDay, PerformanceMetrics, DrawdownInfo,
  AiAnalysis, RiskCalculation, RiskResult, Alert,
  UserInfo, AdminCreateUser
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

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
  getAlert(): Observable<Alert> {
    return this.http.get<Alert>(`${environment.apiUrl}/dashboard/alerts`);
  }
  upsertAlert(data: Alert): Observable<Alert> {
    return this.http.post<Alert>(`${environment.apiUrl}/dashboard/alerts`, data);
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
}
