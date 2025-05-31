import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterModule } from '@angular/router'; 
import { CommonModule } from '@angular/common'; 
import { Subscription } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { AuthService } from '@core/services/auth.service';
import { CurrentUserModel } from '@core/models/current-user.model';
import { UserRole } from '@core/models/user-role.enum';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';

@Component({
  selector: 'app-superadmin-layout', 
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TranslateModule,
    ButtonModule 
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class SuperadminLayoutComponent implements OnInit, OnDestroy {
  currentUser: CurrentUserModel | null = null;
  private currentUserSubscription!: Subscription;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.currentUserSubscription = this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  logout(): void {
    this.authService.logout();
  }

  ngOnDestroy(): void {
    if (this.currentUserSubscription) {
      this.currentUserSubscription.unsubscribe();
    }
  }
}
