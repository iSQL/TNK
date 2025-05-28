import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

/**
 * Interface for the user object stored and emitted by the service.
 */
export interface User {
  token: string;
  email: string;
  userId?: string; // Optional: if you want to store/use it
  firstName?: string; // Optional
  lastName?: string; // Optional
  roles?: string[];
}

/**
 * Interface representing the expected structure of the login API response from the backend.
 * This should match your backend's LoginResponse DTO.
 */
interface LoginApiResponse {
  token: string;
  expiration?: string; // Or DateTime
  userId?: string;
  email?: string; // Email from backend response
  firstName?: string;
  lastName?: string;
  roles?: string[];
  // Add any other properties your backend login endpoint returns
}


@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = environment.apiUrl;

  private readonly USER_STORAGE_KEY = 'currentUser';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private currentUserSubject = new BehaviorSubject<User | null>(null);

  public isAuthenticated$: Observable<boolean> = this.isAuthenticatedSubject.asObservable();
  public currentUser$: Observable<User | null> = this.currentUserSubject.asObservable();

  constructor() {
    this.loadUserFromStorage();
  }

  /**
   * Loads user data from localStorage on service initialization.
   * Validates that essential data (token and email) exists.
   */
  private loadUserFromStorage(): void {
    if (typeof localStorage === 'undefined') {
      console.warn('AuthService: localStorage is not available. User state will not persist across sessions.');
      return;
    }

    const storedUserJson = localStorage.getItem(this.USER_STORAGE_KEY);
    if (storedUserJson) {
      try {
        const storedUser: User = JSON.parse(storedUserJson);
        console.log('AuthService: User object parsed from localStorage:', JSON.stringify(storedUser, null, 2));

        // Validate essential fields from stored user data
        if (storedUser && storedUser.token && storedUser.email && storedUser.email.trim() !== '') {
          this.currentUserSubject.next(storedUser);
          this.isAuthenticatedSubject.next(true);
          console.log('AuthService: User successfully loaded from storage and auth state updated.', storedUser);
        } else {
          console.warn('AuthService: Stored user data is invalid (missing token or email). Clearing storage.');
          this.clearAuthData(false); // Don't navigate, just clear
        }
      } catch (error) {
        console.error('AuthService: Error parsing stored user data from localStorage:', error);
        localStorage.removeItem(this.USER_STORAGE_KEY); // Clear corrupted data
      }
    } else {
      console.log('AuthService: No user found in localStorage.');
    }
  }

  /**
   * Logs in the user.
   * @param credentials Email and password.
   * @returns Observable of the API response.
   */
  login(credentials: { email?: string; password?: string }): Observable<LoginApiResponse> {
    console.log(`AuthService: Attempting login for email: ${credentials.email}`);
    return this.http.post<LoginApiResponse>(`${this.apiUrl}/api/auth/login`, credentials).pipe(
      tap({
        next: (response) => {
          console.log('AuthService: Login API response received:', JSON.stringify(response, null, 2));

          if (response && response.token) {
            console.log('AuthService: Token found in API response.');

            // Construct the User object for application state.
            // Prioritize email from the API response, then from credentials.
            const userEmail = response.email || credentials.email;
            if (!userEmail || userEmail.trim() === '') {
                console.error('AuthService: Critical error - Email is missing from both API response and credentials. Cannot proceed with login.');
                this.clearAuthData(false); // Don't navigate
                // Optionally, throw an error or return a specific error state to the component
                return; // Exit tap if email is missing
            }

            const userToStore: User = {
              token: response.token,
              email: userEmail,
              userId: response.userId,
              firstName: response.firstName,
              lastName: response.lastName,
              roles: response.roles || [],
            };
            console.log('AuthService: User object to be stored:', JSON.stringify(userToStore, null, 2));

            if (typeof localStorage !== 'undefined') {
              localStorage.setItem(this.USER_STORAGE_KEY, JSON.stringify(userToStore));
              console.log('AuthService: User data saved to localStorage.');
            }

            this.currentUserSubject.next(userToStore);
            this.isAuthenticatedSubject.next(true);
            console.log('AuthService: Authentication state updated. Navigating to dashboard...');

            this.router.navigate(['/dashboard'])
              .then(navigated => {
                if (navigated) {
                  console.log('AuthService: Navigation to dashboard successful.');
                } else {
                  console.error('AuthService: Navigation to dashboard failed (router.navigate returned false). Check route guards and configuration.');
                }
              })
              .catch(err => console.error('AuthService: Error during navigation to dashboard:', err));
          } else {
            console.error('AuthService: Login API response did not contain a token or was malformed. Response:', response);
            this.clearAuthData(false); // Don't navigate
          }
        },
        error: (err: HttpErrorResponse) => {
          console.error('AuthService: HTTP error during login:', err);
          this.clearAuthData(false); // Don't navigate, let component handle UI error
        }
      })
    );
  }

  /**
   * Registers a new user.
   * @param userData User registration data.
   * @returns Observable of the API response.
   */
  register(userData: any): Observable<any> { // Consider defining a RegisterRequest and RegisterResponse interface
    console.log(`AuthService: Attempting registration for email: ${userData.email}`);
    return this.http.post<any>(`${this.apiUrl}/api/auth/register`, userData).pipe(
      tap({
        next: (response) => {
          console.log('AuthService: Registration successful. API Response:', response);
          // Typically, after registration, the user is redirected to login.
          // The component handles showing a success message and navigation.
          // If API auto-logs in and returns a token, handle it like the login method.
        },
        error: (err: HttpErrorResponse) => {
          console.error('AuthService: HTTP error during registration:', err);
          // Let component handle UI error display
        }
      })
    );
  }

  /**
   * Logs out the current user, clears auth data, and navigates to login.
   */
  logout(): void {
    console.log('AuthService: Logging out user.');
    this.clearAuthData(true); // Navigate to login after clearing data
  }

  /**
   * Clears authentication data from subjects and localStorage.
   * @param navigateToLogin If true, navigates to the login page.
   */
  private clearAuthData(navigateToLogin: boolean): void {
    console.log(`AuthService: Clearing authentication data. Will navigate to login: ${navigateToLogin}`);
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(this.USER_STORAGE_KEY);
    }
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);

    if (navigateToLogin) {
      this.router.navigate(['/login'])
        .then(navigated => {
          if (navigated) {
            console.log('AuthService: Navigation to login after clearing auth data successful.');
          } else {
            console.error('AuthService: Navigation to login after clearing auth data failed.');
          }
        })
        .catch(err => console.error('AuthService: Error during navigation to login after clearing auth data:', err));
    }
  }

  /**
   * Retrieves the stored JWT token.
   * @returns The token string or null if not found/invalid.
   */
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
