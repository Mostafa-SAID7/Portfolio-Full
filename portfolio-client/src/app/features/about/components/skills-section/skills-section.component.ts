import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Skill, SkillCategory } from '../../../../core/models/skill.model';
import { CardComponent } from '../../../../shared/components/ui/card/card.component';

@Component({
  selector: 'app-skills-section',
  standalone: true,
  imports: [CommonModule, CardComponent],
  template: `
    <section class="skills-section">
      <div class="container">
        <h2 class="section-title">Skills & Technologies</h2>
        <div class="skills-grid">
          <app-card
            *ngFor="let category of skillCategories"
            class="skill-category-card"
            [header]="true"
          >
            <div card-header>
              <h3 class="category-title">{{ category.name }}</h3>
            </div>
            <div class="skills-list">
              <div
                *ngFor="let skill of category.skills"
                class="skill-item"
              >
                <div class="skill-info">
                  <span class="skill-name">{{ skill.name }}</span>
                  <span class="skill-proficiency">{{ skill.proficiency }}%</span>
                </div>
                <div class="skill-bar">
                  <div
                    class="skill-progress"
                    [style.width.%]="skill.proficiency"
                  ></div>
                </div>
              </div>
            </div>
          </app-card>
        </div>
      </div>
    </section>
  `,
  styleUrls: ['./skills-section.component.scss']
})
export class SkillsSectionComponent {
  skillCategories: SkillCategory[] = [
    {
      id: 'frontend',
      name: 'Frontend',
      skills: [
        { id: 1, name: 'React', category: 'Frontend', proficiency: 95 },
        { id: 2, name: 'Angular', category: 'Frontend', proficiency: 90 },
        { id: 3, name: 'Vue.js', category: 'Frontend', proficiency: 85 },
        { id: 4, name: 'TypeScript', category: 'Frontend', proficiency: 90 },
        { id: 5, name: 'JavaScript', category: 'Frontend', proficiency: 95 },
        { id: 6, name: 'HTML/CSS', category: 'Frontend', proficiency: 95 }
      ]
    },
    {
      id: 'backend',
      name: 'Backend',
      skills: [
        { id: 7, name: 'Node.js', category: 'Backend', proficiency: 90 },
        { id: 8, name: 'Python', category: 'Backend', proficiency: 85 },
        { id: 9, name: 'Express.js', category: 'Backend', proficiency: 90 },
        { id: 10, name: 'MongoDB', category: 'Backend', proficiency: 85 },
        { id: 11, name: 'PostgreSQL', category: 'Backend', proficiency: 80 },
        { id: 12, name: 'REST APIs', category: 'Backend', proficiency: 95 }
      ]
    },
    {
      id: 'tools',
      name: 'Tools & Others',
      skills: [
        { id: 13, name: 'Git', category: 'Tools', proficiency: 90 },
        { id: 14, name: 'Docker', category: 'Tools', proficiency: 80 },
        { id: 15, name: 'AWS', category: 'Tools', proficiency: 75 },
        { id: 16, name: 'Linux', category: 'Tools', proficiency: 85 },
        { id: 17, name: 'Agile/Scrum', category: 'Tools', proficiency: 90 },
        { id: 18, name: 'Testing', category: 'Tools', proficiency: 85 }
      ]
    }
  ];
}