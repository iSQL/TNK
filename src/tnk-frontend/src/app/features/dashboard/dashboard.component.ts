import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common'; // For *ngIf, etc.
import { Router, RouterModule } from '@angular/router'; // For routerLink and navigation
import { AuthService } from '../../core/services/auth.service'; // Adjust path if needed

// Import Syncfusion ButtonModule if you plan to use ejs-button
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule, // For routerLink
    ButtonModule  // For ejs-button
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  userFirstName: string | null = null;
  isVendor = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userFirstName = this.authService.getFirstName();
    this.isVendor = this.authService.hasRole('Vendor'); // Assuming 'Vendor' is the role string

    // You can also subscribe to currentUser$ for reactive updates if needed
    // this.authService.currentUser$.subscribe(user => {
    //   if (user) {
    //     this.userFirstName = user.firstName || null;
    //     this.isVendor = this.authService.hasRole('Vendor'); // Re-check role based on current user state
    //   } else {
    //     this.userFirstName = null;
    //     this.isVendor = false;
    //   }
    // });
  }

  logout(): void {
    this.authService.logout();
    // The AuthService logout method should already navigate to '/login'
    // If not, you can add: this.router.navigate(['/login']);
  }

  navigateToManageProfile(): void {
    // Later, this will navigate to the actual business profile management page
    // For now, it can be a placeholder or navigate to a route you'll create soon
    this.router.navigate(['/vendor/my-profile']); // Example future route
    // Or just console.log('Navigate to manage profile - TBD');
  }
}
