import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AssignmentService } from '../../../core/services/assignment.service';
import { AuthService } from '../../../core/services/auth.service';
import { AssignmentDetail, Submission, GradeSubmissionRequest } from '../../../core/models/assignment.model';

@Component({
  selector: 'app-assignment-grading',
  templateUrl: './assignment-grading.component.html',
  styleUrls: ['./assignment-grading.component.css']
})
export class AssignmentGradingComponent implements OnInit {
  courseId!: number;
  assignmentId!: number;
  assignment: AssignmentDetail | null = null;
  submissions: Submission[] = [];
  loading = true;
  errorMessage = '';
  isForbidden = false;

  // Filter
  filterStatus: 'all' | 'pending' | 'graded' = 'all';

  // Grading Modal State
  selectedSubmission: Submission | null = null;
  gradeScore: number | null = null;
  gradeFeedback = '';
  savingGrade = false;
  gradeError = '';

  constructor(
    private route: ActivatedRoute,
    public authService: AuthService,
    private assignmentService: AssignmentService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const cId = params.get('courseId');
      const aId = params.get('id');
      if (cId && aId) {
        this.courseId = +cId;
        this.assignmentId = +aId;
        this.loadData();
      }
    });
  }

  loadData(): void {
    this.loading = true;
    this.errorMessage = '';
    this.isForbidden = false;

    // Load assignment details
    this.assignmentService.getAssignmentById(this.assignmentId).subscribe({
      next: assign => {
        this.assignment = assign;
        // Load submissions
        this.loadSubmissions();
      },
      error: err => {
        this.loading = false;
        if (err.status === 403) {
          this.isForbidden = true;
          this.errorMessage = err.error?.message || 'Bạn không có quyền chấm bài cho bài tập này.';
        } else {
          this.errorMessage = 'Không thể tải thông tin bài tập.';
        }
      }
    });
  }

  loadSubmissions(): void {
    this.assignmentService.getAssignmentSubmissions(this.assignmentId).subscribe({
      next: subs => {
        this.submissions = subs;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err.status === 403) {
          this.isForbidden = true;
          this.errorMessage = err.error?.message || 'Bạn không có quyền xem danh sách bài nộp của khóa học này.';
        } else {
          this.errorMessage = 'Có lỗi xảy ra khi tải danh sách bài nộp.';
        }
      }
    });
  }

  get filteredSubmissions(): Submission[] {
    if (this.filterStatus === 'pending') {
      return this.submissions.filter(s => s.score === null || s.score === undefined);
    }
    if (this.filterStatus === 'graded') {
      return this.submissions.filter(s => s.score !== null && s.score !== undefined);
    }
    return this.submissions;
  }

  get gradedCount(): number {
    return this.submissions.filter(s => s.score !== null && s.score !== undefined).length;
  }

  get pendingCount(): number {
    return this.submissions.length - this.gradedCount;
  }

  openGradeModal(sub: Submission): void {
    this.selectedSubmission = sub;
    this.gradeScore = sub.score !== null && sub.score !== undefined ? sub.score : null;
    this.gradeFeedback = sub.feedback || '';
    this.gradeError = '';
  }

  closeGradeModal(): void {
    this.selectedSubmission = null;
    this.gradeScore = null;
    this.gradeFeedback = '';
    this.gradeError = '';
  }

  submitGrade(): void {
    if (!this.selectedSubmission || !this.assignment) return;

    if (this.gradeScore === null || isNaN(this.gradeScore)) {
      this.gradeError = 'Vui lòng nhập điểm số hợp lệ.';
      return;
    }

    if (this.gradeScore < 0 || this.gradeScore > this.assignment.maxScore) {
      this.gradeError = `Điểm số không hợp lệ. Điểm phải nằm trong thang điểm [0, ${this.assignment.maxScore}].`;
      return;
    }

    this.savingGrade = true;
    this.gradeError = '';

    const req: GradeSubmissionRequest = {
      score: this.gradeScore,
      feedback: this.gradeFeedback
    };

    this.assignmentService.gradeSubmission(this.selectedSubmission.id, req).subscribe({
      next: updated => {
        this.savingGrade = false;
        const index = this.submissions.findIndex(s => s.id === updated.id);
        if (index !== -1) {
          this.submissions[index] = updated;
        }
        this.closeGradeModal();
      },
      error: err => {
        this.savingGrade = false;
        this.gradeError = err.error?.message || 'Không thể lưu kết quả chấm điểm. Vui lòng thử lại.';
      }
    });
  }

  downloadSubmissionFile(submissionId: number, fileName: string): void {
    this.assignmentService.downloadSubmissionFile(submissionId).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        alert('Không thể tải tệp tin bài nộp.');
      }
    });
  }
}
