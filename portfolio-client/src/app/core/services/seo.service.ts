import { Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  constructor(private title: Title, private meta: Meta) {}

  setTitle(title: string): void {
    this.title.setTitle(title);
  }

  setMetaDescription(description: string): void {
    this.meta.updateTag({ name: 'description', content: description });
  }

  setMetaKeywords(keywords: string): void {
    this.meta.updateTag({ name: 'keywords', content: keywords });
  }

  setCanonicalUrl(url: string): void {
    this.meta.updateTag({ rel: 'canonical', href: url });
  }

  setOpenGraphTags(title: string, description: string, image: string, url: string): void {
    this.meta.updateTag({ property: 'og:title', content: title });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:image', content: image });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
  }

  setTwitterCardTags(title: string, description: string, image: string): void {
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: title });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
  }
}