// src/app/features/superadmin/superadmin-routing.module.ts
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SuperadminLayoutComponent } from './layout/layout.component'; // Import the layout

// Import your page components here when ready
// import { SuperadminDashboardComponent } from './pages/dashboard/dashboard.component';
// import { VendorListComponent } from './components/vendor-list/vendor-list.component';

const routes: Routes = [
  {
    path: '', // Base path for /superadmin
    component: SuperadminLayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }, // Default child
      // { path: 'dashboard', component: SuperadminDashboardComponent },
      // { path: 'vendors', component: VendorListComponent },
      // Add other child routes for superadmin section here
      // For now, let's add a temporary placeholder to test the layout
      {
        path: 'dashboard', // Temporary until actual dashboard is created
        loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.SuperadminDashboardComponent)
        // If SuperadminDashboardComponent is not standalone, you'd use:
        // component: SuperadminDashboardComponent
      },
       {
        path: 'vendors', // Temporary until actual vendor list is created
        loadComponent: () => import('./pages/vendor-list/vendor-list.component').then(m => m.VendorListComponent)
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SuperadminRoutingModule { }
