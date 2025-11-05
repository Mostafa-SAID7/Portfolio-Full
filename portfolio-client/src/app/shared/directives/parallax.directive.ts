import { Directive, ElementRef, HostListener, Input, OnDestroy, OnInit } from '@angular/core';

@Directive({
  selector: '[appParallax]',
  standalone: true
})
export class ParallaxDirective implements OnInit, OnDestroy {
  @Input() speed = 0.5;
  @Input() direction: 'up' | 'down' = 'up';

  private initialTransform = '';
  private animationFrame?: number;

  constructor(private el: ElementRef) {}

  ngOnInit(): void {
    this.initialTransform = getComputedStyle(this.el.nativeElement).transform || '';
    this.updatePosition();
  }

  ngOnDestroy(): void {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
    }

    this.animationFrame = requestAnimationFrame(() => {
      this.updatePosition();
    });
  }

  private updatePosition(): void {
    const scrolled = window.pageYOffset;
    const rate = scrolled * this.speed;
    const transform = this.direction === 'up' ? -rate : rate;

    this.el.nativeElement.style.transform = `${this.initialTransform} translateY(${transform}px)`;
  }
}