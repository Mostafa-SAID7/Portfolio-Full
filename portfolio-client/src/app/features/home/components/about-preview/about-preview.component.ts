import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/ui/button/button.component';

@Component({
  selector: 'app-about-preview',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonComponent],
  template: `
    <section class="about-preview">
      <div class="container">
        <div class="about-content">
          <div class="about-text">
            <h2>About Me</h2>
            <p>
              I'm a passionate full-stack developer with 5+ years of experience creating
              web applications. I specialize in modern JavaScript frameworks and love
              turning complex problems into simple, beautiful designs.
            </p>
            <p>
              When I'm not coding, you can find me exploring new technologies,
              contributing to open-source projects, or sharing knowledge with the developer community.
            </p>
            <app-button variant="secondary" routerLink="/about">
              Learn More About Me
            </app-button>
          </div>
          <div class="about-stats">
            <div class="stat">
              <div class="stat-number">50+</div>
              <div class="stat-label">Projects Completed</div>
            </div>
            <div class="stat">
              <div class="stat-number">5+</div>
              <div class="stat-label">Years Experience</div>
            </div>
            <div class="stat">
              <div class="stat-number">20+</div>
              <div class="stat-label">Happy Clients</div>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
  styleUrls: ['./about-preview.component.scss']
})
export class AboutPreviewComponent {}