
import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { AuthService } from '@core/services/auth.service'; 
import { UserRole } from '@core/models/user-role.enum';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean | UrlTree {
    if (!this.authService.isAuthenticated()) {
      console.log('AuthGuard: Not authenticated, redirecting to login.');
      return this.router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    const expectedRoles = route.data['expectedRoles'] as Array<UserRole>;
    if (expectedRoles && expectedRoles.length > 0) {
      const userRoles = this.authService.getUserRoles(); 
      
      console.log('AuthGuard: Expected roles:', expectedRoles);
      console.log('AuthGuard: User roles:', userRoles);

      const hasRequiredRole = expectedRoles.some(role => userRoles.includes(role));
      if (hasRequiredRole) {
        return true; 
      } else {
        console.log('AuthGuard: User does not have required role, redirecting.');
        return this.router.createUrlTree(['/']); 
      }
    }

    return true;
  }
}

