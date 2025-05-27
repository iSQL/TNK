import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

// Import all necessary Syncfusion Modules
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
// CardModule is handled by global styles / base module, no explicit import needed as per recent feedback.
import { ChartModule, CategoryService, ColumnSeriesService, LegendService, TooltipService, DataLabelService, LineSeriesService } from '@syncfusion/ej2-angular-charts';
import { GridModule, PageService, SortService, FilterService, GroupService } from '@syncfusion/ej2-angular-grids';
import { ScheduleModule, DayService, WeekService, WorkWeekService, MonthService, AgendaService, ResizeService, DragAndDropService } from '@syncfusion/ej2-angular-schedule';


@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    ChartModule,
    GridModule,
    ScheduleModule
  ],
  providers: [
    CategoryService, ColumnSeriesService, LegendService, TooltipService, DataLabelService, LineSeriesService, // For Chart
    PageService, SortService, FilterService, GroupService, // For Grid
    DayService, WeekService, WorkWeekService, MonthService, AgendaService, ResizeService, DragAndDropService // For Schedule
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  userFirstName: string | null = null;
  isVendor = false;

  // Data for the Chart (Example: Monthly Bookings)
  public chartData: Object[] = [];
  public primaryXAxis: Object = {
    valueType: 'Category',
    title: 'Month'
  };
  public primaryYAxis: Object = {
    minimum: 0,
    maximum: 100,
    interval: 20,
    title: 'Bookings'
  };
  public chartTitle: string = 'Monthly Bookings Overview';
  public tooltip: Object = { enable: true };
  public legendSettings: Object = { visible: true };

  // Data for the DataGrid (Example: Recent Transactions)
  public gridData: Object[] = [];
  public gridPageSettings: Object = { pageCount: 5, pageSize: 5 };
  public gridSortSettings: Object = { columns: [{ field: 'OrderDate', direction: 'Descending' }] };
  public gridFilterSettings: Object = { type: 'Menu' };


  // Data for the Scheduler (Example: Appointments)
  public scheduleData: Object[] = [];
  public currentView: string = 'Week'; // Default view for scheduler
  public selectedDate: Date = new Date(); // Use current date


  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userFirstName = this.authService.getFirstName();
    this.isVendor = this.authService.hasRole('Vendor');

    // Populate dummy data for all components
    this.generateChartData();
    this.generateGridData();
    this.generateScheduleData();
  }

  logout(): void {
    this.authService.logout();
  }

  navigateToManageProfile(): void {
    this.router.navigate(['/vendor/my-profile']);
  }

  private generateChartData(): void {
    this.chartData = [
      { month: 'Jan', bookings: 35, revenue: 1500 },
      { month: 'Feb', bookings: 40, revenue: 1700 },
      { month: 'Mar', bookings: 55, revenue: 2200 },
      { month: 'Apr', bookings: 60, revenue: 2500 },
      { month: 'May', bookings: 75, revenue: 3000 },
      { month: 'Jun', bookings: 68, revenue: 2800 },
      { month: 'Jul', bookings: 80, revenue: 3200 },
      { month: 'Aug', bookings: 70, revenue: 2900 },
      { month: 'Sep', bookings: 85, revenue: 3500 },
      { month: 'Oct', bookings: 90, revenue: 3800 },
      { month: 'Nov', bookings: 78, revenue: 3100 },
      { month: 'Dec', bookings: 95, revenue: 4000 }
    ];
  }

  private generateGridData(): void {
    this.gridData = [
      { OrderID: 10248, CustomerName: 'John Doe', OrderDate: new Date('2025-05-20'), Amount: 120.00, Status: 'Completed' },
      { OrderID: 10249, CustomerName: 'Jane Smith', OrderDate: new Date('2025-05-21'), Amount: 75.50, Status: 'Pending' },
      { OrderID: 10250, CustomerName: 'Bob Johnson', OrderDate: new Date('2025-05-22'), Amount: 200.00, Status: 'Completed' },
      { OrderID: 10251, CustomerName: 'Alice Brown', OrderDate: new Date('2025-05-23'), Amount: 90.25, Status: 'Cancelled' },
      { OrderID: 10252, CustomerName: 'Chris Green', OrderDate: new Date('2025-05-24'), Amount: 300.00, Status: 'Completed' },
      { OrderID: 10253, CustomerName: 'Sarah White', OrderDate: new Date('2025-05-25'), Amount: 50.00, Status: 'Pending' },
      { OrderID: 10254, CustomerName: 'David Lee', OrderDate: new Date('2025-05-26'), Amount: 150.00, Status: 'Completed' },
      { OrderID: 10255, CustomerName: 'Emily Chen', OrderDate: new Date('2025-05-27'), Amount: 88.75, Status: 'Completed' }
    ];
  }

  private generateScheduleData(): void {
    // Current date (e.g., May 27, 2025)
    const today = new Date();
    const year = today.getFullYear();
    const month = today.getMonth(); // 0-indexed month

    this.scheduleData = [
      {
        Id: 1,
        Subject: 'Client Meeting - Project X',
        StartTime: new Date(year, month, today.getDate(), 10, 0),
        EndTime: new Date(year, month, today.getDate(), 11, 0),
        IsAllDay: false,
        EventType: 'Meeting',
        Description: 'Discuss project milestones and next steps.'
      },
      {
        Id: 2,
        Subject: 'Service: Full Haircut & Style',
        StartTime: new Date(year, month, today.getDate() + 1, 14, 0), // Tomorrow
        EndTime: new Date(year, month, today.getDate() + 1, 15, 30),
        IsAllDay: false,
        EventType: 'Service',
        Customer: 'Ms. Emily White',
        ServiceType: 'Hair'
      },
      {
        Id: 3,
        Subject: 'Team Stand-up',
        StartTime: new Date(year, month, today.getDate(), 9, 30),
        EndTime: new Date(year, month, today.getDate(), 10, 0),
        IsAllDay: false,
        EventType: 'Internal',
        Description: 'Daily team sync-up.'
      },
      {
        Id: 4,
        Subject: 'Product Demo for New Client',
        StartTime: new Date(year, month, today.getDate() + 2, 11, 0), // Two days from now
        EndTime: new Date(year, month, today.getDate() + 2, 12, 0),
        IsAllDay: false,
        EventType: 'Demo',
        Description: 'Showcase new features.'
      },
      {
        Id: 5,
        Subject: 'All-Day Workshop: Digital Marketing',
        StartTime: new Date(year, month, today.getDate() - 2, 9, 0), // Two days ago
        EndTime: new Date(year, month, today.getDate() - 2, 17, 0),
        IsAllDay: true,
        EventType: 'Workshop'
      }
    ];
  }
}
