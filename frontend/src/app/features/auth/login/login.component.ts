import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loading = false;
  errorMessage = '';
  returnUrl = '/courses';

  testAccounts = [
    { label: 'Admin', username: 'admin', password: 'Admin@123', role: 'ADMIN' },
    { label: 'Giảng viên 1', username: 'teacher1', password: 'Teacher@123', role: 'TEACHER' },
    { label: 'Giảng viên 2', username: 'teacher2', password: 'Teacher@123', role: 'TEACHER' },
    { label: 'Sinh viên 1', username: 'student1', password: 'Student@123', role: 'STUDENT' },
    { label: 'Sinh viên 2', username: 'student2', password: 'Student@123', role: 'STUDENT' }
  ];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated) {
      this.router.navigate(['/courses']);
      return;
    }

    this.loginForm = this.fb.group({
      username: ['', [Validators.required]],
      password: ['', [Validators.required]]
    });

    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/courses';
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Tên đăng nhập hoặc mật khẩu không chính xác.';
      }
    });
  }

  fillAccount(acc: { username: string; password: string }): void {
    this.loginForm.patchValue({
      username: acc.username,
      password: acc.password
    });
    this.errorMessage = '';
  }
}
