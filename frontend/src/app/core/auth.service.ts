import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { Session, User } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  readonly user = signal<User | null>(null);
  private session: Session | null = null;
  get token(): string | null {
    return this.session && Date.parse(this.session.expiresAt) > Date.now() ? this.session.token : null;
  }
  login(email: string, password: string) {
    return this.http.post<Session>('/api/auth/login', { email, password }).pipe(tap(session => {
      this.session = session;
      this.user.set(session.user);
    }));
  }
  logout() { this.session = null; this.user.set(null); }
}
