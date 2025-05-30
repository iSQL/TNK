import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, shareReplay } from 'rxjs'; // shareReplay is imported
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export interface User {
  token: string;
  email: string;
  userId?: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
}

interface LoginApiResponse {
  token: string;
  expiration?: string;
  userId?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = environment.apiUrl;

  private readonly USER_STORAGE_KEY = 'currentUser';
  private callIdCounter = 0; 

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private currentUserSubject = new BehaviorSubject<User | null>(null);

  public isAuthenticated$: Observable<boolean> = this.isAuthenticatedSubject.asObservable();
  public currentUser$: Observable<User | null> = this.currentUserSubject.asObservable();

  constructor() {
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    if (typeof localStorage === 'undefined') {
      console.warn('AuthService: localStorage is not available.');
      return;
    }
    const storedUserJson = localStorage.getItem(this.USER_STORAGE_KEY);
    if (storedUserJson) {
      try {
        const storedUser: User = JSON.parse(storedUserJson);
        if (storedUser && storedUser.token && storedUser.email && storedUser.email.trim() !== '') {
          this.currentUserSubject.next(storedUser);
          this.isAuthenticatedSubject.next(true);
          console.log('AuthService: User loaded from storage.', storedUser);
        } else {
          console.warn('AuthService: Stored user data invalid. Clearing.');
          this.clearAuthData(false);
        }
      } catch (error) {
        console.error('AuthService: Error parsing stored user data.', error);
        localStorage.removeItem(this.USER_STORAGE_KEY);
      }
    } else {
      console.log('AuthService: No user in localStorage.');
    }
  }

  login(credentials: { email?: string; password?: string }): Observable<LoginApiResponse> {
    this.callIdCounter++;
    const currentCallId = this.callIdCounter;
    const httpPostObservable = this.http.post<LoginApiResponse>(`${this.apiUrl}/api/auth/login`, credentials);
    return httpPostObservable.pipe(
      tap({
        next: (response) => {
          if (response && response.token) {
            const userEmail = response.email || credentials.email;
            if (!userEmail || userEmail.trim() === '') {
              this.clearAuthData(false);
              return;
            }
            const userToStore: User = {
              token: response.token,
              email: userEmail,
              userId: response.userId,
              firstName: response.firstName,
              lastName: response.lastName,
              roles: response.roles || [],
            };
            if (typeof localStorage !== 'undefined') {
              localStorage.setItem(this.USER_STORAGE_KEY, JSON.stringify(userToStore));
            }
            this.currentUserSubject.next(userToStore);
            this.isAuthenticatedSubject.next(true);
            this.router.navigate(['/dashboard']);
          } else {
            console.error(`AuthService (Call #${currentCallId}): TAP NEXT - Token missing in response. Clearing auth.`);
            this.clearAuthData(false);
          }
        },
        error: (err: HttpErrorResponse) => {
          console.error(`AuthService (Call #${currentCallId}): TAP ERROR - HTTP error for ${credentials.email}: Status ${err.status}`, JSON.stringify(err.error, null, 2)); // Log D
          this.clearAuthData(false);
        },
        complete: () => {
        }
      }),
      shareReplay({ bufferSize: 1, refCount: true }) // Ensures the source (HTTP POST) executes once per unique observable instance
                                                    // if this *specific* observable instance is subscribed to multiple times.
                                                    // It does not prevent the login() method itself from being called twice.
    );
  }

  register(userData: any): Observable<any> {
    this.callIdCounter++;
    const currentCallId = this.callIdCounter;
    return this.http.post<any>(`${this.apiUrl}/api/auth/register`, userData).pipe(
      tap({ }),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  logout(): void {
    this.clearAuthData(true);
  }

  private clearAuthData(navigateToLogin: boolean): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(this.USER_STORAGE_KEY);
    }
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    if (navigateToLogin) {
      this.router.navigate(['/login']);
    }
  }

  getToken(): string | null {
    if (typeof localStorage === 'undefined') return null;
    const storedUserJson = localStorage.getItem(this.USER_STORAGE_KEY);
    if (storedUserJson) {
      try {
        const user: User = JSON.parse(storedUserJson);
        return user.token;
      } catch (error) {
        console.error('AuthService: Error parsing token from stored user data:', error);
        return null;
      }
    }
    return null;
  }
}
