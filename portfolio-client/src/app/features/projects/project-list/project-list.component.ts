import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { ProjectsService } from '../services/projects.service';
import { Project } from '../../../core/models/project.model';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CardComponent } from '../../../shared/components/ui/card/card.component';
import { ButtonComponent } from '../../../shared/components/ui/button/button.component';
import { TruncatePipe } from '../../../shared/pipes/truncate.pipe';

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [CommonModule, RouterModule, CardComponent, ButtonComponent, TruncatePipe],
  template: `
    <div class="project-list">
      <div class="projects-grid" *ngIf="projects$ | async as projectsResponse">
        <app-card
          *ngFor="let project of projectsResponse.data"
          class="project-card"
          routerLink="/projects/{{ project.id }}"
        >
          <div card-header>
            <h3 class="project-title">{{ project.title }}</h3>
          </div>
          <div class="project-content">
            <p class="project-description">{{ project.description | truncate:150 }}</p>
            <div class="project-tech">
              <span
                *ngFor="let tech of project.technologies.slice(0, 4)"
                class="tech-tag"
              >
                {{ tech }}
              </span>
            </div>
            <div class="project-actions">
              <app-button
                variant="outline"
                size="sm"
                *ngIf="project.githubUrl"
                (click)="$event.stopPropagation(); openLink(project.githubUrl!)"
              >
                GitHub
              </app-button>
              <app-button
                variant="primary"
                size="sm"
                *ngIf="project.liveUrl"
                (click)="$event.stopPropagation(); openLink(project.liveUrl!)"
              >
                Live Demo
              </app-button>
            </div>
          </div>
        </app-card>
      </div>

      <div class="loading-state" *ngIf="!(projects$ | async)">
        <p>Loading projects...</p>
      </div>

      <div class="empty-state" *ngIf="(projects$ | async)?.data?.length === 0">
        <p>No projects found matching your criteria.</p>
      </div>
    </div>
  `,
  styleUrls: ['./project-list.component.scss']
})
export class ProjectListComponent implements OnInit {
  private projectsService = inject(ProjectsService);

  projects$: Observable<ApiResponse<Project[]>> | null = null;

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(filter?: any): void {
    this.projects$ = this.projectsService.getProjects(filter);
  }

  openLink(url: string): void {
    window.open(url, '_blank');
  }
}