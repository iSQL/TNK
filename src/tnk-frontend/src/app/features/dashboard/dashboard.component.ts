import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService, User } from '../../core/services/auth.service'; 
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, Subject } from 'rxjs';
import { takeUntil, filter, tap } from 'rxjs/operators';

// Syncfusion Modules - CardModule is removed
import { ButtonModule } from '@syncfusion/ej2-angular-buttons'; // For ejs-button

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    TranslateModule,
    // CardModule, // Removed CardModule
    ButtonModule    // Keep ButtonModule for ejs-button
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private translate = inject(TranslateService);
  private cdr = inject(ChangeDetectorRef); 

  private destroy$ = new Subject<void>();

  currentUser: User | null = null; 
  welcomeMessage: string = '';

  constructor() {}

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(
        takeUntil(this.destroy$), 
        tap(user => console.log('DashboardComponent: currentUser$ emitted value:', JSON.stringify(user, null, 2))),
        filter((user): user is User => user !== null && user.email !== null && user.email !== undefined && user.email.trim() !== '')
      )
      .subscribe({
        next: (user) => {
          this.currentUser = user; 
          console.log(`DashboardComponent: Valid user email found: '${user.email}'. Preparing to translate welcome message.`);
          
          this.translate.get('DASHBOARD.WELCOME_MESSAGE', { email: user.email }) // Ensure placeholder is {{email}} in JSON
            .pipe(takeUntil(this.destroy$)) 
            .subscribe({
              next: (translatedValue: string) => {
                console.log(`DashboardComponent: Translation result for 'DASHBOARD.WELCOME_MESSAGE': '${translatedValue}'`);
                this.welcomeMessage = translatedValue;
                // this.cdr.detectChanges(); 
              },
              error: (err) => console.error('DashboardComponent: Error fetching welcome message translation:', err)
            });
        },
        error: (err) => console.error('DashboardComponent: Error subscribing to currentUser$:', err) 
      });

    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (!user || !user.email || user.email.trim() === '') {
          if (!this.welcomeMessage) { 
            console.warn('DashboardComponent: User or user.email is null/undefined/empty. Setting guest message. User:', JSON.stringify(user, null, 2));
            this.translate.get('DASHBOARD.WELCOME_GUEST')
              .pipe(takeUntil(this.destroy$))
              .subscribe((res: string) => {
                this.welcomeMessage = res;
                console.log('DashboardComponent: Translated GUEST welcome message:', res);
                // this.cdr.detectChanges(); 
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
  }
}
