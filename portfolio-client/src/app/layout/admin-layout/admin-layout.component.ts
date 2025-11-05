import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  template: `
    <div class="admin-layout">
      <aside class="admin-sidebar">
        <div class="sidebar-header">
          <h2>Admin Panel</h2>
        </div>
        <nav class="sidebar-nav">
          <a routerLink="/admin/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/admin/projects" routerLinkActive="active">Projects</a>
          <a routerLink="/admin/blog" routerLinkActive="active">Blog Posts</a>
          <a routerLink="/admin/messages" routerLinkActive="active">Messages</a>
          <a routerLink="/admin/settings" routerLinkActive="active">Settings</a>
        </nav>
      </aside>

      <main class="admin-main">
        <header class="admin-header">
          <div class="header-actions">
            <button class="logout-btn" (click)="logout()">Logout</button>
          </div>
        </header>

        <div class="admin-content">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styleUrls: ['./admin-layout.component.scss']
})
export class AdminLayoutComponent {
  logout(): void {
    // TODO: Implement logout logic
    console.log('Logout clicked');
  }
}