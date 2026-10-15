import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Leave } from './models';

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private http = inject(HttpClient);
  list(manager: boolean) { return this.http.get<Leave[]>(manager ? '/api/manager/leaves' : '/api/leaves/my'); }
  apply(request: { startDate: string; endDate: string; reason: string }) { return this.http.post<Leave>('/api/leaves', request); }
  cancel(id: number) { return this.http.delete<void>(`/api/leaves/${id}`); }
  decide(leave: Leave, status: 'Approved' | 'Rejected', managerComment: string) {
    return this.http.patch<Leave>(`/api/manager/leaves/${leave.id}/decision`, { status, managerComment, rowVersion: leave.rowVersion });
  }
}
