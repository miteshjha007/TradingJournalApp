import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Send, User as UserIcon, MessageSquare } from 'lucide-angular';
import { ApiService } from '../../../services/api.service';
import { ChatService } from '../../../services/chat.service';
import { AuthService } from '../../../services/auth.service';
import { ChannelType, CreateForumMessage, DirectMessage } from '../../../models/models';

interface ChatUser {
  id: string;
  name: string;
  email?: string;
  unreadCount?: number;
}

@Component({
  selector: 'app-direct-messages',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './direct-messages.component.html'
})
export class DirectMessagesComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  currentUserId: string = '';
  loading = false;
  messageContent = '';
  
  users: ChatUser[] = [];
  selectedUser: ChatUser | null = null;
  private shouldScroll = true;

  // Icons
  Send = Send;
  UserIcon = UserIcon;
  MessageSquare = MessageSquare;

  constructor(
    public chatService: ChatService,
    private apiService: ApiService,
    private authService: AuthService
  ) {
    // Trigger scroll whenever new DMs arrive via SignalR
    effect(() => {
      chatService.activeDirectMessages(); // track signal
      this.shouldScroll = true;
    });
  }

  ngOnInit() {
    this.currentUserId = this.authService.currentUser()?.id || '';
    this.loadUsers();
  }

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  loadUsers() {
    this.apiService.getForumUsers().subscribe({
      next: (res) => {
        this.users = res.map(u => ({ id: u.id, name: u.name, email: u.email }));
      }
    });
  }

  selectUser(user: ChatUser) {
    this.selectedUser = user;
    this.loading = true;
    
    // Mark as read
    this.apiService.markAsRead(user.id).subscribe();
    this.chatService.globalUnreadCount.set(0); // Assuming we read all for now, to refine later

    this.apiService.getDirectMessages(user.id).subscribe({
      next: (messages) => {
        this.chatService.activeDirectMessages.set(messages);
        this.loading = false;
        this.shouldScroll = true;
      },
      error: () => this.loading = false
    });
  }

  sendMessage() {
    if (!this.messageContent.trim() || !this.selectedUser) return;

    const dto: CreateForumMessage = {
      content: this.messageContent,
      channelType: ChannelType.DirectMessage,
      receiverId: this.selectedUser.id
    };

    this.apiService.sendDirectMessage(dto).subscribe({
      next: () => {
        this.messageContent = '';
        this.shouldScroll = true;
      }
    });
  }

  private scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch(err) { }
  }

  handleKeydown(event: Event): void {
    const ke = event as KeyboardEvent;
    if (!ke.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
