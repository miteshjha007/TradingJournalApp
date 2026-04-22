import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { UserInfo, AdminCreateUser } from '../../models/models';

const ALL_SECTIONS = [
  { key: 'Dashboard', icon: '📊' },
  { key: 'Instruments', icon: '🎯' },
  { key: 'Trade Journal', icon: '📝' },
  { key: 'Analytics', icon: '🤖' },
  { key: 'Calendar', icon: '📅' },
  { key: 'Notes', icon: '📌' },
  { key: 'Risk Tool', icon: '⚖️' },
  { key: 'Alerts', icon: '🔔' },
];

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe],
  template: `
    <div class="page-wrapper">
      <div class="page-header">
        <div>
          <h1 class="page-title-h1">🛡️ Admin Panel</h1>
          <p class="page-desc">{{ users().length }} registered users</p>
        </div>
        <button class="btn btn-primary" (click)="openModal()">
          <span>➕</span> Create User
        </button>
      </div>

      @if (loading()) {
        <div class="loading-state"><div class="loading-spinner"></div></div>
      } @else {
        <div class="table-card">
          <table class="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Balance</th>
                <th>Allowed Sections</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (user of users(); track user.id) {
                <tr>
                  <td><strong>{{ user.firstName }} {{ user.lastName }}</strong></td>
                  <td>{{ user.email }}</td>
                  <td><span class="role-badge" [class.admin]="user.role === 'Admin'">{{ user.role }}</span></td>
                  <td>{{ user.accountBalance | currency }}</td>
                  <td>
                    @if (user.role === 'Admin') {
                      <span class="section-tag" style="background:rgba(250,180,0,.15);color:#fac800">All Sections</span>
                    } @else if (user.allowedSections?.length) {
                      @for (s of user.allowedSections; track s) {
                        <span class="section-tag">{{ s }}</span>
                      }
                    } @else {
                      <span style="color:var(--text-muted);font-size:.8rem">No sections assigned</span>
                    }
                  </td>
                  <td>
                    <button class="btn btn-sm btn-secondary" (click)="editUser(user)">Edit</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- Create User Modal -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()">
        <div class="modal-box" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>👤 {{ isEditMode() ? 'Edit User' : 'Create New User' }}</h2>
            <button class="close-btn" (click)="closeModal()">✕</button>
          </div>

          <div class="modal-body">
            <!-- Basic Info -->
            <div class="form-row">
              <div class="form-group">
                <label>First Name *</label>
                <input type="text" [(ngModel)]="form.firstName" placeholder="First name" />
              </div>
              <div class="form-group">
                <label>Last Name *</label>
                <input type="text" [(ngModel)]="form.lastName" placeholder="Last name" />
              </div>
            </div>
            <div class="form-group">
              <label>Email Address *</label>
              <input type="email" [(ngModel)]="form.email" placeholder="user@example.com" />
            </div>
            <div class="form-row">
              @if (!isEditMode()) {
              <div class="form-group">
                <label>Password *</label>
                <input type="password" [(ngModel)]="form.password" placeholder="Min 6 characters" />
              </div>
              }
              <div class="form-group">
                <label>Role</label>
                <select [(ngModel)]="form.role">
                  <option value="User">User</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
            </div>
            @if (isEditMode()) {
            <div class="form-group">
              <label>Account Balance</label>
              <input type="number" [(ngModel)]="form.accountBalance" placeholder="0.00" />
            </div>
            }

            <!-- Section Permissions -->
            <div class="permissions-section">
              <div class="permissions-header">
                <label>Section Permissions</label>
                <div class="perm-actions">
                  <button type="button" class="link-btn" (click)="selectAll()">Select All</button>
                  <span>·</span>
                  <button type="button" class="link-btn" (click)="clearAll()">Clear All</button>
                </div>
              </div>
              @if (form.role === 'Admin') {
                <p class="admin-note">⚡ Admin users automatically have access to all sections.</p>
              } @else {
                <div class="section-grid">
                  @for (section of allSections; track section.key) {
                    <label class="section-checkbox" [class.checked]="isSectionEnabled(section.key)">
                      <input type="checkbox"
                             [checked]="isSectionEnabled(section.key)"
                             (change)="toggleSection(section.key)" />
                      <span class="section-icon">{{ section.icon }}</span>
                      <span>{{ section.key }}</span>
                    </label>
                  }
                </div>
              }
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-secondary" (click)="closeModal()">Cancel</button>
            <button class="btn btn-primary" (click)="onSubmit()" [disabled]="saving()">
              {{ saving() ? 'Saving...' : (isEditMode() ? '✅ Update User' : '✅ Create User') }}
            </button>
          </div>
        </div>
      </div>
    }

    <style>
      .form-row { display: flex; gap: 1rem; }
      .form-group { flex: 1; display: flex; flex-direction: column; gap: 0.4rem; margin-bottom: 1rem; }
      .form-group label { font-size: 0.85rem; font-weight: 600; color: var(--text-secondary); }
      .form-group input, .form-group select { width: 100%; box-sizing: border-box; padding: 0.6rem; border: 1px solid var(--border); border-radius: 4px; background: var(--bg-surface); color: var(--text-main); font-size: 0.95rem; }
      .form-group input:focus, .form-group select:focus { border-color: var(--accent); outline: none; }
      .section-tag {
        display: inline-block;
        background: rgba(99,102,241,.12);
        color: var(--accent);
        border-radius: 4px;
        padding: 2px 8px;
        font-size: .75rem;
        margin: 2px;
        font-weight: 600;
      }
      .permissions-section {
        margin-top: 1rem;
        border-top: 1px solid var(--border);
        padding-top: 1rem;
      }
      .permissions-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: .75rem;
      }
      .permissions-header label {
        font-weight: 600;
        color: var(--text-secondary);
        font-size: .85rem;
        text-transform: uppercase;
        letter-spacing: .05em;
        margin: 0;
      }
      .perm-actions { display: flex; gap: .5rem; align-items: center; color: var(--text-muted); font-size: .8rem; }
      .link-btn {
        background: none; border: none; color: var(--accent);
        cursor: pointer; font-size: .8rem; padding: 0; text-decoration: underline;
      }
      .section-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        gap: .6rem;
      }
      .section-checkbox {
        display: flex; align-items: center; gap: .5rem;
        padding: .6rem .75rem; border-radius: 8px;
        border: 1.5px solid var(--border); cursor: pointer;
        transition: all .2s; font-size: .875rem;
        user-select: none;
      }
      .section-checkbox:hover { border-color: var(--accent); background: rgba(99,102,241,.05); }
      .section-checkbox.checked {
        border-color: var(--accent);
        background: rgba(99,102,241,.12);
        color: var(--accent); font-weight: 600;
      }
      .section-checkbox input[type=checkbox] { display: none; }
      .section-icon { font-size: 1rem; }
      .admin-note {
        background: rgba(250,180,0,.08); border: 1px solid rgba(250,180,0,.25);
        color: #c49c00; border-radius: 8px; padding: .6rem 1rem;
        font-size: .85rem; margin: 0;
      }
    </style>
  `
})
export class AdminComponent implements OnInit {
  users = signal<UserInfo[]>([]);
  loading = signal(true);
  showModal = signal(false);
  isEditMode = signal(false);
  saving = signal(false);
  allSections = ALL_SECTIONS;

