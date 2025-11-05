import { trigger, state, style, transition, animate, query, stagger } from '@angular/animations';

export const fadeInUp = trigger('fadeInUp', [
  state('in', style({ opacity: 1, transform: 'translateY(0)' })),
  transition('void => *', [
    style({ opacity: 0, transform: 'translateY(30px)' }),
    animate('0.6s ease-out')
  ])
]);

export const fadeIn = trigger('fadeIn', [
  transition(':enter', [
    style({ opacity: 0 }),
    animate('0.5s ease-in', style({ opacity: 1 }))
  ]),
  transition(':leave', [
    animate('0.5s ease-out', style({ opacity: 0 }))
  ])
]);

export const slideInLeft = trigger('slideInLeft', [
  transition(':enter', [
    style({ transform: 'translateX(-100%)', opacity: 0 }),
    animate('0.5s ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
  ])
]);

export const slideInRight = trigger('slideInRight', [
  transition(':enter', [
    style({ transform: 'translateX(100%)', opacity: 0 }),
    animate('0.5s ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
  ])
]);

export const staggerFadeIn = trigger('staggerFadeIn', [
  transition(':enter', [
    query(':enter', [
      style({ opacity: 0, transform: 'translateY(20px)' }),
      stagger(100, [
        animate('0.3s ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ], { optional: true })
  ])
]);

export const scaleIn = trigger('scaleIn', [
  transition(':enter', [
    style({ transform: 'scale(0.8)', opacity: 0 }),
    animate('0.3s ease-out', style({ transform: 'scale(1)', opacity: 1 }))
  ])
]);

export const bounceIn = trigger('bounceIn', [
  transition(':enter', [
    style({ transform: 'scale(0.3)', opacity: 0 }),
    animate('0.6s cubic-bezier(0.68, -0.55, 0.265, 1.55)', style({ transform: 'scale(1)', opacity: 1 }))
  ])
]);