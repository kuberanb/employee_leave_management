import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AbstractControl, FormBuilder, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { LeaveService } from '../core/leave.service';
import { Leave, LeaveStatus } from '../core/models';
import { errorMessage } from '../core/auth.interceptor';
import { calendarDays, todayInIndia } from '../core/dates';
import { DialogDirective } from '../core/dialog.directive';

function dateRange(control: AbstractControl): ValidationErrors | null {
  const { startDate, endDate } = control.value;
  if (startDate && startDate < todayInIndia()) return { pastDate: true };
  return startDate && endDate && endDate < startDate ? { dateRange: true } : null;
}

@Component({ selector: 'app-dashboard', imports: [DatePipe, ReactiveFormsModule, FormsModule, DialogDirective], templateUrl: './dashboard.html' })
export class Dashboard {
  readonly auth = inject(AuthService);
  private api = inject(LeaveService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  readonly manager = this.auth.user()?.role === 'Manager';
  readonly leaves = signal<Leave[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly showApply = signal(false);
  readonly selected = signal<Leave | null>(null);
  readonly cancelTarget = signal<Leave | null>(null);
  readonly filter = signal('All');
  readonly search = signal('');
  readonly today = todayInIndia();
  readonly statuses = ['All', 'Pending', 'Approved', 'Rejected'];
  comment = '';
  readonly form = this.fb.nonNullable.group({ startDate: ['', Validators.required], endDate: ['', Validators.required], reason: ['', [Validators.required, Validators.maxLength(1000), Validators.pattern(/\S/)]] }, { validators: dateRange });
  readonly counts = computed(() => ({ total: this.leaves().length, Pending: this.count('Pending'), Approved: this.count('Approved'), Rejected: this.count('Rejected') }));
  readonly filtered = computed(() => this.leaves().filter(leave => (this.filter() === 'All' || leave.status === this.filter()) && `${leave.employeeName} ${leave.reason}`.toLowerCase().includes(this.search().toLowerCase().trim())));
  constructor() { this.load(); }
  count(status: LeaveStatus) { return this.leaves().filter(leave => leave.status === status).length; }
  days() { const value = this.form.getRawValue(); return calendarDays(value.startDate, value.endDate); }
  load() {
    this.loading.set(true); this.error.set('');
    this.api.list(this.manager).pipe(finalize(() => this.loading.set(false))).subscribe({ next: leaves => this.leaves.set(leaves), error: error => this.error.set(errorMessage(error)) });
  }
  openApply() { this.form.reset(); this.error.set(''); this.success.set(''); this.showApply.set(true); }
  openDetails(leave: Leave) { this.comment = ''; this.error.set(''); this.selected.set(leave); }
  submit() {
    if (this.saving()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); this.error.set('');
    this.api.apply(this.form.getRawValue()).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: leave => { this.leaves.update(list => [leave, ...list]); this.showApply.set(false); this.success.set('Your leave request has been submitted.'); },
      error: error => this.error.set(errorMessage(error))
    });
  }
  cancel() {
    const leave = this.cancelTarget(); if (!leave || this.saving()) return;
    this.saving.set(true); this.error.set('');
    this.api.cancel(leave.id).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.leaves.update(list => list.filter(item => item.id !== leave.id)); this.cancelTarget.set(null); this.selected.set(null); this.success.set('Leave request cancelled.'); },
      error: error => this.error.set(errorMessage(error))
    });
  }
  decide(status: 'Approved' | 'Rejected') {
    const leave = this.selected(); if (!leave || this.saving()) return;
    this.saving.set(true); this.error.set('');
    this.api.decide(leave, status, this.comment).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: updated => { this.leaves.update(list => list.map(item => item.id === updated.id ? updated : item)); this.selected.set(null); this.success.set(`Request ${status.toLowerCase()}.`); },
      error: error => this.error.set(errorMessage(error))
    });
  }
  logout() { this.auth.logout(); void this.router.navigate(['/login']); }
  close() { if (!this.saving()) { this.showApply.set(false); this.selected.set(null); this.cancelTarget.set(null); this.error.set(''); } }
}
