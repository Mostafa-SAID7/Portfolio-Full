// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/portfolio/portfolio-home.component').then(m => m.PortfolioHomeComponent),
    title: 'Portfolio - Home'
  },
  {
    path: 'about',
    loadComponent: () => import('./components/portfolio/portfolio-about.component').then(m => m.PortfolioAboutComponent),
    title: 'About Me'
  },
  {
    path: 'projects',
    loadComponent: () => import('./components/portfolio/portfolio-projects.component').then(m => m.PortfolioProjectsComponent),
    title: 'My Projects'
  },
  {
    path: 'skills',
    loadComponent: () => import('./components/portfolio/portfolio-skills.component').then(m => m.PortfolioSkillsComponent),
    title: 'Skills & Technologies'
  },
  {
    path: 'contact',
    loadComponent: () => import('./components/portfolio/portfolio-contact.component').then(m => m.PortfolioContactComponent),
    title: 'Contact Me'
  },
  {
    path: 'admin',
    loadComponent: () => import('./components/admin/admin-dashboard.component').then(m => m.AdminDashboardComponent),
    title: 'Admin Dashboard'
  },
  {
    path: 'home',
    redirectTo: '/',
    pathMatch: 'full'
  },
  {
    path: '**',
    loadComponent: () => import('./components/not-found/not-found.component').then(m => m.NotFoundComponent),
    title: 'Page Not Found'
  }
];
