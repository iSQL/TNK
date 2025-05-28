import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service'; 
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
  submitted: boolean = false; 

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(3)]], 
    });
  }

  ngOnInit(): void {
    this.authService.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigate(['/dashboard']); 
      }
    });
  }

  onSubmit(): void {
    
    this.submitted = true; 
    this.errorMessage = null; 
    this.isLoading = true; 

    if (this.loginForm.invalid) {
      this.markFormGroupTouched(this.loginForm);
      this.translate.get('VALIDATION.FORM_INVALID').subscribe((res: string) => { 
        this.errorMessage = res; 
      });
      this.isLoading = false; 
      return;
    }

    const credentials = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password,
    };

    this.authService.login(credentials).subscribe({
      next: () => {
        this.isLoading = false;
      },
      error: (errorResponse: HttpErrorResponse) => {
        this.isLoading = false;
        console.error('LoginComponent - API Error Response Status:', errorResponse.status); 
        console.error('LoginComponent - API Error Response Body (errorResponse.error):', JSON.stringify(errorResponse.error, null, 2)); 

        let displayMessage: string | null = null;
        if (errorResponse.error) {
          // Check for FastEndpoints typical structure with generalErrors (from your backend logs)
          if (errorResponse.error.errors && errorResponse.error.errors.generalErrors && Array.isArray(errorResponse.error.errors.generalErrors) && errorResponse.error.errors.generalErrors.length > 0) {
            displayMessage = errorResponse.error.errors.generalErrors.join(' '); 
            console.log('LoginComponent: Parsed generalErrors:', displayMessage);
          } 
          // Check for field-specific errors (example for 'Email' field if backend structures it this way)
          else if (errorResponse.error.errors && errorResponse.error.errors.Email && Array.isArray(errorResponse.error.errors.Email) && errorResponse.error.errors.Email.length > 0){
            displayMessage = errorResponse.error.errors.Email.join(' ');
            console.log('LoginComponent: Parsed Email field error:', displayMessage);
          }
          // Fallback for other common error message structures
          else if (errorResponse.error.message) { // Common for many APIs
            displayMessage = errorResponse.error.message;
          } else if (errorResponse.error.Message) { // Sometimes PascalCase from .NET
            displayMessage = errorResponse.error.Message;
          } else if (typeof errorResponse.error === 'string') { // If the error body is just a string
            displayMessage = errorResponse.error;
          } else if (errorResponse.error.title && typeof errorResponse.error.title === 'string') { // For RFC7807 ProblemDetails
             displayMessage = errorResponse.error.title;
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
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}
