import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SuperadminLayoutComponent } from './layout/layout.component';
import { SuperadminDashboardComponent } from './pages/dashboard/dashboard.component';
import { VendorListComponent } from './pages/vendor-list/vendor-list.component';
// No need to import UserListComponent if it's standalone and loaded via loadComponent

const routes: Routes = [
  {
    path: '',
    component: SuperadminLayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }, // Default route for /superadmin
      {
        path: 'dashboard',
        component: SuperadminDashboardComponent,
        data: { title: 'SUPERADMIN.LAYOUT.NAV_DASHBOARD' } 
      },
      {
        path: 'vendors',
        component: VendorListComponent,
        data: { title: 'SUPERADMIN.LAYOUT.NAV_VENDORS' } 
      },
      {
        path: 'users',
        // For standalone components, use loadComponent
        loadComponent: () =>
          import('./pages/user-list/user-list.component').then(m => m.UserListComponent),
        data: { title: 'SUPERADMIN.LAYOUT.NAV_USERS' } // For breadcrumbs or page title service
      }
      // Add other superadmin child routes here
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SuperadminRoutingModule { }
