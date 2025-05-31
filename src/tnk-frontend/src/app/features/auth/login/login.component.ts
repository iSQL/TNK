import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterModule } from '@angular/router'; 
import { AuthService } from '../../../core/services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';

// Syncfusion Modules for the template
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    RouterModule, 
    TranslateModule,
    TextBoxModule,
    ButtonModule
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
      password: ['', [Validators.required, Validators.minLength(6)]], // Or your preferred minLength
    });
  }

  ngOnInit(): void {
    this.authService.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigate(['/dashboard']); 
      }
    });
  }

  // Convenience getter for easy access to form fields in the template
  get f() { 
    return this.loginForm.controls; 
  }

  onSubmit(): void {
    
    this.submitted = true; 
    this.errorMessage = null; 
    
    if (this.loginForm.invalid) {

      this.markFormGroupTouched(this.loginForm);
      this.translate.get('AUTH.LOGIN_FAILED_GENERAL').subscribe((res: string) => { 
        this.errorMessage = res; 
      });
      return;
    }

    this.isLoading = true; 
    const credentials = {
      email: this.f['email'].value,
      password: this.f['password'].value,
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
          if (errorResponse.error.errors && errorResponse.error.errors.generalErrors && Array.isArray(errorResponse.error.errors.generalErrors) && errorResponse.error.errors.generalErrors.length > 0) {
            displayMessage = errorResponse.error.errors.generalErrors.join(' '); 
          } 
          else if (errorResponse.error.message) { 
            displayMessage = errorResponse.error.message;
          } else if (errorResponse.error.Message) { 
            displayMessage = errorResponse.error.Message;
          } else if (typeof errorResponse.error === 'string') { 
            displayMessage = errorResponse.error;
          } else if (errorResponse.error.title && typeof errorResponse.error.title === 'string') {
             displayMessage = errorResponse.error.title;
          }
        }

        if (displayMessage) {
          this.errorMessage = displayMessage;
        } else {
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
}
