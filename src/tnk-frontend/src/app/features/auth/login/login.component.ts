import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router'; // Import RouterModule for routerLink
import { AuthService, LoginRequest } from '../../../core/services/auth.service'; // Adjust path as needed

// Syncfusion Modules for the template
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule, // Required for routerLink in the template
    TextBoxModule,  // Syncfusion TextBox
    ButtonModule    // Syncfusion Button
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup; // Definite assignment assertion
  submitted = false;
  isLoading = false;
  loginError: string | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // If user is already logged in, redirect to dashboard (optional)
    if (this.authService.isAuthenticated) {
      this.router.navigate(['/dashboard']);
    }

    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  // Convenience getter for easy access to form fields
  get f() {
    return this.loginForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.loginError = null; // Reset previous login errors

    // Stop here if form is invalid
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading = true;
    const loginRequest: LoginRequest = {
      email: this.f['email'].value,
      password: this.f['password'].value
    };

    this.authService.login(loginRequest).subscribe({
      next: (response) => {
        this.isLoading = false;
        console.log('Login successful', response);
        // Navigate to dashboard or returnUrl if it exists
        // For now, just navigate to dashboard
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.isLoading = false;
        this.loginError = error.message || 'Login failed. Please check your credentials.';
        console.error('Login error:', error);
      }
    });
  }
}
