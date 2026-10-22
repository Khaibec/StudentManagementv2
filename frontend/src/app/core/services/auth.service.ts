import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { User, LoginRequest, LoginResponse, UserRole } from '../models/user.model';

const STORAGE_KEY = 'sm_current_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser$: Observable<User | null>;

  constructor(private http: HttpClient, private router: Router) {
    const savedUserJson = localStorage.getItem(STORAGE_KEY);
    let initialUser: User | null = null;
    if (savedUserJson) {
      try {
        const parsed = JSON.parse(savedUserJson);
        // Check expiration
        if (parsed.expiresAt && new Date(parsed.expiresAt) > new Date()) {
          initialUser = parsed;
        } else {
          localStorage.removeItem(STORAGE_KEY);
        }
      } catch {
        localStorage.removeItem(STORAGE_KEY);
      }
    }

    this.currentUserSubject = new BehaviorSubject<User | null>(initialUser);
    this.currentUser$ = this.currentUserSubject.asObservable();
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  public get isAuthenticated(): boolean {
    const user = this.currentUserValue;
    if (!user || !user.token) return false;
    if (user.expiresAt && new Date(user.expiresAt) <= new Date()) {
      this.logout();
      return false;
    }
    return true;
  }

  public get role(): UserRole | null {
    return this.currentUserValue?.role ?? null;
  }

  public get isStudent(): boolean {
    return this.role === 'STUDENT';
  }

  public get isTeacher(): boolean {
    return this.role === 'TEACHER';
  }

  public get isAdmin(): boolean {
    return this.role === 'ADMIN';
  }

  public getToken(): string | null {
    return this.currentUserValue?.token ?? null;
  }

  public hasRole(roles: UserRole[]): boolean {
    if (!this.role) return false;
    return roles.includes(this.role);
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, credentials).pipe(
      tap((res) => {
        const user: User = {
          userId: res.userId,
          username: res.username,
          fullName: res.fullName,
          role: res.role,
          token: res.token,
          expiresAt: res.expiresAt
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
        this.currentUserSubject.next(user);
      })
    );
  }

  loadCurrentUser(): Observable<User | null> {
    if (!this.getToken()) {
      return of(null);
    }
    return this.http.get<any>(`${environment.apiUrl}/auth/me`).pipe(
      map((res) => {
        const current = this.currentUserValue;
        if (current) {
          const updated: User = {
            ...current,
            username: res.username,
            fullName: res.fullName,
            email: res.email,
            role: res.role
          };
          localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
          this.currentUserSubject.next(updated);
          return updated;
        }
        return null;
      }),
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }

  logout(): void {
    // Optionally call logout endpoint on backend
    if (this.getToken()) {
      this.http.post(`${environment.apiUrl}/auth/logout`, {}).pipe(
        catchError(() => of(null))
      ).subscribe();
    }
    localStorage.removeItem(STORAGE_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }
}