  form: any = {
    id: '', firstName: '', lastName: '', email: '',
    password: '', role: 'User', accountBalance: 0, allowedSections: []
  };

  constructor(private api: ApiService, private toast: ToastService) { }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.api.getAdminUsers().subscribe({
      next: (data) => { this.users.set(data); this.loading.set(false); },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err.error?.error || 'Failed to load users.', 'Admin Error');
      }
    });
  }

  openModal(): void {
    this.isEditMode.set(false);
    this.form = { id: '', firstName: '', lastName: '', email: '', password: '', role: 'User', accountBalance: 0, allowedSections: [] };
    this.showModal.set(true);
  }

  editUser(user: UserInfo): void {
    this.isEditMode.set(true);
    this.form = {
      id: user.id,
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      role: user.role,
      accountBalance: user.accountBalance,
      allowedSections: [...user.allowedSections]
    };
    this.showModal.set(true);
  }

  closeModal(): void { this.showModal.set(false); }

  isSectionEnabled(section: string): boolean {
    return this.form.allowedSections.includes(section);
  }

  toggleSection(section: string): void {
    const idx = this.form.allowedSections.indexOf(section);
    if (idx >= 0) {
      this.form.allowedSections = this.form.allowedSections.filter((s: string) => s !== section);
    } else {
      this.form.allowedSections = [...this.form.allowedSections, section];
    }
  }

  selectAll(): void {
    this.form.allowedSections = this.allSections.map(s => s.key);
  }

  clearAll(): void {
    this.form.allowedSections = [];
  }

  onSubmit(): void {
    if (!this.form.firstName || !this.form.lastName || !this.form.email) {
      this.toast.warning('Please fill in all required fields.', 'Validation');
      return;
    }
    if (!this.isEditMode() && (!this.form.password || this.form.password.length < 6)) {
      this.toast.warning('Password must be at least 6 characters.', 'Validation');
      return;
    }

    this.saving.set(true);

    if (this.isEditMode()) {
      const payload = {
        firstName: this.form.firstName,
        lastName: this.form.lastName,
        email: this.form.email,
        role: this.form.role,
        accountBalance: this.form.accountBalance,
        allowedSections: this.form.role === 'Admin' ? [] : this.form.allowedSections
      };

      this.api.adminUpdateUser(this.form.id, payload).subscribe({
        next: (user) => {
          this.saving.set(false);
          this.closeModal();
          this.loadUsers();
          this.toast.success(`User "${user.firstName} ${user.lastName}" updated successfully!`, 'User Updated');
        },
        error: (err) => {
          this.saving.set(false);
          this.toast.error(err.error?.error || 'Failed to update user.', 'Error');
        }
      });
    } else {
      const payload: AdminCreateUser = {
        firstName: this.form.firstName,
        lastName: this.form.lastName,
        email: this.form.email,
        password: this.form.password,
        role: this.form.role,
        allowedSections: this.form.role === 'Admin' ? [] : this.form.allowedSections
      };

      this.api.adminCreateUser(payload).subscribe({
        next: (user) => {
          this.saving.set(false);
          this.closeModal();
          this.loadUsers();
          this.toast.success(`User "${user.firstName} ${user.lastName}" created successfully!`, 'User Created');
        },
        error: (err) => {
          this.saving.set(false);
          this.toast.error(err.error?.error || 'Failed to create user.', 'Error');
        }
      });
    }
  }
}
