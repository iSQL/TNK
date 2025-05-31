import { Injectable, NgZone } from '@angular/core'; 
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { ApiService } from './api.service';
import { LoginRequest } from '@features/auth/models/login-request.interface';
import { LoginResponse } from '@features/auth/models/login-response.interface';
import { RegisterRequest } from '@features/auth/models/register-request.interface';
import { UserRole } from '@core/models/user-role.enum';
import { CurrentUserModel } from '@core/models/current-user.model';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly TOKEN_KEY = 'authToken';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private currentUserRoleSubject = new BehaviorSubject<UserRole | null>(null);
  public currentUserRole$ = this.currentUserRoleSubject.asObservable();

  private currentUserSubject = new BehaviorSubject<CurrentUserModel | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private router: Router,
    private ngZone: NgZone
  ) {
    const token = this.getToken();
    this.processToken(token);
  }

  private hasValidToken(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }
    try {
      const decodedToken: any = jwtDecode(token);
      if (typeof decodedToken.exp !== 'number') {
        console.warn('Token expiration (exp) claim is missing or not a number.');
        return false;
      }
      const isExpired = decodedToken.exp * 1000 < Date.now();
      return !isExpired;
    } catch (error) {
      return false;
    }
  }

  private processToken(token: string | null): void {
    if (token && this.hasValidToken()) {
      try {
        const decodedToken: any = jwtDecode(token);
        const roleClaim = decodedToken.role as UserRole;
        let userRole: UserRole | null = null;
        if (roleClaim && Object.values(UserRole).includes(roleClaim)) {
          userRole = roleClaim;
          this.currentUserRoleSubject.next(userRole);
        } else {
          this.currentUserRoleSubject.next(null);
          console.warn('Role not found or not recognized in token:', decodedToken.role);
        }

        const user: CurrentUserModel = {
          id: decodedToken.nameid || decodedToken.sub || '',
          email: decodedToken.email || '',
          firstName: decodedToken.firstName || '',
          lastName: decodedToken.lastName || '',
          role: userRole,
        };
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);

      } catch (error) {
        this.clearAuthDataAndToken();
      }
    } else {
      this.clearAuthData();
      if (!this.hasValidToken() && localStorage.getItem(this.TOKEN_KEY)) {
        localStorage.removeItem(this.TOKEN_KEY);
      }
    }
  }

  private clearAuthData(): void {
    this.currentUserRoleSubject.next(null);
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }
  
  private clearAuthDataAndToken(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.clearAuthData();
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.apiService.post<LoginResponse>('/auth/login', credentials).pipe(
      tap({
        next: (response) => {
          if (response && response.token) {
            this.setToken(response.token);
            this.processToken(response.token);
          } else {
            this.clearAuthDataAndToken();
            console.error('Login successful but no token received.');
          }
        },
        error: (err) => {
          this.clearAuthDataAndToken();
        }
      })
    );
  }

  register(userInfo: RegisterRequest): Observable<any> {
    return this.apiService.post('/register', userInfo);
  }

  logout(): void {
    this.clearAuthDataAndToken();
    
    
    this.ngZone.run(() => {
      this.router.navigateByUrl('/login', { replaceUrl: true });
    });
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  isAuthenticated(): boolean {
    return this.hasValidToken();
  }

  getUserRoles(): UserRole[] {
    const role = this.currentUserRoleSubject.value;
    return role ? [role] : [];
  }

  getUserId(): string | null {
    return this.currentUserSubject.value?.id || null;
  }

  public getCurrentUserSnapshot(): CurrentUserModel | null {
    return this.currentUserSubject.value;
  }
}
