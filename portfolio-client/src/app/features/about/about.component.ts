import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SeoService } from '../../core/services/seo.service';
import { SkillsSectionComponent } from './components/skills-section/skills-section.component';
import { ExperienceTimelineComponent } from './components/experience-timeline/experience-timeline.component';
import { TestimonialsComponent } from './components/testimonials/testimonials.component';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, SkillsSectionComponent, ExperienceTimelineComponent, TestimonialsComponent],
  template: `
    <div class="about-page">
      <div class="container">
        <header class="page-header">
          <h1 class="page-title">About Me</h1>
          <p class="page-subtitle">Get to know the person behind the code</p>
        </header>

        <app-skills-section></app-skills-section>
        <app-experience-timeline></app-experience-timeline>
        <app-testimonials></app-testimonials>
      </div>
    </div>
  `,
  styleUrls: ['./about.component.scss']
})
export class AboutComponent {
  private seoService = inject(SeoService);

  ngOnInit(): void {
    this.seoService.setTitle('About - Portfolio');
    this.seoService.setMetaDescription('Learn more about my background, skills, experience, and journey as a full-stack developer.');
  }
}