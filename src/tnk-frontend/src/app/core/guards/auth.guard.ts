import { Injectable, inject } from '@angular/core';
import {
  CanActivateFn,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree,
  Router,
} from '@angular/router';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service'; // Correct path to your AuthService

export const AuthGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticated$.pipe(
    take(1), // Take the latest value and complete
    map(isAuthenticated => {
      if (isAuthenticated) {
        return true; // User is authenticated, allow access
      } else {
        // User is not authenticated, redirect to login page
        console.log('AuthGuard: User not authenticated, redirecting to login.');
        return router.createUrlTree(['/login']);
      }
    })
  );
};
