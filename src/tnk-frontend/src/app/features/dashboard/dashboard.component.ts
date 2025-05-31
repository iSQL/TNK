import { Component, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '@core/services/auth.service';
import { CurrentUserModel } from '@core/models/current-user.model';
import { UserRole } from '@core/models/user-role.enum';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; 
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule], // Added RouterModule
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  currentUser: CurrentUserModel | null = null;
  
  UserRoleEnum = UserRole; 

  private currentUserSubscription!: Subscription;

  constructor(public authService: AuthService) {} 

  ngOnInit(): void {
    this.currentUserSubscription = this.authService.currentUser$.subscribe(
      (user: CurrentUserModel | null) => { 
        this.currentUser = user;
      }
    );

    
    // });
  }

  ngOnDestroy(): void {
    if (this.currentUserSubscription) {
      this.currentUserSubscription.unsubscribe();
    }
    // if (this.userRoleSubscription) {
    //   this.userRoleSubscription.unsubscribe();
    // }
  }
}
