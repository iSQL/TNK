import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AuthService, User } from '../../core/services/auth.service'; 
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
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
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  currentUser: User | null = null; 
  welcomeMessage: string = '';

  constructor() {
  }

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(
        takeUntil(this.destroy$), // Unsubscribe when component is destroyed
        filter((user): user is User => user !== null && user.email !== null && user.email !== undefined && user.email.trim() !== '') // Ensure user and email are valid
      )
      .subscribe({
        next: (user) => {
          this.currentUser = user; // Store the valid user object
          
          this.translate.get("DASHBOARD.WELCOME_MESSAGE", { email: user.email })
          
            .pipe(takeUntil(this.destroy$)) 
            .subscribe({
              next: (translatedValue: string) => {
                this.welcomeMessage = translatedValue;
              },
              error: (err) => console.error('DashboardComponent: Error fetching welcome message translation:', err)
            });
        },
        error: (err) => console.error('DashboardComponent: Error subscribing to currentUser$:', err) 
      });

    // This ensures a guest message is set if the filter doesn't pass or if there's an initial null emission.
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
              });
            }
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  logout(): void {
    this.authService.logout();
  }
}
