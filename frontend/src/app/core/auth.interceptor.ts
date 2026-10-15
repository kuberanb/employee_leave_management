import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!request.url.startsWith('/api/')) return next(request);
  const token = auth.token;
  return next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request)
    .pipe(catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !request.url.endsWith('/login')) {
        auth.logout();
        void router.navigate(['/login'], { queryParams: { expired: 'true' } });
      }
      return throwError(() => error);
    }));
};

export function errorMessage(error: HttpErrorResponse): string {
  if (error.status === 0 || error.status === 502 || error.status === 504) return 'Cannot reach the server. Please try again shortly.';
  if (error.error?.message) return error.error.message;
  const messages = error.error?.errors ? Object.values(error.error.errors).flat() : [];
  return messages.length ? String(messages[0]) : 'Unable to complete the request. Please try again.';
}
