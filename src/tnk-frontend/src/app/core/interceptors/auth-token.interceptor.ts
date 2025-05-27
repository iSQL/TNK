import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpHandlerFn // For functional interceptors if you switch later
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service'; // Adjust path if needed
import { environment } from '../../../environments/environment'; // To check if the request is to your API

@Injectable()
export class AuthTokenInterceptor implements HttpInterceptor {

  constructor(private authService: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();
    const isApiUrl = request.url.startsWith(environment.apiUrl); // Check if the request is to your backend API

    if (token && isApiUrl) {
      // Clone the request and add the authorization header
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request);
  }
}

// --- For Functional Interceptor (Alternative, more modern Angular way) ---
// If you prefer functional interceptors (common in Angular 15+ with standalone APIs):
// You would not create a class, but a function like this:

/*
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service'; // Adjust path
import { environment } from '../../../environments/environment'; // Adjust path

export const authTokenInterceptorFunctional: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();
  const isApiUrl = req.url.startsWith(environment.apiUrl);

  if (token && isApiUrl) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }
  return next(req);
};
*/
