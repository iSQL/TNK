import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
// Router is injected but not used. Remove if not needed for future dashboard actions.
// import { Router } from '@angular/router'; 
import { AuthService, User } from '../../core/services/auth.service'; // Adjust path as necessary
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, Subject } from 'rxjs';
import { takeUntil, filter, tap } from 'rxjs/operators';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  // private router = inject(Router); // Uncomment if you add navigation actions from dashboard
  private translate = inject(TranslateService);
  private cdr = inject(ChangeDetectorRef); // Inject ChangeDetectorRef for diagnostics

  // Subject to manage unsubscription
  private destroy$ = new Subject<void>();

  currentUser: User | null = null; // Store the user object directly
  welcomeMessage: string = '';

  constructor() {
    // No need to assign currentUser$ as a public property if only used in ngOnInit
  }

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(
        takeUntil(this.destroy$), // Unsubscribe when component is destroyed
        tap(user => console.log('DashboardComponent: currentUser$ emitted value:', JSON.stringify(user, null, 2))), // Log emission
        filter((user): user is User => user !== null && user.email !== null && user.email !== undefined && user.email.trim() !== '') // Ensure user and email are valid
      )
      .subscribe({
        next: (user) => {
          this.currentUser = user; // Store the valid user object
          
          console.log(`DashboardComponent: Valid user email found: '${user.email}'. Preparing to translate welcome message.`);
          this.translate.get("DASHBOARD.WELCOME_MESSAGE", { email: user.email })
          
            .pipe(takeUntil(this.destroy$)) // Also manage this subscription
            .subscribe({
              next: (translatedValue: string) => {
                console.log(`DashboardComponent: Translation result for 'DASHBOARD.WELCOME_MESSAGE': '${translatedValue}'`);
                this.welcomeMessage = translatedValue;
                // this.cdr.detectChanges(); // For debugging change detection - uncomment if needed
              },
              error: (err) => console.error('DashboardComponent: Error fetching welcome message translation:', err)
            });
        },
        // This error on currentUser$ is unlikely if AuthService handles its errors, but good practice
        error: (err) => console.error('DashboardComponent: Error subscribing to currentUser$:', err) 
      });

    // Handle the case where currentUser$ might initially be null or not meet filter criteria
    // This ensures a guest message is set if the filter doesn't pass or if there's an initial null emission.
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (!user || !user.email || user.email.trim() === '') {
          // Only set guest message if welcomeMessage hasn't already been set by a valid user
          if (!this.welcomeMessage) { 
            console.warn('DashboardComponent: User or user.email is null/undefined/empty. Setting guest message. User:', JSON.stringify(user, null, 2));
            this.translate.get('DASHBOARD.WELCOME_GUEST')
              .pipe(takeUntil(this.destroy$))
              .subscribe((res: string) => {
                this.welcomeMessage = res;
                console.log('DashboardComponent: Translated GUEST welcome message:', res);
                // this.cdr.detectChanges(); // For debugging - uncomment if needed
              });
            }
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    console.log('DashboardComponent: Unsubscribed from observables.');
  }

  logout(): void {
    this.authService.logout();
    // Navigation to login is handled by authService.logout()
  }
}
