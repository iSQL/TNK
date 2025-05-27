import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service'; // Adjust path if your AuthService is elsewhere
import { map, take } from 'rxjs/operators';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Use the isAuthenticated$ observable for a reactive check,
  // or the isAuthenticated getter for an immediate check.
  // Using the getter is simpler for a basic guard.
  if (authService.isAuthenticated) {
    return true; // User is authenticated, allow access
  } else {
    // User is not authenticated, redirect to login page
    // You might want to store the attempted URL (state.url) to redirect back after login
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false; // Deny access
  }

  // Alternative using the observable (more robust if auth state can change rapidly
  // and you need to wait for the first emission):
  /*
  return authService.isAuthenticated$.pipe(
    take(1), // Take the first emitted value and complete
    map(isAuthenticated => {
      if (isAuthenticated) {
        return true;
      } else {
        router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
        return false;
      }
    })
  );
  */
};
