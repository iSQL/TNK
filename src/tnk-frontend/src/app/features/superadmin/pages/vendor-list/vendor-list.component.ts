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
  // Import Grid services that are used
  PageService,
  SortService,
  CommandColumnService // For command columns
} from '@syncfusion/ej2-angular-grids';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { TextBoxModule, TextBoxComponent } from '@syncfusion/ej2-angular-inputs';
import { createSpinner, showSpinner, hideSpinner } from '@syncfusion/ej2-popups';


// Project Services & Models
import { SuperadminVendorService } from '../../services/vendor.service';
import { BusinessProfileAdminDTO, PagedResult } from '../../models/business-profile-admin.dto';
import { SuperadminVendorFormComponent, VendorFormOutput } from '../../components/vendor-form/vendor-form.component';

@Component({
  selector: 'app-superadmin-vendor-list',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    FormsModule,
    GridModule, // GridModule itself
    ButtonModule,
    DialogModule,
    TextBoxModule,
    SuperadminVendorFormComponent
  ],
  providers: [ // Provide necessary Syncfusion Grid services for standalone components
    PageService,    // For Paging
    SortService,    // For Sorting
    CommandColumnService // For Command Columns (Edit, Delete)
    // Add FilterService if allowFiltering is true and server-side,
    // Add EditService if using built-in grid editing modes,
    // Add ToolbarService if using built-in toolbar features extensively.
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
    this.editSettings = { allowEditing: false, allowAdding: false, allowDeleting: false, mode: 'Dialog' }; // Using custom dialogs
    this.commands = [
      { type: 'Edit', buttonOption: { cssClass: 'e-flat e-edit-icon', iconCss: 'e-icons e-edit-icon', /* click: this.onEditClick.bind(this) */ } },
      { type: 'Delete', buttonOption: { cssClass: 'e-flat e-delete-icon', iconCss: 'e-icons e-delete-icon', /* click: this.onDeleteClick.bind(this) */ } }
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
        // this.gridInstance?.goToPage(1); // Let loadVendors handle page settings for fetch
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
      console.error('Error during search:', error);
      // TODO: Show error toast
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
        console.error('Error fetching vendors:', error);
        this.isLoading = false;
        // TODO: Show error toast
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
        const actionArgs = state.action as NotifyArgs; // Cast to NotifyArgs to access requestType
        if (actionArgs.requestType === 'paging') {
            if (state.skip !== undefined && state.take !== undefined) {
                this.pageSettings.currentPage = Math.floor(state.skip / state.take) + 1;
                this.pageSettings.pageSize = state.take;
            }
            this.loadVendors();
        }
        // else if (actionArgs.requestType === 'sorting') {
        //   const sortColumn = state.sorted?.[0]?.name;
        //   const sortDirection = state.sorted?.[0]?.direction;
        //   // Update your service call with sorting parameters and call loadVendors()
        // }
    }
  }

  actionBegin(args: ActionEventArgs): void {
    if ((args.requestType as string) === 'commandClick') {
      const commandArgs = args as CommandClickEventArgs;
      const vendorData = commandArgs.rowData as BusinessProfileAdminDTO;
      if (commandArgs.commandColumn?.type === 'Edit') {
        args.cancel = true; // Prevent default grid action, we handle it manually
        this.openEditVendorDialog(vendorData);
      } else if (commandArgs.commandColumn?.type === 'Delete') {
        args.cancel = true; // Prevent default grid action
        this.openDeleteConfirmDialog(vendorData);
      }
    }
  }

  actionComplete(args: ActionEventArgs): void {
    // Can be used for post-action logic if needed
  }

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
    this.isDialogVisible = false;
    this.selectedVendor = null;
    if (output.success && output.refresh !== false) {
      this.loadVendors();
    }
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
          this.translate.get('SUPERADMIN.VENDORS.LIST.DELETE_SUCCESS_MESSAGE', { name: this.vendorToDelete?.name }).subscribe(msg => {
             alert(msg); // Replace with actual toast
          });
          this.loadVendors();
          this.vendorToDelete = null;
          this.confirmDialog.hide();
          this.isConfirmDialogVisible = false;
        },
        error: (err) => {
          this.isLoading = false;
          console.error('Error deleting vendor:', err);
           this.translate.get('SUPERADMIN.VENDORS.LIST.DELETE_ERROR_MESSAGE').subscribe(msg => {
            alert(msg); // Replace with actual toast
          });
          this.vendorToDelete = null;
          this.confirmDialog.hide();
          this.isConfirmDialogVisible = false;
        }
      });
    }
  }
}
