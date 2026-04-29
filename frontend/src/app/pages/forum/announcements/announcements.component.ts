import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Plus, Trash2, Megaphone } from 'lucide-angular';
import { ApiService } from '../../../services/api.service';
import { ChatService } from '../../../services/chat.service';
import { AuthService } from '../../../services/auth.service';
import { Announcement, AnnouncementPriority, CreateAnnouncement } from '../../../models/models';

@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './announcements.component.html'
})
export class AnnouncementsComponent implements OnInit {
  isAdmin = false;
  showModal = false;
  loading = false;
  
  newAnnouncement: CreateAnnouncement = {
    title: '',
    content: '',
    priority: AnnouncementPriority.Info
  };
  
  // Expose enum for template
  AnnouncementPriority = AnnouncementPriority;
  
  // Icons
  Plus = Plus;
  Trash2 = Trash2;
  Megaphone = Megaphone;

  constructor(
    public chatService: ChatService,
    private apiService: ApiService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.isAdmin = this.authService.isAdmin();
    this.loadAnnouncements();
  }

  loadAnnouncements() {
    this.loading = true;
    this.apiService.getAnnouncements().subscribe({
      next: (res) => {
        this.chatService.activeAnnouncements.set(res.announcements);
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  postAnnouncement() {
    if (!this.newAnnouncement.title || !this.newAnnouncement.content) return;
    
    this.apiService.createAnnouncement(this.newAnnouncement).subscribe({
      next: () => {
        this.closeModal();
      },
      error: (err) => console.error('Failed to post announcement', err)
    });
  }

  deleteAnnouncement(id: string) {
    if (confirm('Are you sure you want to delete this announcement?')) {
      this.apiService.deleteAnnouncement(id).subscribe({
        next: () => {
          this.chatService.activeAnnouncements.update(list => list.filter(a => a.id !== id));
        }
      });
    }
  }

  openModal() {
    this.newAnnouncement = { title: '', content: '', priority: AnnouncementPriority.Info };
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }
}
