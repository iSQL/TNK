import { Component, OnInit, ViewChild, OnDestroy, ChangeDetectorRef, ElementRef, AfterViewInit, LOCALE_ID, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subject, Subscription, Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, tap, finalize } from 'rxjs/operators';

import {
  GridModule,
  GridComponent,
  PageSettingsModel,
  CommandModel,
  ColumnModel,
  DataStateChangeEventArgs,
  NotifyArgs,
  PageService,
  SortService,
  CommandColumnService,
  EditService,
  FilterService,
  ToolbarService,
  ResizeService // Ensure ResizeService is imported
} from '@syncfusion/ej2-angular-grids';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { DialogModule } from '@syncfusion/ej2-angular-popups';
import { TextBoxModule, TextBoxComponent } from '@syncfusion/ej2-angular-inputs';
import { createSpinner, showSpinner, hideSpinner } from '@syncfusion/ej2-popups'; 
import { ToastUtility } from '@syncfusion/ej2-notifications';

import { SuperadminVendorService } from '../../services/vendor.service';
import { UserDetailsAdminDTO } from '../../models/user-details-admin.dto';
import { PagedResult } from '../../models/business-profile-admin.dto'; // Directly use PagedResult

@Component({
  selector: 'app-superadmin-user-list',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    FormsModule,
    GridModule,
    ButtonModule,
    DialogModule,
    TextBoxModule
  ],
  providers: [
    PageService,
    SortService,
    CommandColumnService,
    EditService,
    FilterService,
    ToolbarService,
    ResizeService // Provide ResizeService
  ],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit, OnDestroy, AfterViewInit {
  public gridData: UserDetailsAdminDTO[] = [];
  public pageSettings: PageSettingsModel;
  public initialPageSettings: PageSettingsModel;
  public gridHeight = '450px'; 

  @ViewChild('userGrid') public gridInstance?: GridComponent;
  @ViewChild('searchInput') public searchInput?: TextBoxComponent;
  @ViewChild('gridContainer') public gridContainerRef?: ElementRef<HTMLDivElement>;

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
      this.cdr.detectChanges();
    }
  }

  private searchTerms = new Subject<string>();
  private subscriptions: Subscription = new Subscription();
  private currentSearchTerm: string = '';

  constructor(
    private superadminService: SuperadminVendorService,
    public translate: TranslateService,
    private cdr: ChangeDetectorRef,
    @Inject(LOCALE_ID) private locale: string
  ) {
    // Initialize totalRecordsCount to 0
    this.initialPageSettings = { pageSize: 10, pageSizes: true, currentPage: 1, pageCount: 5, totalRecordsCount: 0 };
    this.pageSettings = { ...this.initialPageSettings };
  }

  ngOnInit(): void {
    this.setupSearchSubscription();
    this.loadUsers(); 
  }

  ngAfterViewInit(): void {
    if (this.gridContainerRef?.nativeElement) {
      createSpinner({ 
        target: this.gridContainerRef.nativeElement, 
        label: this.translate.instant('COMMON.LOADING') 
      });
      if (!this._isLoading) { 
        hideSpinner(this.gridContainerRef.nativeElement);
      }
    }
    // Call adjustGridHeight after a short delay to ensure grid is more likely to be initialized
    setTimeout(() => this.adjustGridHeight(), 0);
    window.addEventListener('resize', this.adjustGridHeight.bind(this));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.gridContainerRef?.nativeElement && this.gridContainerRef.nativeElement.querySelector('.e-spinner-pane')) {
      hideSpinner(this.gridContainerRef.nativeElement);
    }
    window.removeEventListener('resize', this.adjustGridHeight.bind(this));
  }
  
  private adjustGridHeight(): void {
    const availableHeight = window.innerHeight;
    const reservedSpace = 300; 
    this.gridHeight = Math.max(300, availableHeight - reservedSpace) + 'px'; 
    
    if (this.gridInstance && this.gridInstance.element) {
      this.gridInstance.height = this.gridHeight;
    }
    this.cdr.detectChanges();
  }

  private setupSearchSubscription(): void {
    const searchSubscription = this.searchTerms.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      tap(term => {
        this.isLoading = true;
        this.currentSearchTerm = term;
        this.pageSettings.currentPage = 1; 
      }),
      switchMap(term => this.fetchUsersObservable().pipe(
        catchError(error => {
          this.handleError(error, 'COMMON.ERROR_SEARCHING_DATA');
          // Return an empty PagedResult structure
          return of({ items: [], totalCount: 0, pageNumber: 1, pageSize: this.pageSettings.pageSize || 10, totalPages: 0, hasNextPage: false, hasPreviousPage: false } as PagedResult<UserDetailsAdminDTO>);
        })
      ))
    ).subscribe(
      (response: PagedResult<UserDetailsAdminDTO>) => { 
        if (response) { 
          this.gridData = response.items;
          this.pageSettings = {
            ...this.pageSettings,
            totalRecordsCount: response.totalCount || 0, 
            currentPage: response.pageNumber
          };
        } else { 
          this.gridData = [];
          this.pageSettings = { ...this.initialPageSettings, totalRecordsCount: 0, currentPage: 1 };
        }
        this.isLoading = false;
      }
    );
    this.subscriptions.add(searchSubscription);
  }

  loadUsers(): void {
    this.isLoading = true;
    const loadSubscription = this.fetchUsersObservable().pipe(
      catchError(error => {
        this.handleError(error, 'COMMON.ERROR_FETCHING_DATA');
        return of({ items: [], totalCount: 0, pageNumber: 1, pageSize: this.pageSettings.pageSize || 10, totalPages: 0, hasNextPage: false, hasPreviousPage: false } as PagedResult<UserDetailsAdminDTO>);
      }),
      finalize(() => this.isLoading = false)
    ).subscribe(
      (response: PagedResult<UserDetailsAdminDTO>) => {
         if (response) { 
          this.gridData = response.items;
          this.pageSettings = {
            ...this.pageSettings,
            totalRecordsCount: response.totalCount || 0, 
            currentPage: response.pageNumber
          };
        } else {
          this.gridData = [];
          this.pageSettings = { ...this.initialPageSettings, totalRecordsCount: 0, currentPage: 1 };
        }
      }
    );
    this.subscriptions.add(loadSubscription);
  }

  private fetchUsersObservable(): Observable<PagedResult<UserDetailsAdminDTO>> {
    const currentPage = this.pageSettings.currentPage || 1;
    const pageSize = this.pageSettings.pageSize || 10;
    // Expect the service to return PagedResult<UserDetailsAdminDTO> directly
    return this.superadminService.getUsers(currentPage, pageSize, this.currentSearchTerm);
  }

  public dataStateChange(state: DataStateChangeEventArgs): void {
    if (!this.gridInstance) return;

    if (state.action) {
      const actionArgs = state.action as NotifyArgs;
      if (actionArgs.requestType === 'paging') {
        if (state.skip !== undefined && state.take !== undefined) {
          this.pageSettings.currentPage = Math.floor(state.skip / state.take) + 1;
          this.pageSettings.pageSize = state.take;
        }
        this.loadUsers();
      } else if (actionArgs.requestType === 'sorting') {
        // Client-side sorting is default.
      }
    }
  }

  public onSearchInput(event: Event | string): void { 
    let searchTerm = '';
    if (typeof event === 'string') {
      searchTerm = event;
    } else if (event?.target) {
      searchTerm = (event.target as HTMLInputElement).value;
    }
    this.searchTerms.next(searchTerm.trim());
  }

  public applySearch(): void {
    const searchTerm = this.searchInput?.value || '';
    this.onSearchInput(searchTerm); 
  }

  public clearSearch(): void {
    if (this.searchInput) {
      this.searchInput.value = '';
    }
    this.onSearchInput(''); 
  }

  public rolesValueAccessor = (field: string, data: UserDetailsAdminDTO, column: ColumnModel) => {
    return data.roles.join(', ');
  };
  
  public emailConfirmedValueAccessor = (field: string, data: UserDetailsAdminDTO, column: ColumnModel) => {
    return this.translate.instant(data.emailConfirmed ? 'COMMON.YES' : 'COMMON.NO');
  }

  private handleError(error: any, messageKey: string): void {
    this.isLoading = false; 
    console.error(`Error for key ${messageKey}:`, error); 
    this.showToast('error', this.translate.instant(messageKey), this.translate.instant('COMMON.ERROR_TITLE'));
    this.gridData = [];
    this.pageSettings = { ...this.initialPageSettings, totalRecordsCount: 0, currentPage: 1 };
    this.cdr.detectChanges(); 
  }

  private showToast(type: 'success' | 'error' | 'warning' | 'info', content: string, title?: string): void {
    const toastTitleKey = `COMMON.${type.toUpperCase()}_TITLE`;
    const toastTitle = title || this.translate.instant(toastTitleKey);
    
    if (ToastUtility && typeof ToastUtility.show === 'function') {
        ToastUtility.show({
          title: toastTitle,
          content: content,
          cssClass: `e-toast-${type.toLowerCase()}`, 
          timeOut: 5000,
          position: { X: 'Right', Y: 'Top' },
          showCloseButton: true,
        });
    } else {
        console.warn(`ToastUtility not available. Toast not shown: ${type} - ${title} - ${content}`);
    }
  }
}
