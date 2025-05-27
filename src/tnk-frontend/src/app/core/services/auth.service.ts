import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode'; 
import { environment } from '../../../environments/environment'; // Adjust path if needed

// --- Interfaces for DTOs ---
// These should match the DTOs your backend expects/returnss

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password?: string; // Password might be optional if using external providers later
  confirmPassword?: string;
  role: string; // e.g., 'Vendor', 'Customer'
}

export interface RegisterResponse {
  userId: string;
  message: string;
}

export interface LoginRequest {
  email: string;
  password?: string;
}

export interface LoginResponse {
  token: string;
  expiration: string; // Assuming ISO date string
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

// Interface for the decoded JWT payload
interface DecodedToken {
  sub: string; // Subject (usually user ID)
  email: string;
  nameid: string; // Often user ID
  unique_name: string; // Often username or email
  firstName?: string;
  lastName?: string;
  role: string | string[]; // Role can be a single string or an array of strings
  nbf?: number;
  exp?: number;
  iat?: number;
  iss?: string;
  aud?: string;
}

@Injectable({
  providedIn: 'root' // Provided in root, so it's a singleton
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/auth`; // Base URL for auth endpoints
  private readonly TOKEN_KEY = 'tnk_auth_token';

  // BehaviorSubject to hold the current authentication state
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated$: Observable<boolean> = this.isAuthenticatedSubject.asObservable();

  // BehaviorSubject to hold the current user's decoded token data (or null if not logged in)
  private currentUserSubject = new BehaviorSubject<DecodedToken | null>(this.getDecodedToken());
  public currentUser$: Observable<DecodedToken | null> = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Initialize auth state on service construction
    this.checkTokenExpiration();
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  private checkTokenExpiration(): void {
    const token = this.getToken();
    if (token) {
      const decoded = this.decodeToken(token);
      if (decoded && decoded.exp) {
        const isExpired = Date.now() >= decoded.exp * 1000;
        if (isExpired) {
          this.logout(); // Token is expired, log out
        } else {
          this.isAuthenticatedSubject.next(true);
          this.currentUserSubject.next(decoded);
        }
      } else {
        // Token exists but can't be decoded or has no expiration - treat as invalid
        this.logout();
      }
    } else {
      this.isAuthenticatedSubject.next(false);
      this.currentUserSubject.next(null);
    }
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(response => console.log('Registration successful', response)),
      catchError(this.handleError)
    );
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        if (response && response.token) {
          this.storeToken(response.token);
          const decodedToken = this.decodeToken(response.token);
          this.isAuthenticatedSubject.next(true);
          this.currentUserSubject.next(decodedToken);
          console.log('Login successful, token stored.');
        }
      }),
      catchError(this.handleError)
    );
  }

  logout(): void {
    this.removeToken();
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']); // Or your desired logout destination
    console.log('Logged out, token removed.');
  }

  storeToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  removeToken(): void {
    localStorage.removeItem(this.TOKEN_KEY);
  }

  public get isAuthenticated(): boolean {
    // More robust check considering token expiration
    const token = this.getToken();
    if (!token) {
      return false;
    }
    const decoded = this.decodeToken(token);
    if (!decoded || !decoded.exp) {
      return false; // Invalid token or no expiration
    }
    const isExpired = Date.now() >= decoded.exp * 1000;
    if (isExpired) {
      this.logout(); // Clean up if found expired during a check
      return false;
    }
    return true;
  }

  private decodeToken(token: string): DecodedToken | null {
    try {
      return jwtDecode<DecodedToken>(token);
    } catch (error) {
      console.error('Error decoding token:', error);
      return null;
    }
  }

  private getDecodedToken(): DecodedToken | null {
    const token = this.getToken();
    if (token) {
      return this.decodeToken(token);
    }
    return null;
  }

  // --- Helper methods to get user info from token ---
  public getUserId(): string | null {
    const user = this.currentUserSubject.value;
    return user ? user.sub || user.nameid : null; // 'sub' is standard, 'nameid' is common from ASP.NET Core Identity
  }

  public getUserEmail(): string | null {
    const user = this.currentUserSubject.value;
    return user ? user.email : null;
  }

  public getUserRoles(): string[] {
    const user = this.currentUserSubject.value;
    if (user && user.role) {
      return Array.isArray(user.role) ? user.role : [user.role];
    }
    return [];
  }

  public getFirstName(): string | null {
    const user = this.currentUserSubject.value;
    return user && user.firstName ? user.firstName : null;
  }

  public getLastName(): string | null {
    const user = this.currentUserSubject.value;
    return user && user.lastName ? user.lastName : null;
  }

  public hasRole(role: string): boolean {
    return this.getUserRoles().includes(role);
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An unknown error occurred!';
    if (error.error instanceof ErrorEvent) {
      // Client-side or network error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong.
      if (error.status === 0) {
        errorMessage = 'Cannot connect to API. Please check your network or server.';
      } else if (error.error && typeof error.error === 'string') {
        errorMessage = error.error; // Plain text error from backend
      } else if (error.error && error.error.message) {
        errorMessage = error.error.message; // Error object with a message property
      } else if (error.error && error.error.title) { // For RFC7807 ProblemDetails
        errorMessage = `${error.error.title}${error.error.detail ? ': ' + error.error.detail : ''}`;
      }
       else {
        errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      }
    }
    console.error(errorMessage);
    return throwError(() => new Error(errorMessage)); // Return an observable error
  }
}
