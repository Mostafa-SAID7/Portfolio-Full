import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Experience } from '../../../../core/models/experience.model';
import { CardComponent } from '../../../../shared/components/ui/card/card.component';

@Component({
  selector: 'app-experience-timeline',
  standalone: true,
  imports: [CommonModule, CardComponent],
  template: `
    <section class="experience-timeline">
      <div class="container">
        <h2 class="section-title">Experience & Education</h2>
        <div class="timeline">
          <div
            *ngFor="let item of timelineItems; let i = index"
            class="timeline-item"
            [class.left]="i % 2 === 0"
            [class.right]="i % 2 !== 0"
          >
            <div class="timeline-marker"></div>
            <app-card class="timeline-content">
              <div class="experience-header">
                <h3 class="position">{{ item.position }}</h3>
                <h4 class="company">{{ item.company }}</h4>
                <div class="experience-meta">
                  <span class="date-range">
                    {{ item.startDate | date:'MMM yyyy' }}
                    {{ item.endDate ? ' - ' + (item.endDate | date:'MMM yyyy') : ' - Present' }}
                  </span>
                  <span class="location" *ngIf="item.location">{{ item.location }}</span>
                </div>
              </div>
              <p class="experience-description">{{ item.description }}</p>
              <div class="experience-tech">
                <span
                  *ngFor="let tech of item.technologies"
                  class="tech-tag"
                >
                  {{ tech }}
                </span>
              </div>
            </app-card>
          </div>
        </div>
      </div>
    </section>
  `,
  styleUrls: ['./experience-timeline.component.scss']
})
export class ExperienceTimelineComponent {
  timelineItems: Experience[] = [
    {
      id: 1,
      company: 'Tech Solutions Inc.',
      position: 'Senior Full Stack Developer',
      startDate: new Date('2022-01-01'),
      description: 'Led development of enterprise web applications using modern JavaScript frameworks. Mentored junior developers and implemented best practices for code quality and performance.',
      technologies: ['React', 'Node.js', 'TypeScript', 'AWS', 'MongoDB'],
      location: 'San Francisco, CA',
      current: true
    },
    {
      id: 2,
      company: 'Digital Innovations Ltd.',
      position: 'Full Stack Developer',
      startDate: new Date('2020-03-01'),
      endDate: new Date('2021-12-31'),
      description: 'Developed and maintained multiple client projects using Angular and .NET. Collaborated with design team to implement responsive user interfaces and RESTful APIs.',
      technologies: ['Angular', 'C#', '.NET', 'SQL Server', 'Azure'],
      location: 'New York, NY',
      current: false
    },
    {
      id: 3,
      company: 'StartupXYZ',
      position: 'Frontend Developer',
      startDate: new Date('2019-06-01'),
      endDate: new Date('2020-02-28'),
      description: 'Built responsive web applications for a fast-growing startup. Worked closely with product team to deliver features that improved user engagement by 40%.',
      technologies: ['Vue.js', 'JavaScript', 'Firebase', 'SCSS'],
      location: 'Austin, TX',
      current: false
    },
    {
      id: 4,
      company: 'University of Technology',
      position: 'Bachelor of Computer Science',
      startDate: new Date('2015-09-01'),
      endDate: new Date('2019-05-31'),
      description: 'Graduated with honors. Specialized in software engineering and web development. Completed capstone project on scalable web architectures.',
      technologies: ['Java', 'Python', 'JavaScript', 'SQL'],
      location: 'Boston, MA',
      current: false
    }
  ];
}