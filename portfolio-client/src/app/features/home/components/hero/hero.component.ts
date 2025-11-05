import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/ui/button/button.component';

@Component({
  selector: 'app-hero',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonComponent],
  template: `
    <section class="hero">
      <div class="hero-content">
        <h1 class="hero-title">Hi, I'm John Doe</h1>
        <p class="hero-subtitle">Full Stack Developer & UI/UX Designer</p>
        <p class="hero-description">
          I create beautiful, functional web applications with modern technologies.
          Passionate about clean code and great user experiences.
        </p>
        <div class="hero-actions">
          <app-button variant="primary" size="lg" routerLink="/projects">
            View My Work
          </app-button>
          <app-button variant="outline" size="lg" routerLink="/contact">
            Get In Touch
          </app-button>
        </div>
      </div>
      <div class="hero-image">
        <div class="profile-placeholder">
          <!-- Profile image would go here -->
        </div>
      </div>
    </section>
  `,
  styleUrls: ['./hero.component.scss']
})
export class HeroComponent {}