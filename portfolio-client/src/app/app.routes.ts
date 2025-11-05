import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
      },
      {
        path: 'projects',
        loadComponent: () => import('./features/projects/projects.component').then(m => m.ProjectsComponent)
      },
      {
        path: 'about',
        loadComponent: () => import('./features/about/about.component').then(m => m.AboutComponent)
      },
      {
        path: 'contact',
        loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent)
      },
      {
        path: 'blog',
        children: [
          {
            path: '',
            loadComponent: () => import('./features/blog/blog-list/blog-list.component').then(m => m.BlogListComponent)
          },
          {
            path: ':slug',
            loadComponent: () => import('./features/blog/blog-detail/blog-detail.component').then(m => m.BlogDetailComponent)
          }
        ]
      }
    ]
  },
  // Admin routes - commented out until admin components are created
  // {
  //   path: 'admin',
  //   component: AdminLayoutComponent,
  //   canActivate: [AuthGuard],
  //   children: [
  //     {
  //       path: '',
  //       redirectTo: 'dashboard',
  //       pathMatch: 'full'
  //     },
  //     {
  //       path: 'dashboard',
  //       loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent)
  //     },
  //     {
  //       path: 'projects',
  //       loadComponent: () => import('./features/admin/projects/projects.component').then(m => m.AdminProjectsComponent)
  //     },
  //     {
  //       path: 'blog',
  //       loadComponent: () => import('./features/admin/blog/blog.component').then(m => m.AdminBlogComponent)
  //     },
  //     {
  //       path: 'messages',
  //       loadComponent: () => import('./features/admin/messages/messages.component').then(m => m.AdminMessagesComponent)
  //     },
  //     {
  //       path: 'settings',
  //       loadComponent: () => import('./features/admin/settings/settings.component').then(m => m.AdminSettingsComponent)
  //     }
  //   ]
  // },
  {
    path: '**',
    redirectTo: ''
  }
];
