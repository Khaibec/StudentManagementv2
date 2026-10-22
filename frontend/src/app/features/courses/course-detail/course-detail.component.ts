import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CourseService } from '../../../core/services/course.service';
import { ContentService } from '../../../core/services/content.service';
import { AssignmentService } from '../../../core/services/assignment.service';
import { AuthService } from '../../../core/services/auth.service';
import { Course } from '../../../core/models/course.model';
import { Chapter } from '../../../core/models/content.model';
import { Assignment, CreateAssignmentRequest, UpdateAssignmentRequest } from '../../../core/models/assignment.model';

@Component({
  selector: 'app-course-detail',
  templateUrl: './course-detail.component.html',
  styleUrls: ['./course-detail.component.css']
})
export class CourseDetailComponent implements OnInit {
  courseId!: number;
  course: Course | null = null;
  chapters: Chapter[] = [];
  assignments: Assignment[] = [];
  loading = true;
  assignmentsLoading = false;
  errorMessage = '';
  isForbidden = false;
  isNotFound = false;

  activeTab: 'overview' | 'content' | 'assignments' = 'overview';

  // Assignment Modal State (Teacher / Admin)
  showAssignmentModal = false;
  editingAssignmentId: number | null = null;
  assignmentForm = {
    title: '',
    description: '',
    dueDate: '',
    maxScore: 100
  };
  assignmentModalError = '';
  savingAssignment = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    private contentService: ContentService,
    private assignmentService: AssignmentService,
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
        this.loadChapters();
        this.loadAssignments();
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

  loadChapters(): void {
    this.contentService.getCourseChapters(this.courseId).subscribe({
      next: (chaptersData) => {
        this.chapters = chaptersData;
      },
      error: () => {}
    });
  }

  loadAssignments(): void {
    this.assignmentsLoading = true;
    this.assignmentService.getCourseAssignments(this.courseId).subscribe({
      next: (data) => {
        this.assignments = data;
        this.assignmentsLoading = false;
      },
      error: () => {
        this.assignmentsLoading = false;
      }
    });
  }

  setTab(tab: 'overview' | 'content' | 'assignments'): void {
    this.activeTab = tab;
    if (tab === 'assignments' && this.assignments.length === 0) {
      this.loadAssignments();
    }
  }

  // Assignment CRUD Modals
  openCreateAssignmentModal(): void {
    this.editingAssignmentId = null;
    const defaultDate = new Date();
    defaultDate.setDate(defaultDate.getDate() + 7);
    this.assignmentForm = {
      title: '',
      description: '',
      dueDate: this.formatDateForInput(defaultDate),
      maxScore: 100
    };
    this.assignmentModalError = '';
    this.showAssignmentModal = true;
  }

  openEditAssignmentModal(a: Assignment): void {
    this.editingAssignmentId = a.id;
    this.assignmentForm = {
      title: a.title,
      description: a.description || '',
      dueDate: this.formatDateForInput(new Date(a.dueDate)),
      maxScore: a.maxScore
    };
    this.assignmentModalError = '';
    this.showAssignmentModal = true;
  }

  closeAssignmentModal(): void {
    this.showAssignmentModal = false;
    this.editingAssignmentId = null;
    this.assignmentModalError = '';
  }

  saveAssignment(): void {
    if (!this.assignmentForm.title.trim()) {
      this.assignmentModalError = 'Vui lòng nhập tiêu đề bài tập.';
      return;
    }

    if (!this.assignmentForm.dueDate) {
      this.assignmentModalError = 'Vui lòng chọn hạn nộp.';
      return;
    }

    if (this.assignmentForm.maxScore <= 0) {
      this.assignmentModalError = 'Thang điểm tối đa phải lớn hơn 0.';
      return;
    }

    this.savingAssignment = true;
    this.assignmentModalError = '';

    const payloadDueDate = new Date(this.assignmentForm.dueDate).toISOString();

    if (this.editingAssignmentId) {
      const updateReq: UpdateAssignmentRequest = {
        title: this.assignmentForm.title.trim(),
        description: this.assignmentForm.description,
        dueDate: payloadDueDate,
        maxScore: this.assignmentForm.maxScore,
        isActive: true
      };

      this.assignmentService.updateAssignment(this.editingAssignmentId, updateReq).subscribe({
        next: () => {
          this.savingAssignment = false;
          this.closeAssignmentModal();
          this.loadAssignments();
        },
        error: (err) => {
          this.savingAssignment = false;
          this.assignmentModalError = err.error?.message || 'Có lỗi xảy ra khi cập nhật bài tập.';
        }
      });
    } else {
      const createReq: CreateAssignmentRequest = {
        title: this.assignmentForm.title.trim(),
        description: this.assignmentForm.description,
        dueDate: payloadDueDate,
        maxScore: this.assignmentForm.maxScore,
        isActive: true
      };

      this.assignmentService.createAssignment(this.courseId, createReq).subscribe({
        next: () => {
          this.savingAssignment = false;
          this.closeAssignmentModal();
          this.loadAssignments();
        },
        error: (err) => {
          this.savingAssignment = false;
          this.assignmentModalError = err.error?.message || 'Có lỗi xảy ra khi tạo bài tập.';
        }
      });
    }
  }

  deleteAssignment(id: number): void {
    if (!confirm('Bạn có chắc chắn muốn xóa bài tập này? Tất cả bài nộp của học viên sẽ bị xóa.')) {
      return;
    }

    this.assignmentService.deleteAssignment(id).subscribe({
      next: () => {
        this.loadAssignments();
      },
      error: (err) => {
        alert(err.error?.message || 'Không thể xóa bài tập này.');
      }
    });
  }

  private formatDateForInput(date: Date): string {
    const pad = (n: number) => (n < 10 ? '0' + n : n);
    const yyyy = date.getFullYear();
    const mm = pad(date.getMonth() + 1);
    const dd = pad(date.getDate());
    const hh = pad(date.getHours());
    const min = pad(date.getMinutes());
    return `${yyyy}-${mm}-${dd}T${hh}:${min}`;
  }
}
