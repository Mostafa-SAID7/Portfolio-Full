import { Injectable } from '@angular/core';
import { Observable, of, map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';

export interface BlogPost {
  id: number;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  author: string;
  publishedAt: Date;
  updatedAt: Date;
  tags: string[];
  featuredImage?: string;
  readingTime: number;
  published: boolean;
}

export interface BlogFilter {
  tag?: string;
  search?: string;
  published?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BlogService {
  constructor(private apiService: ApiService) {}

  getPosts(filter?: BlogFilter): Observable<ApiResponse<BlogPost[]>> {
    // In a real app, this would call the API
    // return this.apiService.get<ApiResponse<BlogPost[]>>('blog/posts', { params: filter });

    // Mock data
    const mockPosts: BlogPost[] = [
      {
        id: 1,
        title: 'Building Scalable Web Applications with Angular',
        slug: 'building-scalable-web-applications-angular',
        excerpt: 'Learn how to structure and scale your Angular applications for better performance and maintainability.',
        content: 'Full content here...',
        author: 'John Doe',
        publishedAt: new Date('2023-12-01'),
        updatedAt: new Date('2023-12-01'),
        tags: ['Angular', 'Web Development', 'Scalability'],
        featuredImage: '/assets/images/blog1.jpg',
        readingTime: 8,
        published: true
      },
      {
        id: 2,
        title: 'The Future of JavaScript Frameworks',
        slug: 'future-javascript-frameworks',
        excerpt: 'Exploring upcoming trends and innovations in JavaScript framework development.',
        content: 'Full content here...',
        author: 'John Doe',
        publishedAt: new Date('2023-11-15'),
        updatedAt: new Date('2023-11-15'),
        tags: ['JavaScript', 'Frameworks', 'Future Tech'],
        featuredImage: '/assets/images/blog2.jpg',
        readingTime: 6,
        published: true
      },
      {
        id: 3,
        title: 'Optimizing React Performance',
        slug: 'optimizing-react-performance',
        excerpt: 'Best practices and techniques for improving React application performance.',
        content: 'Full content here...',
        author: 'John Doe',
        publishedAt: new Date('2023-11-01'),
        updatedAt: new Date('2023-11-01'),
        tags: ['React', 'Performance', 'Optimization'],
        featuredImage: '/assets/images/blog3.jpg',
        readingTime: 10,
        published: true
      }
    ];

    let filteredPosts = mockPosts.filter(post => post.published);

    if (filter) {
      if (filter.tag) {
        filteredPosts = filteredPosts.filter(post =>
          post.tags.includes(filter.tag!)
        );
      }
      if (filter.search) {
        const searchTerm = filter.search.toLowerCase();
        filteredPosts = filteredPosts.filter(post =>
          post.title.toLowerCase().includes(searchTerm) ||
          post.excerpt.toLowerCase().includes(searchTerm) ||
          post.tags.some(tag => tag.toLowerCase().includes(searchTerm))
        );
      }
      if (filter.published !== undefined) {
        filteredPosts = filteredPosts.filter(post =>
          post.published === filter.published
        );
      }
    }

    return of({
      success: true,
      data: filteredPosts,
      message: 'Blog posts retrieved successfully'
    });
  }

  getPostBySlug(slug: string): Observable<ApiResponse<BlogPost>> {
    // In a real app: return this.apiService.get<ApiResponse<BlogPost>>(`blog/posts/${slug}`);

    // Mock implementation
    return this.getPosts().pipe(
      map(response => {
        const post = response.data?.find(p => p.slug === slug);
        return {
          success: !!post,
          data: post,
          message: post ? 'Post found' : 'Post not found'
        };
      })
    );
  }

  getTags(): Observable<ApiResponse<string[]>> {
    // In a real app: return this.apiService.get<ApiResponse<string[]>>('blog/tags');

    // Mock data
    const mockTags = ['Angular', 'React', 'JavaScript', 'TypeScript', 'Web Development', 'Performance', 'Scalability'];

    return of({
      success: true,
      data: mockTags,
      message: 'Tags retrieved successfully'
    });
  }
}