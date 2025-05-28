import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { AuthGuard } from './core/guards/auth.guard';
import { PageNotFoundComponent } from './features/page-not-found/page-not-found.component'; // Adjust path if needed

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { 
    path: 'dashboard', 
    component: DashboardComponent, 
    canActivate: [AuthGuard] 
  },
  { path: '', redirectTo: '/login', pathMatch: 'full' }, // Or '/dashboard' if you prefer landing there for authenticated users (guard will handle)
  
  // Wildcard route for 404 Page Not Found - MUST BE THE LAST ROUTE
  { path: '**', component: PageNotFoundComponent } 
];
