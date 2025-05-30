// src/app/features/superadmin/components/vendor-form/vendor-form.component.ts
import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

// Syncfusion Modules
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';

// Models
import { BusinessProfileAdminDTO } from '../../models/business-profile-admin.dto';

// Output event type
export interface VendorFormOutput {
  success: boolean;
  data?: Partial<BusinessProfileAdminDTO> & { vendorId?: string; name: string }; 
  id?: number; // For update
  refresh?: boolean; // Added refresh property
}


@Component({
  selector: 'app-superadmin-vendor-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    TextBoxModule,
    ButtonModule
  ],
  templateUrl: './vendor-form.component.html',
  styleUrls: ['./vendor-form.component.scss']
})
export class SuperadminVendorFormComponent implements OnInit, OnChanges {
  @Input() vendorData: BusinessProfileAdminDTO | null = null;
  @Input() isEditMode: boolean = false;
  @Output() formClose = new EventEmitter<VendorFormOutput>();

  vendorForm!: FormGroup;
  isLoading = false;

  constructor(private fb: FormBuilder, private translate: TranslateService) {}

  ngOnInit(): void {
    this.initForm();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['vendorData'] || changes['isEditMode']) {
      if (this.vendorForm) {
        this.updateFormForMode();
      }
    }
  }

  private initForm(): void {
    this.vendorForm = this.fb.group({
      vendorId: ['', [Validators.required]],
      name: ['', [Validators.required, Validators.minLength(3)]],
      address: [''],
      phoneNumber: [''],
      description: [''],
    });
    this.updateFormForMode();
  }

  private updateFormForMode(): void {
    if (this.isEditMode && this.vendorData) {
      this.vendorForm.patchValue({
        vendorId: this.vendorData.vendorId,
        name: this.vendorData.name,
        address: this.vendorData.address,
        phoneNumber: this.vendorData.phoneNumber,
        description: this.vendorData.description,
      });
      this.vendorForm.get('vendorId')?.disable();
    } else {
      this.vendorForm.reset();
      this.vendorForm.get('vendorId')?.enable();
    }
  }

  get formTitleKey(): string {
    return this.isEditMode ? 'SUPERADMIN.VENDORS.FORM.EDIT_TITLE' : 'SUPERADMIN.VENDORS.FORM.CREATE_TITLE';
  }

  get submitButtonKey(): string {
    return this.isEditMode ? 'COMMON.SAVE_CHANGES_BUTTON' : 'COMMON.CREATE_BUTTON';
  }

  onSubmit(): void {
    if (this.vendorForm.invalid) {
      this.vendorForm.markAllAsTouched();
      console.log("Form Invalid:", this.vendorForm.errors, this.vendorForm.value);
      return;
    }

    this.isLoading = true; // This component doesn't directly use isLoading for now, but good to have
    const formData = this.vendorForm.getRawValue();

    this.formClose.emit({
      success: true,
      data: {
        vendorId: this.isEditMode ? this.vendorData?.vendorId : formData.vendorId,
        name: formData.name,
        address: formData.address,
        phoneNumber: formData.phoneNumber,
        description: formData.description
      },
      id: this.isEditMode ? this.vendorData?.id : undefined,
      refresh: true // Explicitly set refresh to true on successful submission
    });
  }

  onCancel(): void {
    this.formClose.emit({ success: false, refresh: false }); // On cancel, no refresh needed
  }
}
