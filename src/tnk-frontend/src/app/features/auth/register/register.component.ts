import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service'; // Correct path
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslateModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router); // Router is injected but not used, can be removed if not needed
  private translate = inject(TranslateService);

  registerForm: FormGroup;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  isLoading: boolean = false;

  constructor() {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      // Add confirmPassword if your UI and backend expect it
      // confirmPassword: ['', Validators.required],
      // Add other fields as required by your backend registration endpoint
      // e.g., firstName, lastName, etc.
    }
    // Add custom validator for confirmPassword if used
    // { validators: this.passwordMatchValidator }
    );
  }

  // Example password match validator (if you add confirmPassword)
  // passwordMatchValidator(form: FormGroup) {
  //   const password = form.get('password');
  //   const confirmPassword = form.get('confirmPassword');
  //   if (password && confirmPassword && password.value !== confirmPassword.value) {
  //     confirmPassword.setErrors({ passwordMismatch: true });
  //   } else if (confirmPassword) {
  //     confirmPassword.setErrors(null);
  //   }
  //   return null;
  // }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.markFormGroupTouched(this.registerForm);
      this.translate.get('VALIDATION.FORM_INVALID').subscribe((res: string) => {
        this.errorMessage = res; // You'll need to add FORM_INVALID to your translation files
      });
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;

    // The authService.register method expects the user data object
    // Ensure the structure matches your backend API's expected request body
    const registrationData = {
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
      // Map other form fields if you have them
    };

    this.authService.register(registrationData).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.translate.get('AUTH.REGISTER_SUCCESS').subscribe((res: string) => {
            this.successMessage = res; // Add AUTH.REGISTER_SUCCESS to translation files
        });
        // Navigation to login is handled in authService.register, or you can do it here:
        // setTimeout(() => this.router.navigate(['/login']), 2000); // Optional delay
        this.registerForm.reset();
      },
      error: (errorResponse: HttpErrorResponse) => {
        this.isLoading = false;
        if (errorResponse.error && errorResponse.error.errors) {
            // Handle ASP.NET Core Identity validation errors (often an object with arrays of strings)
            const errors = errorResponse.error.errors;
            let messages: string[] = [];
            for (const key in errors) {
                if (errors.hasOwnProperty(key)) {
                    messages = messages.concat(errors[key]);
                }
            }
            this.errorMessage = messages.join(' ');
        } else if (errorResponse.error?.message || errorResponse.error?.Message) {
             this.errorMessage = errorResponse.error.message || errorResponse.error.Message;
        } else if (errorResponse.status === 400) { // General Bad Request
            this.translate.get('AUTH.REGISTER_FAILED_BAD_REQUEST').subscribe((res: string) => {
                 this.errorMessage = res; // Add AUTH.REGISTER_FAILED_BAD_REQUEST
            });
        }
        else {
          this.translate.get('AUTH.REGISTER_ERROR_NETWORK').subscribe((res: string) => {
            this.errorMessage = res; // Add AUTH.REGISTER_ERROR_NETWORK
          });
        }
        console.error('Registration failed:', errorResponse);
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
  get email() { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }
  // get confirmPassword() { return this.registerForm.get('confirmPassword'); }
}
