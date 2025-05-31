import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { HttpErrorResponse } from '@angular/common/http';

// Syncfusion Modules for the template
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';

// Custom validator for password matching
export function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (password && confirmPassword && password.value !== confirmPassword.value) {
    // Set error on confirmPassword field to display message there
    confirmPassword?.setErrors({ passwordMismatch: true }); // Use optional chaining for safety
    return { passwordMismatch: true }; // Also return form-level error if needed
  }
  // Clear error if passwords match and confirmPassword had the error
  if (confirmPassword?.hasError('passwordMismatch')) {
      confirmPassword.setErrors(null);
  }
  return null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    RouterModule,
    TranslateModule,
    // Syncfusion Modules
    TextBoxModule,
    ButtonModule,
    DropDownListModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private translate = inject(TranslateService);
  private router = inject(Router);

  registerForm!: FormGroup;
  registrationError: string | null = null; // Ensure this property is declared
  // successMessage: string | null = null; // You can keep this if you plan to use it
  isLoading: boolean = false;
  submitted: boolean = false;

  public rolesDataSource: { value: string, text: string }[] = [];
  public roleFields: Object = { text: 'text', value: 'value' };

  constructor() {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      role: [null, Validators.required]
    }, {
      validators: passwordMatchValidator
    });

    this.loadAndTranslateRoles();

    this.translate.onLangChange.subscribe(() => {
      this.loadAndTranslateRoles();
    });
  }

  loadAndTranslateRoles(): void {
    const roleKeys = [
      { value: 'Customer', key: 'AUTH.ROLE_CUSTOMER' },
      { value: 'Vendor', key: 'AUTH.ROLE_VENDOR' }
    ];
    const translationKeys = roleKeys.map(r => r.key);
    this.translate.get(translationKeys).subscribe(translations => {
      this.rolesDataSource = roleKeys.map(role => ({
        value: role.value,
        text: translations[role.key] || role.value
      }));
    });
  }

  get f() {
    return this.registerForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.registrationError = null;
    // this.successMessage = null; // Reset if you use it

    if (this.registerForm.invalid) {
      this.markFormGroupTouched(this.registerForm);
      // Optionally set a generic form invalid message using translate service if needed
      // this.translate.get('VALIDATION.FORM_INVALID').subscribe(msg => this.registrationError = msg);
      return;
    }

    this.isLoading = true;
    const formValues = this.registerForm.value;
    const registerData = {
      firstName: formValues.firstName,
      lastName: formValues.lastName,
      email: formValues.email,
      password: formValues.password,
      confirmPassword: formValues.confirmPassword,
      role: formValues.role
    };

    this.authService.register(registerData).subscribe({
      next: (response) => {
        this.isLoading = false;
        console.log('Registration successful', response);
        this.translate.get('AUTH.REGISTER_SUCCESS_ALERT').subscribe(msg => alert(msg));
        this.router.navigate(['/login']);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading = false;
        if (error.error && error.error.errors && error.error.errors.generalErrors && error.error.errors.generalErrors.length > 0) {
          this.registrationError = error.error.errors.generalErrors.join(' ');
        } else if (error.error && error.error.message) {
          this.registrationError = error.error.message;
        } else if (error.error && error.error.Message) {
          this.registrationError = error.error.Message;
        } else if (error.message) { // Fallback to HttpErrorResponse.message
          this.registrationError = error.message;
        } else {
          this.translate.get('AUTH.REGISTER_FAILED_GENERAL').subscribe(msg => this.registrationError = msg);
        }
        console.error('Registration error:', error);
      }
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
