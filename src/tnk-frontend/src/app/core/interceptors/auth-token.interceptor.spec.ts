import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient, HttpRequest } from '@angular/common/http';

import { AuthTokenInterceptor } from './auth-token.interceptor';
import { AuthService } from '../services/auth.service'; 
import { environment } from '../../../environments/environment';

// Create a simple mock for AuthService for this test suite
class MockAuthService {
  // Implement methods used by the interceptor, or spy on them
  getToken(): string | null {
    return 'mock-test-token'; // Default behavior for tests
  }
  // Add other methods if your interceptor calls them, e.g., isAuthenticated
}

describe('AuthTokenInterceptor', () => {
  let interceptor: AuthTokenInterceptor;
  let authService: AuthService;
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        // Provide the real interceptor class
        AuthTokenInterceptor,
        // Provide the mock for its dependency
        { provide: AuthService, useClass: MockAuthService },
        // Also provide it as an HTTP_INTERCEPTOR so we can test it through HttpClient
        {
          provide: HTTP_INTERCEPTORS,
          useClass: AuthTokenInterceptor,
          multi: true,
        },
      ],
    });

    // Inject instances
    interceptor = TestBed.inject(AuthTokenInterceptor); // Get instance of your interceptor
    authService = TestBed.inject(AuthService); // Get instance of the (mocked) AuthService
    httpMock = TestBed.inject(HttpTestingController); // For mocking backend calls
    httpClient = TestBed.inject(HttpClient); // To make actual HTTP calls that will be intercepted
  });

  afterEach(() => {
    // After every test, assert that there are no more pending requests.
    httpMock.verify();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('should add an Authorization header when token exists and URL is API URL', () => {
    // Arrange
    const mockToken = 'my-super-secret-test-token';
    // Spy on authService.getToken and make it return our mockToken
    spyOn(authService, 'getToken').and.returnValue(mockToken);

    // Act: Make an HTTP call through HttpClient which will be intercepted
    httpClient.get(`${environment.apiUrl}/some/api/endpoint`).subscribe(response => {
      // Assert on response if needed, but here we focus on the request
    });

    // Assert: Check the outgoing request
    const req = httpMock.expectOne(`${environment.apiUrl}/some/api/endpoint`); // Expect a request to this URL
    expect(req.request.headers.has('Authorization')).toBe(true);
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${mockToken}`);

    req.flush({}); // Respond to the request to complete the observable
  });

  it('should NOT add an Authorization header if token does NOT exist', () => {
    // Arrange
    spyOn(authService, 'getToken').and.returnValue(null); // No token

    // Act
    httpClient.get(`${environment.apiUrl}/some/api/endpoint`).subscribe();

    // Assert
    const req = httpMock.expectOne(`${environment.apiUrl}/some/api/endpoint`);
    expect(req.request.headers.has('Authorization')).toBe(false);

    req.flush({});
  });

  it('should NOT add an Authorization header if URL is NOT an API URL', () => {
    // Arrange
    const mockToken = 'my-super-secret-test-token';
    spyOn(authService, 'getToken').and.returnValue(mockToken);

    // Act
    httpClient.get('https://some.other.external.api/data').subscribe();

    // Assert
    const req = httpMock.expectOne('https://some.other.external.api/data');
    expect(req.request.headers.has('Authorization')).toBe(false);

    req.flush({});
  });
});
