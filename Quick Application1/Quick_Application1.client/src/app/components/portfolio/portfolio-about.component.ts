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
  selector: 'app-portfolio-about',
  templateUrl: './portfolio-about.component.html',
  styleUrl: './portfolio-about.component.scss',
  animations: [fadeInOut],
  imports: [TranslateModule]
})
export class PortfolioAboutComponent implements OnInit {
  private portfolioService = inject(PortfolioService);

  aboutSection: any = null;
  experienceItems: any[] = [];
  educationItems: any[] = [];

  ngOnInit() {
    this.loadAboutData();
  }

  private loadAboutData() {
    // Load about section
    this.portfolioService.getSection('about').subscribe(section => {
      this.aboutSection = section;
    });

    // Load experience items
    this.portfolioService.getItemsByType('experience').subscribe(items => {
      this.experienceItems = items;
    });

    // Load education items
    this.portfolioService.getItemsByType('education').subscribe(items => {
      this.educationItems = items;
    });
  }
}