import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard'; // 1. Import your authGuard

export const routes: Routes = [
  // Authentication routes (public)
  {
    path: 'login',
    // We will create LoginComponent later in features/auth/login
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
    title: 'Login - TerminNaKlik' // Optional: for browser tab title
  },
  {
    path: 'register',
    // We will create RegisterComponent later in features/auth/register
    loadComponent: () =>
      import('./features/auth/register/register.component').then(m => m.RegisterComponent),
    title: 'Register - TerminNaKlik'
  },

  // Protected routes (require authentication)
  {
    path: 'dashboard',
    // We will create DashboardComponent later, perhaps in features/dashboard
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard], // 2. Protect this route with the authGuard
    title: 'Dashboard - TerminNaKlik'
  },
  // Add more protected routes here as needed, e.g., for vendor profile, services, etc.
  // {
  //   path: 'vendor/profile',
  //   loadComponent: () => import('./features/vendor-profile/profile.component').then(m => m.ProfileComponent),
  //   canActivate: [authGuard]
  // },


  // Default route: Redirect to login if no path is specified
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },

  // Wildcard route: Redirect to login (or a 'NotFoundComponent' later) for any unspecified paths
  {
    path: '**',
    redirectTo: '/login' // Or later: loadComponent: () => import('./core/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
