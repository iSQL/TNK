import { Routes } from '@angular/router';
import { PageNotFoundComponent } from '@features/page-not-found/page-not-found.component'; 
import { LoginComponent } from '@features/auth/login/login.component'; 
import { RegisterComponent } from '@features/auth/register/register.component'; 
import { DashboardComponent } from '@features/dashboard/dashboard.component'; 
import { AuthGuard } from '@core/guards/auth.guard';
import { UserRole } from '@core/models/user-role.enum'; 

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  
  // Updated Auth Routes
  { path: 'login', component: LoginComponent }, 
  { path: 'register', component: RegisterComponent },

  // Superadmin Feature Module (Lazy Loaded and Protected)
  {
    path: 'superadmin',
    loadChildren: () => import('@features/superadmin/superadmin.module').then(m => m.SuperadminModule), 
    canActivate: [AuthGuard],
    data: {
      expectedRoles: [UserRole.Admin]
    }
  },

  { path: '**', component: PageNotFoundComponent }, // Wildcard route for 404
];
