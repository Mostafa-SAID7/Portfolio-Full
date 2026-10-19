/** Mirrors PortfolioAPI.Core.DTOs.BlogPostDto (api/Blog, api/public/blog). */
export interface BlogPost {
  id: number;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  author: string;
  publishedAt: string;
  tags: string[];
  featuredImage: string | null;
  /** Minutes. */
  readingTime: number;
  published: boolean;
  viewCount: number;
  metaTitle: string | null;
  metaDescription: string | null;
  createdAt: string;
  updatedAt: string | null;
}
