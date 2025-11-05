import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardComponent } from '../../../../shared/components/ui/card/card.component';

interface Testimonial {
  id: number;
  name: string;
  position: string;
  company: string;
  content: string;
  avatar?: string;
  rating: number;
}

@Component({
  selector: 'app-testimonials',
  standalone: true,
  imports: [CommonModule, CardComponent],
  template: `
    <section class="testimonials">
      <div class="container">
        <h2 class="section-title">What People Say</h2>
        <div class="testimonials-grid">
          <app-card
            *ngFor="let testimonial of testimonials"
            class="testimonial-card"
          >
            <div class="testimonial-content">
              <div class="rating">
                <span
                  *ngFor="let star of [1,2,3,4,5]"
                  class="star"
                  [class.filled]="star <= testimonial.rating"
                >
                  ★
                </span>
              </div>
              <blockquote class="testimonial-text">
                "{{ testimonial.content }}"
              </blockquote>
              <div class="testimonial-author">
                <div class="author-avatar" *ngIf="testimonial.avatar">
                  <img [src]="testimonial.avatar" [alt]="testimonial.name" />
                </div>
                <div class="author-info">
                  <div class="author-name">{{ testimonial.name }}</div>
                  <div class="author-position">
                    {{ testimonial.position }} at {{ testimonial.company }}
                  </div>
                </div>
              </div>
            </div>
          </app-card>
        </div>
      </div>
    </section>
  `,
  styleUrls: ['./testimonials.component.scss']
})
export class TestimonialsComponent {
  testimonials: Testimonial[] = [
    {
      id: 1,
      name: 'Sarah Johnson',
      position: 'Product Manager',
      company: 'Tech Solutions Inc.',
      content: 'Working with this developer was an absolute pleasure. They delivered high-quality code on time and was always willing to go the extra mile to ensure the project was successful.',
      rating: 5
    },
    {
      id: 2,
      name: 'Michael Chen',
      position: 'CTO',
      company: 'Digital Innovations Ltd.',
      content: 'Exceptional technical skills combined with great communication. They not only built what we asked for but also suggested improvements that made our product much better.',
      rating: 5
    },
    {
      id: 3,
      name: 'Emily Rodriguez',
      position: 'Startup Founder',
      company: 'StartupXYZ',
      content: 'Incredible attention to detail and problem-solving abilities. They helped us launch our MVP in record time and the application has been running smoothly ever since.',
      rating: 5
    },
    {
      id: 4,
      name: 'David Kim',
      position: 'Engineering Manager',
      company: 'Global Tech Corp',
      content: 'One of the most talented developers I\'ve worked with. Their code is clean, well-documented, and they have a great understanding of modern development practices.',
      rating: 5
    }
  ];
}