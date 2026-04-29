import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Send, Trash2, MessageSquare, Reply } from 'lucide-angular';
import { ApiService } from '../../../services/api.service';
import { ChatService } from '../../../services/chat.service';
import { AuthService } from '../../../services/auth.service';
import { ChannelType, CreateForumMessage, ForumMessage } from '../../../models/models';

@Component({
  selector: 'app-public-forum',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './public-forum.component.html'
})
export class PublicForumComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  currentUserId: string = '';
  isAdmin = false;
  loading = false;
  messageContent = '';
  replyingTo: ForumMessage | null = null;
  private shouldScroll = true;

  // Icons
  Send = Send;
  Trash2 = Trash2;
  MessageSquare = MessageSquare;
  Reply = Reply;

  constructor(
    public chatService: ChatService,
    private apiService: ApiService,
    private authService: AuthService
  ) { }

  ngOnInit() {
    this.currentUserId = this.authService.currentUser()?.id || '';
    this.isAdmin = this.authService.isAdmin();
    this.loadMessages();
  }

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  loadMessages() {
    this.loading = true;
    this.apiService.getPublicForumMessages().subscribe({
      next: (res) => {
        // Sort chronologically for chat view
        const sorted = res.messages.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
        this.chatService.activeForumMessages.set(sorted);
        this.loading = false;
        this.shouldScroll = true;
      },
      error: () => this.loading = false
    });
  }

  sendMessage() {
    if (!this.messageContent.trim()) return;

    const dto: CreateForumMessage = {
      content: this.messageContent,
      channelType: ChannelType.PublicForum,
      parentMessageId: this.replyingTo?.id
    };

    this.apiService.postForumMessage(dto).subscribe({
      next: () => {
        this.messageContent = '';
        this.replyingTo = null;
        this.shouldScroll = true;
      }
    });
  }

  deleteMessage(id: string) {
    if (confirm('Delete this message?')) {
      this.apiService.deleteForumMessage(id).subscribe({
        next: () => {
          this.chatService.activeForumMessages.update(list => list.filter(m => m.id !== id));
        }
      });
    }
  }

  setReply(msg: ForumMessage | null) {
    this.replyingTo = msg;
  }

  private scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch (err) { }
  }

  handleKeydown(event: Event): void {
    const ke = event as KeyboardEvent;
    if (!ke.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
