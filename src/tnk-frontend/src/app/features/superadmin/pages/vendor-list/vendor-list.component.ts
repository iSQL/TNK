// src/app/features/superadmin/pages/vendor-list/vendor-list.component.ts
import { Component, OnInit, ViewChild, OnDestroy, ChangeDetectorRef, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, debounceTime, distinctUntilChanged, switchMap, Observable } from 'rxjs';

// Syncfusion Modules & Functions
import {
  GridModule,
  GridComponent,
  PageSettingsModel,
  EditSettingsModel,
  CommandModel,
  CommandClickEventArgs,
  ActionEventArgs,
  DataStateChangeEventArgs,
  NotifyArgs,
  PageService,
  SortService,
  CommandColumnService,
  EditService
} from '@syncfusion/ej2-angular-grids';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups'; // Added Toast, ToastModel
import { TextBoxModule, TextBoxComponent } from '@syncfusion/ej2-angular-inputs';
import { createSpinner, showSpinner, hideSpinner } from '@syncfusion/ej2-popups';
import { ToastUtility } from '@syncfusion/ej2-notifications';


// Project Services & Models
import { SuperadminVendorService } from '../../services/vendor.service';
import { BusinessProfileAdminDTO, PagedResult } from '../../models/business-profile-admin.dto';
import { SuperadminVendorFormComponent, VendorFormOutput } from '../../components/vendor-form/vendor-form.component';
import { CreateBusinessProfileAdminRequest } from '../../models/create-business-profile-admin.request';
import { UpdateBusinessProfileAdminRequest } from '../../models/update-business-profile-admin.request';

@Component({
  selector: 'app-superadmin-vendor-list',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    FormsModule,
    GridModule,
    ButtonModule,
    DialogModule,
    TextBoxModule,
    SuperadminVendorFormComponent
    // Note: Syncfusion Toast is often used programmatically or via a global instance,
    // so ToastModule might not be needed in 'imports' if you use ToastUtility or a global Toast.
    // For this example, we'll assume programmatic usage or a template-defined toast if you add one.
  ],
  providers: [
    PageService,
    SortService,
    CommandColumnService,
    EditService
    
  ],
  templateUrl: './vendor-list.component.html',
  styleUrls: ['./vendor-list.component.scss']
})
export class VendorListComponent implements OnInit, OnDestroy, AfterViewInit {
  public gridData: BusinessProfileAdminDTO[] = [];
  public pageSettings: PageSettingsModel;
  public editSettings: EditSettingsModel;
  public commands: CommandModel[];
  
  private _isLoading = false;
  get isLoading(): boolean {
    return this._isLoading;
  }
  set isLoading(value: boolean) {
    if (this._isLoading !== value) {
      this._isLoading = value;
      if (this.gridContainerRef?.nativeElement) {
        if (value) {
          showSpinner(this.gridContainerRef.nativeElement);
        } else {
          hideSpinner(this.gridContainerRef.nativeElement);
        }
      }
    }
  }

  public isDialogVisible = false;
  public isEditMode = false;
  public selectedVendor: BusinessProfileAdminDTO | null = null;

  @ViewChild('vendorGrid') public gridInstance?: GridComponent;
  @ViewChild('vendorDialog') public vendorDialog?: DialogComponent;
  @ViewChild('searchInput') public searchInput?: TextBoxComponent;
  @ViewChild('gridContainer') public gridContainerRef?: ElementRef<HTMLDivElement>;

  @ViewChild('confirmDialog') public confirmDialog!: DialogComponent;
  public isConfirmDialogVisible = false;
  public vendorToDelete: BusinessProfileAdminDTO | null = null;

  // For Syncfusion Toast (if defined in template, otherwise use ToastUtility)
  // @ViewChild('toastElement') private toastObj?: ToastComponent;
  // public toastSettings: ToastModel = {
  //   position: { X: 'Right', Y: 'Top' },
  //   showCloseButton: true,
  //   newestOnTop: true,
  //   showProgressBar: true
  // };

  private searchTerms = new Subject<string>();
  private searchSubscription!: Subscription;
  private currentSearchTerm: string = '';
  private dataSubscription!: Subscription;


  constructor(
    private vendorService: SuperadminVendorService,
    private translate: TranslateService,
    private cdr: ChangeDetectorRef
    ) {
    this.pageSettings = { pageSize: 10, pageSizes: true, currentPage: 1, pageCount: 5, totalRecordsCount: 0 };
    this.editSettings = { allowEditing: false, allowAdding: false, allowDeleting: false, mode: 'Dialog' };
    this.commands = [
      { type: 'Edit', 
        buttonOption: { 
          cssClass: 'e-flat', 
          iconCss: 'e-icons e-edit'
        } },
      { 
        type: 'Delete', 
        buttonOption: { 
          cssClass: 'e-flat', 
          iconCss: 'e-icons e-delete'
        } 
      }
    ];
  }

