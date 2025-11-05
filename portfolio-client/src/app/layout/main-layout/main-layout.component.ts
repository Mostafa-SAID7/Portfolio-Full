import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../../shared/components/header/header.component';
import { FooterComponent } from '../../shared/components/footer/footer.component';
import { ScrollToTopComponent } from '../../shared/components/scroll-to-top/scroll-to-top.component';
import { ToastComponent } from '../../shared/components/ui/toast/toast.component';
import { LoaderComponent } from '../../shared/components/loader/loader.component';
import { LoadingService } from '../../core/services/loading.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    FooterComponent,
    ScrollToTopComponent,
    ToastComponent,
    LoaderComponent
  ],
  template: `
    <div class="main-layout">
      <app-header></app-header>

      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <app-footer></app-footer>

      <app-scroll-to-top></app-scroll-to-top>

      <app-toast></app-toast>

      <app-loader [show]="loadingService.isLoading"></app-loader>
    </div>
  `,
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent {
  constructor(public loadingService: LoadingService) {}
}