import { Component, OnInit } from '@angular/core';
import { CourseService } from '../../../core/services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { Course, CreateCourseRequest, UpdateCourseRequest } from '../../../core/models/course.model';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-course-list',
  templateUrl: './course-list.component.html',
  styleUrls: ['./course-list.component.css']
})
export class CourseListComponent implements OnInit {
  courses: Course[] = [];
  loading = false;
  errorMessage = '';

  // Modal create/edit
  showModal = false;
  isEditing = false;
  editingCourseId: number | null = null;
  courseForm!: FormGroup;
  modalLoading = false;
  modalError = '';

  // Delete confirmation
  showDeleteModal = false;
  deletingCourse: Course | null = null;
  deleteLoading = false;
  deleteError = '';

  constructor(
    private courseService: CourseService,
    public authService: AuthService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadCourses();
  }

  initForm(): void {
    this.courseForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      title: ['', [Validators.required, Validators.maxLength(250)]],
      description: [''],
      status: ['Active', [Validators.required]]
    });
  }

  loadCourses(): void {
    this.loading = true;
    this.errorMessage = '';
    this.courseService.getCourses().subscribe({
      next: (data) => {
        this.courses = data;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 403) {
          this.errorMessage = 'Bạn không có quyền xem danh sách khóa học này.';
        } else {
          this.errorMessage = 'Không thể tải danh sách khóa học. Vui lòng thử lại sau.';
        }
      }
    });
  }

  canManageCourse(course: Course): boolean {
    if (this.authService.isAdmin) return true;
    if (this.authService.isTeacher && course.teacherId === this.authService.currentUserValue?.userId) {
      return true;
    }
    return false;
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingCourseId = null;
    this.modalError = '';
    this.courseForm.reset({
      code: '',
      title: '',
      description: '',
      status: 'Active'
    });
    this.courseForm.get('code')?.enable();
    this.showModal = true;
  }

  openEditModal(course: Course): void {
    this.isEditing = true;
    this.editingCourseId = course.id;
    this.modalError = '';
    this.courseForm.patchValue({
      code: course.code,
      title: course.title,
      description: course.description || '',
      status: course.status
    });
    // Code cannot be edited once created
    this.courseForm.get('code')?.disable();
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.modalLoading = false;
    this.modalError = '';
  }

  saveCourse(): void {
    if (this.courseForm.invalid) {
      this.courseForm.markAllAsTouched();
      return;
    }

    this.modalLoading = true;
    this.modalError = '';

    const formVal = this.courseForm.getRawValue();

    if (this.isEditing && this.editingCourseId) {
      const updateData: UpdateCourseRequest = {
        title: formVal.title,
        description: formVal.description,
        status: formVal.status
      };

      this.courseService.updateCourse(this.editingCourseId, updateData).subscribe({
        next: () => {
          this.modalLoading = false;
          this.closeModal();
          this.loadCourses();
        },
        error: (err) => {
          this.modalLoading = false;
          this.modalError = err.error?.message || 'Lỗi khi cập nhật khóa học.';
        }
      });
    } else {
      const createData: CreateCourseRequest = {
        code: formVal.code,
        title: formVal.title,
        description: formVal.description
      };

      this.courseService.createCourse(createData).subscribe({
        next: () => {
          this.modalLoading = false;
          this.closeModal();
          this.loadCourses();
        },
        error: (err) => {
          this.modalLoading = false;
          this.modalError = err.error?.message || 'Lỗi khi tạo khóa học.';
        }
      });
    }
  }

  confirmDelete(course: Course): void {
    this.deletingCourse = course;
    this.deleteError = '';
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.deletingCourse = null;
    this.deleteError = '';
  }

  executeDelete(): void {
    if (!this.deletingCourse) return;

    this.deleteLoading = true;
    this.deleteError = '';

    this.courseService.deleteCourse(this.deletingCourse.id).subscribe({
      next: () => {
        this.deleteLoading = false;
        this.closeDeleteModal();
        this.loadCourses();
      },
      error: (err) => {
        this.deleteLoading = false;
        this.deleteError = err.error?.message || 'Lỗi khi xóa khóa học.';
      }
    });
  }
}
