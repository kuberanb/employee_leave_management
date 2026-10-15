import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/auth.interceptor';

@Component({ selector: 'app-login', imports: [ReactiveFormsModule], templateUrl: './login.html' })
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  readonly busy = signal(false);
  readonly error = signal(this.route.snapshot.queryParamMap.has('expired') ? 'Your session has expired. Please sign in again.' : '');
  readonly showPassword = signal(false);
  readonly form = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]], password: ['', Validators.required] });
  submit() {
    if (this.busy()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.busy.set(true); this.error.set('');
    const { email, password } = this.form.getRawValue();
    this.auth.login(email.trim(), password).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: session => void this.router.navigate([session.user.role === 'Manager' ? '/manager' : '/employee']),
      error: error => this.error.set(errorMessage(error))
    });
  }
}