  ngOnInit(): void {
    this.loadVendors();

    this.searchSubscription = this.searchTerms.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap((term: string) => {
        this.isLoading = true;
        this.currentSearchTerm = term;
        this.pageSettings.currentPage = 1;
        return this.fetchVendorsObservable();
      })
    ).subscribe(data => {
      this.gridData = data.items;
      this.pageSettings = { 
          ...this.pageSettings,
          totalRecordsCount: data.totalCount,
          currentPage: data.pageNumber
      };
      this.isLoading = false;
      this.cdr.detectChanges();
    }, error => {
      this.isLoading = false;
      this.showToast('error', this.translate.instant('COMMON.ERROR_FETCHING_DATA'));
      console.error('Error during search:', error);
    });
  }

  ngAfterViewInit(): void {
    if (this.gridContainerRef?.nativeElement) {
        createSpinner({ target: this.gridContainerRef.nativeElement, label: this.translate.instant('COMMON.LOADING') });
        if (!this._isLoading) {
            hideSpinner(this.gridContainerRef.nativeElement);
        }
    }
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
    if (this.dataSubscription) {
      this.dataSubscription.unsubscribe();
    }
    if (this.gridContainerRef?.nativeElement) {
        hideSpinner(this.gridContainerRef.nativeElement);
    }
  }

  loadVendors(): void {
    this.isLoading = true;
    if (this.dataSubscription) {
        this.dataSubscription.unsubscribe();
    }
    this.dataSubscription = this.fetchVendorsObservable().subscribe(
      (data: PagedResult<BusinessProfileAdminDTO>) => {
        this.gridData = data.items;
        this.pageSettings = { 
            ...this.pageSettings,
            totalRecordsCount: data.totalCount,
            currentPage: data.pageNumber
        };
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      (error) => {
        this.isLoading = false;
        this.showToast('error', this.translate.instant('COMMON.ERROR_FETCHING_DATA'));
        console.error('Error fetching vendors:', error);
      }
    );
  }

  private fetchVendorsObservable(): Observable<PagedResult<BusinessProfileAdminDTO>> {
    const currentPage = this.pageSettings.currentPage || 1;
    const pageSize = this.pageSettings.pageSize || 10;
    return this.vendorService.getVendors(currentPage, pageSize, this.currentSearchTerm);
  }

  dataStateChange(state: DataStateChangeEventArgs): void {
    if (state.action) {
        const actionArgs = state.action as NotifyArgs;
        if (actionArgs.requestType === 'paging') {
            if (state.skip !== undefined && state.take !== undefined) {
                this.pageSettings.currentPage = Math.floor(state.skip / state.take) + 1;
                this.pageSettings.pageSize = state.take;
            }
            this.loadVendors();
        }
    }
  }

  actionBegin(args: ActionEventArgs): void {
  console.log('actionBegin triggered. Event Arguments:', args);
  console.log('Request Type:', args.requestType);

  if ((args.requestType as string) === 'commandClick') {
    console.log('Command click detected!');
    const commandArgs = args as CommandClickEventArgs;
    console.log('Command Column Type:', commandArgs.commandColumn?.type);
    console.log('Row Data:', commandArgs.rowData);

    const vendorData = commandArgs.rowData as BusinessProfileAdminDTO;
    if (commandArgs.commandColumn?.type === 'Edit') {
      console.log('Edit command identified. Opening edit dialog...');
      args.cancel = true;
      this.openEditVendorDialog(vendorData);
    } else if (commandArgs.commandColumn?.type === 'Delete') {
      console.log('Delete command identified. Opening delete dialog...');
      args.cancel = true;
      this.openDeleteConfirmDialog(vendorData);
    } else {
      console.log('Command type not recognized:', commandArgs.commandColumn?.type);
    }
  }
}

onCommandClick(args: CommandClickEventArgs) {
  const vendor = args.rowData as BusinessProfileAdminDTO;

  switch (args.commandColumn?.type) {
    case 'Edit':
      this.openEditVendorDialog(vendor);
      break;
    case 'Delete':
      this.openDeleteConfirmDialog(vendor);
      break;
  }

  // prevent any default action
  args.cancel = true;
}

  actionComplete(args: ActionEventArgs): void { /* For future use */ }

  onSearchInput(event: any): void {
    const searchTerm = (event.target as HTMLInputElement)?.value || event.value || '';
    this.searchTerms.next(searchTerm.trim());
  }

  applySearch(): void {
    const searchTerm = this.searchInput?.value || '';
    this.searchTerms.next(searchTerm.trim());
  }

  openCreateVendorDialog(): void {
    this.isEditMode = false;
    this.selectedVendor = null;
    this.isDialogVisible = true;
    this.vendorDialog?.show();
  }

  openEditVendorDialog(vendor: BusinessProfileAdminDTO): void {
    this.isEditMode = true;
    this.selectedVendor = { ...vendor };
    this.isDialogVisible = true;
    this.vendorDialog?.show();
  }

  dialogClose(): void {
    this.isDialogVisible = false;
    this.selectedVendor = null;
  }

  handleFormClose(output: VendorFormOutput): void {
    this.isDialogVisible = false; // Always close dialog
    if (output.success && output.data) {
      this.isLoading = true;
      if (this.isEditMode && output.id) {
        // UPDATE
        const updatePayload: UpdateBusinessProfileAdminRequest = {
          name: output.data.name,
          address: output.data.address,
          phoneNumber: output.data.phoneNumber,
          description: output.data.description
        };
        this.vendorService.updateVendor(output.id, updatePayload).subscribe({
          next: () => {
            this.isLoading = false;
            this.showToast('success', this.translate.instant('SUPERADMIN.VENDORS.LIST.UPDATE_SUCCESS_MESSAGE'));
            this.loadVendors(); // Refresh grid
          },
          error: (err) => {
            this.isLoading = false;
            this.showToast('error', this.translate.instant('SUPERADMIN.VENDORS.LIST.UPDATE_ERROR_MESSAGE'));
            console.error('Error updating vendor:', err);
          }
        });
      } else if (!this.isEditMode && output.data.vendorId) {
        // CREATE
        const createPayload: CreateBusinessProfileAdminRequest = {
          vendorId: output.data.vendorId, // Ensure vendorId is present for create
          name: output.data.name,
          address: output.data.address,
          phoneNumber: output.data.phoneNumber,
          description: output.data.description
        };
        this.vendorService.createVendor(createPayload).subscribe({
          next: () => {
            this.isLoading = false;
            this.showToast('success', this.translate.instant('SUPERADMIN.VENDORS.LIST.CREATE_SUCCESS_MESSAGE'));
            this.loadVendors(); // Refresh grid
          },
          error: (err) => {
            this.isLoading = false;
            this.showToast('error', this.translate.instant('SUPERADMIN.VENDORS.LIST.CREATE_ERROR_MESSAGE'));
            console.error('Error creating vendor:', err);
          }
        });
      } else {
         this.isLoading = false; // Should not happen if form is valid
         console.error('Form output data is insufficient for create/update.', output.data);
      }
    }
    this.selectedVendor = null; // Clear selected vendor regardless
  }

  openDeleteConfirmDialog(vendor: BusinessProfileAdminDTO): void {
    this.vendorToDelete = vendor;
    this.isConfirmDialogVisible = true;
    this.confirmDialog?.show();
  }

  deleteConfirmed(): void {
    if (this.vendorToDelete) {
      this.isLoading = true;
      this.vendorService.deleteVendor(this.vendorToDelete.id).subscribe({
        next: () => {
          this.isLoading = false;
          this.showToast('success', this.translate.instant('SUPERADMIN.VENDORS.LIST.DELETE_SUCCESS_MESSAGE', { name: this.vendorToDelete?.name }));
          this.loadVendors();
          this.vendorToDelete = null;
          this.confirmDialog.hide();
          this.isConfirmDialogVisible = false;
        },
        error: (err) => {
          this.isLoading = false;
          this.showToast('error', this.translate.instant('SUPERADMIN.VENDORS.LIST.DELETE_ERROR_MESSAGE'));
          console.error('Error deleting vendor:', err);
          this.vendorToDelete = null;
          this.confirmDialog.hide();
          this.isConfirmDialogVisible = false;
        }
      });
    }
  }

  // Helper for showing toasts (using ToastUtility for simplicity)
  private showToast(type: 'success' | 'error' | 'warning' | 'info', content: string, title?: string): void {
    const toastTitle = title || this.translate.instant(`COMMON.${type.toUpperCase()}`); // e.g., COMMON.SUCCESS
    let cssClass = `e-toast-${type}`; // e.g., e-toast-success

    ToastUtility.show({
      title: toastTitle,
      content: content,
      cssClass: cssClass,
      timeOut: 5000,
      position: { X: 'Right', Y: 'Top' },
      showCloseButton: true,
      // icon: `e-icons e-${type}-icon` // You might need to define these CSS classes for icons
    });
  }
}
