import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { BlogService, BlogPost, BlogFilter } from '../services/blog.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CardComponent } from '../../../shared/components/ui/card/card.component';
import { ButtonComponent } from '../../../shared/components/ui/button/button.component';
import { TruncatePipe } from '../../../shared/pipes/truncate.pipe';

@Component({
  selector: 'app-blog-list',
  standalone: true,
  imports: [CommonModule, RouterModule, CardComponent, ButtonComponent, TruncatePipe],
  template: `
    <div class="blog-list">
      <div class="blog-header">
        <h2 class="blog-title">Latest Posts</h2>
        <p class="blog-subtitle">Thoughts, tutorials, and insights about web development</p>
      </div>

      <div class="blog-grid" *ngIf="posts$ | async as postsResponse">
        <app-card
          *ngFor="let post of postsResponse.data"
          class="blog-card"
          routerLink="/blog/{{ post.slug }}"
        >
          <div class="blog-image" *ngIf="post.featuredImage">
            <img [src]="post.featuredImage" [alt]="post.title" />
          </div>

          <div class="blog-content">
            <div class="blog-meta">
              <span class="blog-date">{{ post.publishedAt | date:'mediumDate' }}</span>
              <span class="blog-reading-time">{{ post.readingTime }} min read</span>
            </div>

            <h3 class="blog-title">{{ post.title }}</h3>
            <p class="blog-excerpt">{{ post.excerpt | truncate:150 }}</p>

            <div class="blog-tags">
              <span
                *ngFor="let tag of post.tags.slice(0, 3)"
                class="tag"
              >
                {{ tag }}
              </span>
            </div>

            <div class="blog-actions">
              <app-button variant="outline" size="sm">
                Read More
              </app-button>
            </div>
          </div>
        </app-card>
      </div>

      <div class="loading-state" *ngIf="!(posts$ | async)">
        <p>Loading blog posts...</p>
      </div>

      <div class="empty-state" *ngIf="(posts$ | async)?.data?.length === 0">
        <p>No blog posts found.</p>
      </div>
    </div>
  `,
  styleUrls: ['./blog-list.component.scss']
})
export class BlogListComponent implements OnInit {
  private blogService = inject(BlogService);

  posts$: Observable<ApiResponse<BlogPost[]>> | null = null;

  ngOnInit(): void {
    this.loadPosts();
  }

  loadPosts(filter?: BlogFilter): void {
    this.posts$ = this.blogService.getPosts(filter);
  }
}