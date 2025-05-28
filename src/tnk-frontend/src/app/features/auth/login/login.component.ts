import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service'; // Correct path
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslateModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private translate = inject(TranslateService);

  loginForm!: FormGroup;
  errorMessage: string | null = null;
  isLoading: boolean = false;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(3)]],
    });
  }

  ngOnInit(): void {
    // If the user is already authenticated, redirect them from the login page
    // This check is useful if a user manually navigates to /login
    this.authService.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigate(['/dashboard']);
      }
    });
  }

  onSubmit(): void {
            console.log('LoginComponent: onSubmit() called at:', new Date().toISOString()); // Log1

    if (this.loginForm.invalid) {
      console.log('LoginComponent: Form is invalid, onSubmit() is returning.'); // Log 2
      this.markFormGroupTouched(this.loginForm);
      this.translate.get('VALIDATION.FORM_INVALID').subscribe((res: string) => {
        this.errorMessage = res; // You'll need to add FORM_INVALID to your translation files
      });
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    // The authService.login method expects an object with email and password
    const credentials = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password,
    };
        console.log('LoginComponent: BEFORE calling authService.login() with credentials:', credentials.email); // Log 3

     this.authService.login(credentials).subscribe({
      next: () => {
        this.isLoading = false;
                console.log('LoginComponent: authService.login() successful in component subscribe.'); // Log 4 (on success)

        // Navigation is handled by AuthService
      },
      error: (errorResponse: HttpErrorResponse) => {
        this.isLoading = false;
        console.error('LoginComponent - API Error Response Status:', errorResponse.status);
        console.error('LoginComponent - API Error Response Body (errorResponse.error):', errorResponse.error);

        let displayMessage: string | null = null;

        if (errorResponse.error) {
          // Scenario 1: FastEndpoints typical validation error structure (e.g., from SendErrorsAsync or structured problem details)
          if (errorResponse.error.errors && typeof errorResponse.error.errors === 'object') {
            const errorsObject = errorResponse.error.errors;
            const messages: string[] = [];
            // Check for general errors (empty key) or specific field errors
            if (Array.isArray(errorsObject[''])) { // General errors
              messages.push(...errorsObject['']);
            }
            // You can also iterate over other keys if you expect field-specific errors
            // for (const key in errorsObject) {
            //   if (errorsObject.hasOwnProperty(key) && Array.isArray(errorsObject[key]) && key !== '') {
            //     messages.push(...errorsObject[key]);
            //   }
            // }
            if (messages.length > 0) {
              displayMessage = messages.join(' ');
            }
          }
          // Scenario 2: A single 'message' or 'Message' property in the error body
          else if (errorResponse.error.message) {
            displayMessage = errorResponse.error.message;
          } else if (errorResponse.error.Message) { // ASP.NET Core often uses PascalCase
            displayMessage = errorResponse.error.Message;
          }
          // Scenario 3: The error body itself is just a string (less common for structured APIs)
          else if (typeof errorResponse.error === 'string') {
            displayMessage = errorResponse.error;
          }
        }

        if (displayMessage) {
          this.errorMessage = displayMessage;
        } else {
          // Fallback to a generic translated message if no specific message could be parsed
          this.translate.get('AUTH.LOGIN_FAILED_GENERAL').subscribe((res: string) => {
            this.errorMessage = res;
          });
        }
      },
    });

    this.authService.login(credentials).subscribe({
      next: () => {
        // Navigation is handled within the authService.login method's tap operator
        // console.log('Login successful, navigation should occur.');
        this.isLoading = false;
      },
      error: (errorResponse: HttpErrorResponse) => {
        this.isLoading = false;
        if (errorResponse.status === 401 || errorResponse.status === 400) { // Unauthorized or Bad Request
            // Try to get message from backend response, otherwise use generic
            const backendMessage = errorResponse.error?.message || errorResponse.error?.Message; // ASP.NET Core might use PascalCase for Message
            if (backendMessage) {
                 this.errorMessage = backendMessage; // This assumes backend sends localized messages or keys
            } else {
                this.translate.get('AUTH.LOGIN_FAILED_GENERAL').subscribe((res: string) => {
                    this.errorMessage = res; // Add AUTH.LOGIN_FAILED_GENERAL to translation files
                });
            }
        } else {
          // Handle other types of errors (network, server-side 500, etc.)
          this.translate.get('AUTH.LOGIN_ERROR_NETWORK').subscribe((res: string) => {
            this.errorMessage = res; // Add AUTH.LOGIN_ERROR_NETWORK to translation files
          });
        }
        console.error('Login failed:', errorResponse);
      },
    });
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  // Helper to easily get form controls for template validation
  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}
