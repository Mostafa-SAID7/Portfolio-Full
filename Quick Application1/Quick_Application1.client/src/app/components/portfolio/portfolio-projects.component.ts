// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

import { Component, OnInit, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { fadeInOut } from '../../services/animations';
import { PortfolioService } from '../../services/portfolio.service';

@Component({
  selector: 'app-portfolio-projects',
  templateUrl: './portfolio-projects.component.html',
  styleUrl: './portfolio-projects.component.scss',
  animations: [fadeInOut],
  imports: [TranslateModule]
})
export class PortfolioProjectsComponent implements OnInit {
  private portfolioService = inject(PortfolioService);

  projects: any[] = [];
  selectedCategory = 'all';
  categories = ['all', 'web', 'mobile', 'desktop', 'api'];

  ngOnInit() {
    this.loadProjects();
  }

  private loadProjects() {
    this.portfolioService.getItemsByType('project').subscribe(items => {
      this.projects = items;
    });
  }

  filterProjects(category: string) {
    this.selectedCategory = category;
    if (category === 'all') {
      this.loadProjects();
    } else {
      this.portfolioService.getItemsByType('project').subscribe(items => {
        this.projects = items.filter(project =>
          project.technologies?.toLowerCase().includes(category.toLowerCase())
        );
      });
    }
  }

  getProjectImage(project: any): string {
    return project.imageUrl || 'assets/images/project-placeholder.jpg';
  }
}