import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { Note, CreateNote, NoteFilter } from '../../models/models';

@Component({
  selector: 'app-notes',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, DatePipe],
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">Notes</h1>
          <p class="page-desc">{{ pagedNotes().totalCount }} notes</p>
        </div>
        <button class="btn btn-primary" (click)="openModal()">+ Add Note</button>
      </div>

      <!-- Search -->
      <div class="search-bar">
        <input type="text" [(ngModel)]="searchTerm" placeholder="🔍 Search notes..." class="form-input search-input" (input)="onSearch()" />
      </div>

      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
        <div class="notes-grid">
          @for (note of pagedNotes().notes; track note.id) {
            <div class="note-card" [class.pinned]="note.isPinned">
              @if (note.isPinned) { <div class="pin-badge">📌 Pinned</div> }
              <h3 class="note-title">{{ note.title }}</h3>
              <p class="note-content">{{ note.content | slice:0:180 }}{{ note.content.length > 180 ? '...' : '' }}</p>
              @if (note.tags) {
                <div class="note-tags">
                  @for (tag of note.tags.split(','); track tag) {
                    <span class="tag">{{ tag.trim() }}</span>
                  }
                </div>
              }
              <div class="note-footer">
                <span class="note-date">{{ note.createdAt | date:'dd MMM yyyy' }}</span>
                <div class="note-actions">
                  <button class="btn-icon" (click)="openModal(note)">✏️</button>
                  <button class="btn-icon danger" (click)="deleteNote(note.id)">🗑️</button>
                </div>
              </div>
            </div>
          }
          @empty {
            <div class="empty-state full-width">
              <span class="empty-icon">📌</span>
              <h3>No notes yet</h3>
              <p>Capture your trading insights and strategies</p>
              <button class="btn btn-primary" (click)="openModal()">Write Your First Note</button>
            </div>
          }
        </div>

        @if (pagedNotes().totalPages > 1) {
          <div class="pagination">
            <button class="btn btn-ghost" [disabled]="page <= 1" (click)="goToPage(page - 1)">← Prev</button>
            <span class="page-info">Page {{ page }} of {{ pagedNotes().totalPages }}</span>
            <button class="btn btn-ghost" [disabled]="page >= pagedNotes().totalPages" (click)="goToPage(page + 1)">Next →</button>
          </div>
        }
      }

      <!-- Note Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ editingId() ? 'Edit' : 'Add' }} Note</h3>
              <button class="modal-close" (click)="closeModal()">✕</button>
            </div>
            <form [formGroup]="form" (ngSubmit)="onSubmit()" class="modal-body">
              <div class="form-group">
                <label>Title *</label>
                <input type="text" formControlName="title" placeholder="Note title..." class="form-input" />
              </div>
              <div class="form-group">
                <label>Content *</label>
                <textarea formControlName="content" rows="8" placeholder="Write your note..." class="form-input"></textarea>
              </div>
              <div class="form-group">
                <label>Tags</label>
                <input type="text" formControlName="tags" placeholder="Strategy, Review (comma-separated)" class="form-input" />
              </div>
              <div class="form-group checkbox-group">
                <label class="checkbox-label">
                  <input type="checkbox" formControlName="isPinned" />
                  <span>Pin this note</span>
                </label>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-ghost" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">
                  @if (saving()) { <span class="spinner"></span> }
                  {{ editingId() ? 'Update' : 'Save' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `
})
export class NotesComponent implements OnInit {
  pagedNotes = signal({ notes: [] as Note[], totalCount: 0, page: 1, pageSize: 12, totalPages: 0 });
  loading = signal(true);
  showModal = signal(false);
  editingId = signal<string | null>(null);
  saving = signal(false);
  page = 1;
  searchTerm = '';
  private searchTimeout: any;

  form;

  constructor(private api: ApiService, private fb: FormBuilder, private toast: ToastService) {
    this.form = this.fb.group({
      title: ['', Validators.required],
      content: ['', Validators.required],
      tags: [''],
      isPinned: [false]
    });
  }
  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.api.getNotes({ page: this.page, pageSize: 12, searchTerm: this.searchTerm || undefined }).subscribe({
      next: (data) => { this.pagedNotes.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => { this.page = 1; this.load(); }, 400);
  }

  goToPage(page: number): void { this.page = page; this.load(); }

  openModal(note?: Note): void {
    if (note) {
      this.editingId.set(note.id);
      this.form.patchValue({ title: note.title, content: note.content, tags: note.tags || '', isPinned: note.isPinned });
    } else {
      this.editingId.set(null);
      this.form.reset({ isPinned: false });
    }
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const data = this.form.value as CreateNote;
    const obs = this.editingId()
      ? this.api.updateNote(this.editingId()!, data)
      : this.api.createNote(data);
    obs.subscribe({ 
      next: () => { 
        this.closeModal(); 
        this.load(); 
        this.saving.set(false); 
        this.toast.success(`Note ${this.editingId() ? 'updated' : 'created'} successfully!`, 'Success');
      }, 
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err.error?.error || 'Failed to save note.', 'Error');
      } 
    });
  }

  deleteNote(id: string): void {
    if (!confirm('Delete this note?')) return;
    this.api.deleteNote(id).subscribe({
      next: () => {
        this.load();
        this.toast.success('Note deleted successfully.', 'Deleted');
      },
      error: (err) => this.toast.error(err.error?.error || 'Failed to delete note.', 'Error')
    });
  }
}
