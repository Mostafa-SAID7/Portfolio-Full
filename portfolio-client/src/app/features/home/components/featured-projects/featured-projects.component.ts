import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/ui/button/button.component';
import { CardComponent } from '../../../../shared/components/ui/card/card.component';
import { Project } from '../../../../core/models/project.model';

@Component({
  selector: 'app-featured-projects',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonComponent, CardComponent],
  template: `
    <section class="featured-projects">
      <div class="container">
        <h2 class="section-title">Featured Projects</h2>
        <div class="projects-grid">
          <app-card
            *ngFor="let project of featuredProjects"
            class="project-card"
            routerLink="/projects/{{ project.id }}"
          >
            <div card-header>
              <h3 class="project-title">{{ project.title }}</h3>
            </div>
            <div class="project-content">
              <p class="project-description">{{ project.description }}</p>
              <div class="project-tech">
                <span
                  *ngFor="let tech of project.technologies.slice(0, 3)"
                  class="tech-tag"
                >
                  {{ tech }}
                </span>
              </div>
            </div>
          </app-card>
        </div>
        <div class="view-all">
          <app-button variant="outline" routerLink="/projects">
            View All Projects
          </app-button>
        </div>
      </div>
    </section>
  `,
  styleUrls: ['./featured-projects.component.scss']
})
export class FeaturedProjectsComponent {
  // Mock data - in real app, this would come from a service
  featuredProjects: Project[] = [
    {
      id: 1,
      title: 'E-Commerce Platform',
      description: 'A full-stack e-commerce solution with React, Node.js, and MongoDB.',
      imageUrl: '/assets/images/project1.jpg',
      technologies: ['React', 'Node.js', 'MongoDB', 'Express'],
      featured: true,
      createdAt: new Date(),
      updatedAt: new Date()
    },
    {
      id: 2,
      title: 'Task Management App',
      description: 'A collaborative task management application with real-time updates.',
      imageUrl: '/assets/images/project2.jpg',
      technologies: ['Angular', 'Firebase', 'RxJS'],
      featured: true,
      createdAt: new Date(),
      updatedAt: new Date()
    },
    {
      id: 3,
      title: 'Weather Dashboard',
      description: 'A responsive weather dashboard with location-based forecasts.',
      imageUrl: '/assets/images/project3.jpg',
      technologies: ['Vue.js', 'OpenWeather API', 'Chart.js'],
      featured: true,
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];
}