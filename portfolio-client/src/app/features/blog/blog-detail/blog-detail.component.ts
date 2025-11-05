import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { BlogService, BlogPost } from '../services/blog.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CardComponent } from '../../../shared/components/ui/card/card.component';
import { ButtonComponent } from '../../../shared/components/ui/button/button.component';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';
import { SeoService } from '../../../core/services/seo.service';

@Component({
  selector: 'app-blog-detail',
  standalone: true,
  imports: [CommonModule, CardComponent, ButtonComponent, SafeHtmlPipe],
  template: `
    <div class="blog-detail" *ngIf="post$ | async as postResponse">
      <div class="container" *ngIf="postResponse.data as post">
        <div class="blog-header">
          <button class="back-button" (click)="goBack()">
            ← Back to Blog
          </button>

          <div class="blog-meta">
            <span class="blog-date">{{ post.publishedAt | date:'mediumDate' }}</span>
            <span class="blog-reading-time">{{ post.readingTime }} min read</span>
            <span class="blog-author">By {{ post.author }}</span>
          </div>

          <h1 class="blog-title">{{ post.title }}</h1>

          <div class="blog-tags">
            <span
              *ngFor="let tag of post.tags"
              class="tag"
            >
              {{ tag }}
            </span>
          </div>
        </div>

        <div class="blog-content">
          <div class="blog-image" *ngIf="post.featuredImage">
            <img [src]="post.featuredImage" [alt]="post.title" />
          </div>

          <div class="blog-body">
            <div class="blog-text" [innerHTML]="post.content | safeHtml"></div>
          </div>
        </div>

        <div class="blog-footer">
          <div class="share-section">
            <h3>Share this post</h3>
            <div class="share-buttons">
              <app-button variant="outline" size="sm" (click)="shareOnTwitter()">
                Twitter
              </app-button>
              <app-button variant="outline" size="sm" (click)="shareOnLinkedIn()">
                LinkedIn
              </app-button>
              <app-button variant="outline" size="sm" (click)="copyLink()">
                Copy Link
              </app-button>
            </div>
          </div>
        </div>
      </div>

      <div class="loading-state" *ngIf="!postResponse.data">
        <p>Loading blog post...</p>
      </div>
    </div>
  `,
  styleUrls: ['./blog-detail.component.scss']
})
export class BlogDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private blogService = inject(BlogService);
  private seoService = inject(SeoService);

  post$: Observable<ApiResponse<BlogPost>> | null = null;

  ngOnInit(): void {
    this.post$ = this.route.params.pipe(
      switchMap(params => {
        const slug = params['slug'];
        return this.blogService.getPostBySlug(slug);
      })
    );

    // Update SEO when post loads
    this.post$.subscribe(response => {
      if (response.data) {
        this.seoService.setTitle(`${response.data.title} - Blog`);
        this.seoService.setMetaDescription(response.data.excerpt);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/blog']);
  }

  shareOnTwitter(): void {
    const url = window.location.href;
    const text = `Check out this blog post: ${this.getCurrentPost()?.title}`;
    const twitterUrl = `https://twitter.com/intent/tweet?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
    window.open(twitterUrl, '_blank');
  }

  shareOnLinkedIn(): void {
    const url = window.location.href;
    const linkedInUrl = `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(url)}`;
    window.open(linkedInUrl, '_blank');
  }

  copyLink(): void {
    navigator.clipboard.writeText(window.location.href);
    // Could show a toast notification here
  }

  private getCurrentPost(): BlogPost | null {
    let currentPost: BlogPost | null = null;
    this.post$?.subscribe(response => {
      currentPost = response.data || null;
    });
    return currentPost;
  }
}