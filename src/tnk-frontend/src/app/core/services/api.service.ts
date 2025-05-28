// src/app/core/services/api.service.ts
// This service provides a wrapper around HttpClient for making API requests.

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment'; // Ensure this path is correct

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  // Base URL for all API requests, loaded from environment configuration.
  // Assumes your environment.ts (and environment.prod.ts) has an apiUrl property.
  // e.g., apiUrl: 'http://localhost:5000/api' or just '/api' if using a proxy
  private readonly apiUrl = environment.apiUrl;

  // Default HTTP options
  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
      // 'Accept': 'application/json' // Optional, often default
    })
    // 'observe': 'response' as 'body' // Can be useful for getting full response
  };

  constructor(private http: HttpClient) {
    if (!this.apiUrl) {
      console.error("ApiService: 'apiUrl' is not defined in the environment configuration. Please check src/environments/environment.ts and environment.prod.ts.");
    }
  }

  /**
   * Handles HTTP errors.
   * @param error The HttpErrorResponse.
   * @returns An Observable that throws an error.
   */
  private handleError(error: HttpErrorResponse) {
    // A client-side or network error occurred.
    if (error.error instanceof ErrorEvent) {
      console.error('An error occurred:', error.error.message);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong.
      console.error(
        `Backend returned code ${error.status}, ` +
        `body was: ${JSON.stringify(error.error)}`);
    }
    // Return an observable with a user-facing error message.
    // Customize this based on how you want to display errors to the user.
    return throwError(() => new Error('Something bad happened; please try again later. Details logged to console.'));
  }

  /**
   * Performs a GET request.
   * @param path The API endpoint path (e.g., '/users').
   * @param params Optional HTTP parameters.
   * @returns An Observable of the response body.
   */
  get<T>(path: string, params: HttpParams = new HttpParams()): Observable<T> {
    const fullPath = `${this.apiUrl}${path}`;
    return this.http.get<T>(fullPath, { ...this.httpOptions, params })
      .pipe(catchError(this.handleError));
  }

  /**
   * Performs a POST request.
   * @param path The API endpoint path.
   * @param body The request body.
   * @returns An Observable of the response body.
   */
  post<T>(path: string, body: object = {}): Observable<T> {
    const fullPath = `${this.apiUrl}${path}`;
    return this.http.post<T>(fullPath, JSON.stringify(body), this.httpOptions)
      .pipe(catchError(this.handleError));
  }

  /**
   * Performs a PUT request.
   * @param path The API endpoint path.
   * @param body The request body.
   * @returns An Observable of the response body.
   */
  put<T>(path: string, body: object = {}): Observable<T> {
    const fullPath = `${this.apiUrl}${path}`;
    return this.http.put<T>(fullPath, JSON.stringify(body), this.httpOptions)
      .pipe(catchError(this.handleError));
  }

  /**
   * Performs a DELETE request.
   * @param path The API endpoint path.
   * @returns An Observable of the response body.
   */
  delete<T>(path: string): Observable<T> {
    const fullPath = `${this.apiUrl}${path}`;
    return this.http.delete<T>(fullPath, this.httpOptions)
      .pipe(catchError(this.handleError));
  }

  /**
   * Allows setting custom headers for a specific request if needed.
   * @param customHeaders An object containing custom headers.
   * @returns A new HttpHeaders object.
   */
  private getHeadersWith(customHeaders?: { [header: string]: string | string[] }): HttpHeaders {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    if (customHeaders) {
      Object.keys(customHeaders).forEach(key => {
        headers = headers.set(key, customHeaders[key]);
      });
    }
    return headers;
  }

  // Example of a GET request with custom headers (if ever needed)
  // getWithCustomHeaders<T>(path: string, customHeaders: { [header: string]: string | string[] }): Observable<T> {
  //   const fullPath = `${this.apiUrl}${path}`;
  //   return this.http.get<T>(fullPath, { headers: this.getHeadersWith(customHeaders) })
  //     .pipe(catchError(this.handleError));
  // }
}
