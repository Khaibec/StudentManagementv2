import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CourseService } from '../../../core/services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { Course } from '../../../core/models/course.model';

@Component({
  selector: 'app-course-detail',
  templateUrl: './course-detail.component.html',
  styleUrls: ['./course-detail.component.css']
})
export class CourseDetailComponent implements OnInit {
  courseId!: number;
  course: Course | null = null;
  loading = true;
  errorMessage = '';
  isForbidden = false;
  isNotFound = false;

  activeTab: 'overview' | 'content' | 'assignments' = 'overview';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const idParam = params.get('id');
      if (idParam) {
        this.courseId = +idParam;
        this.loadCourseDetail();
      } else {
        this.router.navigate(['/courses']);
      }
    });
  }

  loadCourseDetail(): void {
    this.loading = true;
    this.errorMessage = '';
    this.isForbidden = false;
    this.isNotFound = false;

    this.courseService.getCourseById(this.courseId).subscribe({
      next: (data) => {
        this.course = data;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 403) {
          this.isForbidden = true;
          this.errorMessage = '403 - Bạn không có quyền truy cập vào nội dung khóa học này.';
        } else if (err.status === 404) {
          this.isNotFound = true;
          this.errorMessage = '404 - Khóa học không tồn tại trong hệ thống.';
        } else {
          this.errorMessage = err.error?.message || 'Không thể tải thông tin khóa học.';
        }
      }
    });
  }

  setTab(tab: 'overview' | 'content' | 'assignments'): void {
    this.activeTab = tab;
  }
}
