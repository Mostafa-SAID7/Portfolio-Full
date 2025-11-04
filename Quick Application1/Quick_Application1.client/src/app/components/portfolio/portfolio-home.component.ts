// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { fadeInOut } from '../../services/animations';
import { PortfolioService } from '../../services/portfolio.service';

@Component({
  selector: 'app-portfolio-home',
  templateUrl: './portfolio-home.component.html',
  styleUrl: './portfolio-home.component.scss',
  animations: [fadeInOut],
  imports: [RouterLink, TranslateModule]
})
export class PortfolioHomeComponent implements OnInit {
  private portfolioService = inject(PortfolioService);

  featuredProjects: any[] = [];
  aboutSection: any = null;
  skills: any[] = [];

  ngOnInit() {
    this.loadHomeData();
  }

  private loadHomeData() {
    // Load featured projects (first 3)
    this.portfolioService.getItems().subscribe(items => {
      this.featuredProjects = items.filter(item => item.type === 'project').slice(0, 3);
    });

    // Load about section
    this.portfolioService.getSection('about').subscribe(section => {
      this.aboutSection = section;
    });

    // Load skills (first 6)
    this.portfolioService.getItems().subscribe(items => {
      this.skills = items.filter(item => item.type === 'skill').slice(0, 6);
    });
  }
}