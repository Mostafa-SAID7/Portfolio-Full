import { Directive, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';

@Directive({
  selector: '[appIntersectionObserver]',
  standalone: true
})
export class IntersectionObserverDirective implements OnInit, OnDestroy {
  @Input() threshold: number | number[] = 0.1;
  @Input() rootMargin = '0px';
  @Input() root: Element | null = null;

  @Output() intersection = new EventEmitter<IntersectionObserverEntry>();

  private observer?: IntersectionObserver;

  constructor(private el: ElementRef) {}

  ngOnInit(): void {
    this.setupObserver();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private setupObserver(): void {
    const options: IntersectionObserverInit = {
      root: this.root,
      rootMargin: this.rootMargin,
      threshold: this.threshold
    };

    this.observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        this.intersection.emit(entry);
      });
    }, options);

    this.observer.observe(this.el.nativeElement);
  }
}