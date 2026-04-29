import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { Announcement, ForumMessage, DirectMessage } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private hubConnection: signalR.HubConnection | null = null;
  private hubUrl = environment.apiUrl.replace('/api', '/hubs/chat');

  // Signals for UI to bind to
  public activeAnnouncements = signal<Announcement[]>([]);
  public activeForumMessages = signal<ForumMessage[]>([]);
  public activeDirectMessages = signal<DirectMessage[]>([]);
  public globalUnreadCount = signal<number>(0);

  // Optional callback for new DM notifications (used by shell)
  private newDMCallback: ((dm: DirectMessage) => void) | null = null;

  constructor() {}

  /** Register a listener for when new DMs arrive via SignalR */
  public onNewDirectMessage(callback: (dm: DirectMessage) => void): void {
    this.newDMCallback = callback;
  }

  public startConnection(token: string): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubUrl}?access_token=${token}`)
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .build();

    this.hubConnection.start()
      .then(() => console.log('[ChatHub] Connected'))
      .catch(err => console.error('[ChatHub] Error:', err));

    this.registerListeners();
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  private registerListeners(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveAnnouncement', (dto: Announcement) => {
      this.activeAnnouncements.update(list => [dto, ...list]);
    });

    this.hubConnection.on('ReceiveForumMessage', (dto: ForumMessage) => {
      this.activeForumMessages.update(list => [...list, dto]);
    });

    this.hubConnection.on('ReceiveDirectMessage', (dto: DirectMessage) => {
      this.activeDirectMessages.update(list => [...list, dto]);
      // Only increment unread if it's an incoming message (not from self)
      this.globalUnreadCount.update(c => c + 1);
      // Notify the shell for the notification bell popup
      if (this.newDMCallback) {
        this.newDMCallback(dto);
      }
    });

    this.hubConnection.on('MessageDeleted', (id: string) => {
      this.activeForumMessages.update(list => list.filter(m => m.id !== id));
    });

    this.hubConnection.onreconnecting(() => {
      console.warn('[ChatHub] Reconnecting...');
    });

    this.hubConnection.onreconnected(() => {
      console.log('[ChatHub] Reconnected successfully');
    });
  }
}
