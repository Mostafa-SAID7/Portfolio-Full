import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <header class="header">
      <nav class="nav">
        <div class="nav-brand">
          <a routerLink="/" class="brand-link">
            <span class="brand-text">Portfolio</span>
          </a>
        </div>
        <div class="nav-menu">
          <a routerLink="/" class="nav-link" routerLinkActive="active">Home</a>
          <a routerLink="/projects" class="nav-link" routerLinkActive="active">Projects</a>
          <a routerLink="/about" class="nav-link" routerLinkActive="active">About</a>
          <a routerLink="/contact" class="nav-link" routerLinkActive="active">Contact</a>
          <a routerLink="/blog" class="nav-link" routerLinkActive="active">Blog</a>
        </div>
        <div class="nav-actions">
          <button class="theme-toggle" (click)="toggleTheme()">
            <svg *ngIf="!isDarkMode" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
            </svg>
            <svg *ngIf="isDarkMode" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="5"></circle>
              <line x1="12" y1="1" x2="12" y2="3"></line>
              <line x1="12" y1="21" x2="12" y2="23"></line>
              <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line>
              <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line>
              <line x1="1" y1="12" x2="3" y2="12"></line>
              <line x1="21" y1="12" x2="23" y2="12"></line>
              <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line>
              <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>
            </svg>
          </button>
        </div>
      </nav>
    </header>
  `,
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  isDarkMode = false;

  toggleTheme(): void {
    this.isDarkMode = !this.isDarkMode;
    document.documentElement.classList.toggle('dark', this.isDarkMode);
  }
}