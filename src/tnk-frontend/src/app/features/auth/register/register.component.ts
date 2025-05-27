import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService, RegisterRequest } from '../../../core/services/auth.service'; // Adjust path as needed

// Syncfusion Modules for the template
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns'; // For ejs-dropdownlist

// Custom validator for password matching
export function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (password && confirmPassword && password.value !== confirmPassword.value) {
    return { passwordMismatch: true };
  }
  return null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule, // Required for routerLink in the template
    TextBoxModule,
    ButtonModule,
    DropDownListModule // Import Syncfusion DropDownListModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'] // Assuming you'll create a similar SCSS file
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  submitted = false;
  isLoading = false;
  registrationError: string | null = null;

  // Data source for the roles dropdown
  public roles: { value: string, text: string }[] = [
    { value: 'Customer', text: 'Customer (Kupac)' },
    { value: 'Vendor', text: 'Vendor (Prodavac)' }
    // Add other roles if needed, ensure 'value' matches what backend expects
  ];
  public roleFields: Object = { text: 'text', value: 'value' };


  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(3)]],
      confirmPassword: ['', Validators.required],
      role: [null, Validators.required] // Default to null, user must select
    }, {
      validators: passwordMatchValidator // Add custom validator to the form group
    });
  }

  // Convenience getter for easy access to form fields
  get f() {
    return this.registerForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.registrationError = null;

    if (this.registerForm.invalid) {
      // Log all form errors for debugging
      Object.keys(this.registerForm.controls).forEach(key => {
        const controlErrors = this.registerForm.get(key)?.errors;
        if (controlErrors != null) {
          console.log('Key control: ' + key + ', errors: ' + JSON.stringify(controlErrors));
        }
      });
      if(this.registerForm.errors?.['passwordMismatch']) {
        console.log('Form error: Password Mismatch');
      }
      return;
    }

    this.isLoading = true;
    const formValues = this.registerForm.value;

    const registerRequest: RegisterRequest = {
      firstName: formValues.firstName,
      lastName: formValues.lastName,
      email: formValues.email,
      password: formValues.password,
      confirmPassword: formValues.confirmPassword, // Backend might not need this if validated client-side
      role: formValues.role
    };

    this.authService.register(registerRequest).subscribe({
      next: (response) => {
        this.isLoading = false;
        console.log('Registration successful', response);
        // Optionally show a success message (e.g., using a toast notification)
        // For now, redirect to login page
        alert('Registration successful! Please login.'); // Simple alert for now
        this.router.navigate(['/login']);
      },
      error: (error) => {
        this.isLoading = false;
        this.registrationError = error.message || 'Registration failed. Please try again.';
        console.error('Registration error:', error);
      }
    });
  }
}
