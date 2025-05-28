import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service'; // Adjust path as needed
import { Observable } from 'rxjs';

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, TranslateModule],
  templateUrl: './bottom-nav.component.html',
  styleUrls: ['./bottom-nav.component.scss'],
})
export class BottomNavComponent {
  private authService = inject(AuthService);
  isAuthenticated$: Observable<boolean>;

  constructor() {
    this.isAuthenticated$ = this.authService.isAuthenticated$;
  }

  // You can add more items here or make them dynamic based on roles later
  navItems = [
    { path: '/dashboard', iconClass: 'fa fa-tachometer-alt', labelKey: 'NAV.DASHBOARD' }, // Font Awesome example
    { path: '/profile', iconClass: 'fa fa-user', labelKey: 'NAV.PROFILE' },
    { path: '/settings', iconClass: 'fa fa-cog', labelKey: 'NAV.SETTINGS' },
  ];
}
