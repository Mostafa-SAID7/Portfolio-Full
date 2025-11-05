import { Directive, ElementRef, Input, OnInit, OnDestroy } from '@angular/core';

@Directive({
  selector: '[appLazyLoad]',
  standalone: true
})
export class LazyLoadDirective implements OnInit, OnDestroy {
  @Input() appLazyLoad!: string;
  @Input() placeholder?: string;

  private observer?: IntersectionObserver;

  constructor(private el: ElementRef) {}

  ngOnInit(): void {
    this.setupLazyLoading();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private setupLazyLoading(): void {
    const element = this.el.nativeElement;

    // Set placeholder if provided
    if (this.placeholder) {
      element.src = this.placeholder;
    }

    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            this.loadImage();
            this.observer?.unobserve(element);
          }
        });
      },
      {
        rootMargin: '50px'
      }
    );

    this.observer.observe(element);
  }

  private loadImage(): void {
    const element = this.el.nativeElement;
    const img = new Image();

    img.onload = () => {
      element.src = this.appLazyLoad;
      element.classList.add('loaded');
    };

    img.src = this.appLazyLoad;
  }
}