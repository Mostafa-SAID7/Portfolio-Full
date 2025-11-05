import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SeoService } from '../../core/services/seo.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  private seoService = inject(SeoService);

  ngOnInit(): void {
    this.seoService.setTitle('Portfolio - Home');
    this.seoService.setMetaDescription('Welcome to my portfolio website. Discover my projects, skills, and experience.');
  }
}