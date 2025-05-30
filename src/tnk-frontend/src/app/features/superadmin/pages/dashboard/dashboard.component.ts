// src/app/features/superadmin/pages/dashboard/dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common'; // For *ngIf, etc.
import { TranslateModule } from '@ngx-translate/core';

// Syncfusion CardModule if you want to use <ejs-card> explicitly.
// For this example, we're relying on simple divs with e-card classes.
// import { CardModule } from '@syncfusion/ej2-angular-layouts';

@Component({
  selector: 'app-superadmin-dashboard', // Renamed selector
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    // CardModule // If using <ejs-card>
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class SuperadminDashboardComponent implements OnInit {
  totalVendors: number | null = null; // Placeholder, fetch from service later
  newSignups: number | null = null;   // Placeholder
  activeBookings: number | null = null; // Placeholder

  constructor() { }

  ngOnInit(): void {
    // Simulate fetching data - replace with actual service calls later
    this.loadDashboardMetrics();
  }

  loadDashboardMetrics(): void {
    // Simulate API call delay
    setTimeout(() => {
      this.totalVendors = 125;
      this.newSignups = 15;
      this.activeBookings = 340;
    }, 1000);
  }
}
