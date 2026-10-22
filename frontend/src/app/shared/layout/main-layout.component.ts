import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { User, UserRole } from '../../core/models/user.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.css']
})
export class MainLayoutComponent implements OnInit {
  currentUser$: Observable<User | null>;

  constructor(public authService: AuthService) {
    this.currentUser$ = this.authService.currentUser$;
  }

  ngOnInit(): void {}

  onLogout(): void {
    this.authService.logout();
  }

  getRoleLabel(role?: UserRole): string {
    switch (role) {
      case 'ADMIN': return 'Quản trị viên';
      case 'TEACHER': return 'Giảng viên';
      case 'STUDENT': return 'Sinh viên';
      default: return '';
    }
  }

  getRoleBadgeClass(role?: UserRole): string {
    switch (role) {
      case 'ADMIN': return 'badge-admin';
      case 'TEACHER': return 'badge-teacher';
      case 'STUDENT': return 'badge-student';
      default: return '';
    }
  }
}
