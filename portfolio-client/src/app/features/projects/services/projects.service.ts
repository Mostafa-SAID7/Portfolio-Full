import { Injectable } from '@angular/core';
import { Observable, of, map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Project, ProjectFilter } from '../../../core/models/project.model';
import { ApiResponse } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectsService {
  constructor(private apiService: ApiService) {}

  getProjects(filter?: ProjectFilter): Observable<ApiResponse<Project[]>> {
    // In a real app, this would call the API
    // return this.apiService.get<ApiResponse<Project[]>>('projects', { params: filter });

    // Mock data for now
    const mockProjects: Project[] = [
      {
        id: 1,
        title: 'E-Commerce Platform',
        description: 'A full-stack e-commerce solution with React, Node.js, and MongoDB. Features include user authentication, product catalog, shopping cart, and payment integration.',
        imageUrl: '/assets/images/project1.jpg',
        technologies: ['React', 'Node.js', 'MongoDB', 'Express', 'Stripe'],
        githubUrl: 'https://github.com/username/ecommerce-platform',
        liveUrl: 'https://ecommerce-demo.com',
        featured: true,
        createdAt: new Date('2023-01-15'),
        updatedAt: new Date('2023-02-20')
      },
      {
        id: 2,
        title: 'Task Management App',
        description: 'A collaborative task management application with real-time updates using WebSockets. Includes team collaboration, project tracking, and deadline management.',
        imageUrl: '/assets/images/project2.jpg',
        technologies: ['Angular', 'Firebase', 'RxJS', 'Socket.io'],
        githubUrl: 'https://github.com/username/task-manager',
        liveUrl: 'https://taskmanager-demo.com',
        featured: true,
        createdAt: new Date('2023-03-10'),
        updatedAt: new Date('2023-04-05')
      },
      {
        id: 3,
        title: 'Weather Dashboard',
        description: 'A responsive weather dashboard with location-based forecasts, interactive maps, and weather alerts. Built with Vue.js and integrated with multiple weather APIs.',
        imageUrl: '/assets/images/project3.jpg',
        technologies: ['Vue.js', 'OpenWeather API', 'Chart.js', 'Leaflet'],
        githubUrl: 'https://github.com/username/weather-dashboard',
        liveUrl: 'https://weather-demo.com',
        featured: false,
        createdAt: new Date('2023-05-01'),
        updatedAt: new Date('2023-05-15')
      }
    ];

    let filteredProjects = mockProjects;

    if (filter) {
      if (filter.technology) {
        filteredProjects = filteredProjects.filter(project =>
          project.technologies.includes(filter.technology!)
        );
      }
      if (filter.featured !== undefined) {
        filteredProjects = filteredProjects.filter(project =>
          project.featured === filter.featured
        );
      }
      if (filter.search) {
        const searchTerm = filter.search.toLowerCase();
        filteredProjects = filteredProjects.filter(project =>
          project.title.toLowerCase().includes(searchTerm) ||
          project.description.toLowerCase().includes(searchTerm) ||
          project.technologies.some(tech => tech.toLowerCase().includes(searchTerm))
        );
      }
    }

    return of({
      success: true,
      data: filteredProjects,
      message: 'Projects retrieved successfully'
    });
  }

  getProjectById(id: number): Observable<ApiResponse<Project>> {
    // In a real app: return this.apiService.get<ApiResponse<Project>>(`projects/${id}`);

    // Mock implementation
    return this.getProjects().pipe(
      map(response => {
        const project = response.data?.find(p => p.id === id);
        return {
          success: !!project,
          data: project,
          message: project ? 'Project found' : 'Project not found'
        };
      })
    );
  }
}