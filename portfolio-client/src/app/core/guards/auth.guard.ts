import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private router: Router) {}

  canActivate(): Observable<boolean> | Promise<boolean> | boolean {
    const token = localStorage.getItem('authToken');

    if (token) {
      // TODO: Add token validation logic here
      return true;
    } else {
      this.router.navigate(['/login']);
      return false;
    }
  }
}