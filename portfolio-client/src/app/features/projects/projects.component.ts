import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SeoService } from '../../core/services/seo.service';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="projects-page">
      <div class="container">
        <header class="page-header">
          <h1 class="page-title">My Projects</h1>
          <p class="page-description">
            A collection of projects I've worked on, showcasing my skills and experience.
          </p>
        </header>

        <app-project-filter></app-project-filter>

        <app-project-list></app-project-list>
      </div>
    </div>
  `,
  styleUrls: ['./projects.component.scss']
})
export class ProjectsComponent {
  private seoService = inject(SeoService);

  ngOnInit(): void {
    this.seoService.setTitle('Projects - Portfolio');
    this.seoService.setMetaDescription('Explore my portfolio of web development projects, including full-stack applications, responsive designs, and modern technologies.');
  }
}