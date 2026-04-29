import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, Megaphone, MessageSquare, MessagesSquare } from 'lucide-angular';
import { ChatService } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { AnnouncementsComponent } from './announcements/announcements.component';
import { PublicForumComponent } from './public-forum/public-forum.component';
import { DirectMessagesComponent } from './direct-messages/direct-messages.component';

@Component({
  selector: 'app-forum',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, AnnouncementsComponent, PublicForumComponent, DirectMessagesComponent],
  templateUrl: './forum.component.html',
  styleUrls: ['./forum.component.css']
})
export class ForumComponent implements OnInit, OnDestroy {
  activeTab: 'announcements' | 'public' | 'dm' = 'public';

  // Expose icons for template
  Megaphone = Megaphone;
  MessageSquare = MessageSquare;
  MessagesSquare = MessagesSquare;

  constructor(
    public chatService: ChatService,
    private authService: AuthService
  ) { }

  ngOnInit() {
    const token = this.authService.getToken();
    if (token) {
      this.chatService.startConnection(token);
    }
  }

  ngOnDestroy() {
    this.chatService.stopConnection();
  }

  setTab(tab: 'announcements' | 'public' | 'dm') {
    this.activeTab = tab;
  }
}
