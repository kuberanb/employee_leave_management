import { CanActivateFn, Router, Routes } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './core/auth.service';

const roleGuard = (role: 'Employee' | 'Manager'): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.token) return router.createUrlTree(['/login']);
  return auth.user()?.role === role || router.createUrlTree([auth.user()?.role === 'Manager' ? '/manager' : '/employee']);
};
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login').then(m => m.Login) },
  { path: 'employee', canActivate: [roleGuard('Employee')], loadComponent: () => import('./pages/dashboard').then(m => m.Dashboard) },
  { path: 'manager', canActivate: [roleGuard('Manager')], loadComponent: () => import('./pages/dashboard').then(m => m.Dashboard) },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' }
];
