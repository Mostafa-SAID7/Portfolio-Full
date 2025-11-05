import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { ProjectsService } from '../services/projects.service';
import { Project } from '../../../core/models/project.model';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CardComponent } from '../../../shared/components/ui/card/card.component';
import { ButtonComponent } from '../../../shared/components/ui/button/button.component';
import { SeoService } from '../../../core/services/seo.service';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, CardComponent, ButtonComponent],
  template: `
    <div class="project-detail" *ngIf="project$ | async as projectResponse">
      <div class="container" *ngIf="projectResponse.data as project">
        <div class="project-header">
          <button class="back-button" (click)="goBack()">
            ← Back to Projects
          </button>
          <h1 class="project-title">{{ project.title }}</h1>
          <div class="project-meta">
            <span class="project-date">
              {{ project.createdAt | date:'mediumDate' }}
            </span>
            <span class="project-featured" *ngIf="project.featured">
              ⭐ Featured
            </span>
          </div>
        </div>

        <div class="project-content">
          <div class="project-image" *ngIf="project.imageUrl">
            <img [src]="project.imageUrl" [alt]="project.title" />
          </div>

          <app-card class="project-description-card">
            <div card-header>
              <h2>Project Description</h2>
            </div>
            <p class="project-description">{{ project.description }}</p>
          </app-card>

          <app-card class="project-tech-card">
            <div card-header>
              <h2>Technologies Used</h2>
            </div>
            <div class="tech-stack">
              <span
                *ngFor="let tech of project.technologies"
                class="tech-tag"
              >
                {{ tech }}
              </span>
            </div>
          </app-card>

          <app-card class="project-links-card">
            <div card-header>
              <h2>Project Links</h2>
            </div>
            <div class="project-links">
              <app-button
                variant="outline"
                *ngIf="project.githubUrl"
                (click)="openLink(project.githubUrl!)"
              >
                View on GitHub
              </app-button>
              <app-button
                variant="primary"
                *ngIf="project.liveUrl"
                (click)="openLink(project.liveUrl!)"
              >
                Live Demo
              </app-button>
            </div>
          </app-card>
        </div>
      </div>

      <div class="loading-state" *ngIf="!projectResponse.data">
        <p>Loading project details...</p>
      </div>
    </div>
  `,
  styleUrls: ['./project-detail.component.scss']
})
export class ProjectDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private projectsService = inject(ProjectsService);
  private seoService = inject(SeoService);

  project$: Observable<ApiResponse<Project>> | null = null;

  ngOnInit(): void {
    this.project$ = this.route.params.pipe(
      switchMap(params => {
        const id = +params['id'];
        return this.projectsService.getProjectById(id);
      })
    );

    // Update SEO when project loads
    this.project$.subscribe(response => {
      if (response.data) {
        this.seoService.setTitle(`${response.data.title} - Portfolio`);
        this.seoService.setMetaDescription(response.data.description);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/projects']);
  }

  openLink(url: string): void {
    window.open(url, '_blank');
  }
}