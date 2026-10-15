export interface User { id: string; name: string; email: string; role: 'Employee' | 'Manager'; }
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected';
export interface Leave {
  id: number; employeeName: string; employeeEmail: string; startDate: string; endDate: string;
  days: number; reason: string; status: LeaveStatus; managerComment: string | null;
  createdAt: string; decidedAt: string | null; decidedByName: string | null; rowVersion: string;
}
export interface Session { token: string; expiresAt: string; user: User; }
