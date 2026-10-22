import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssignmentService } from '../../../core/services/assignment.service';
import { AuthService } from '../../../core/services/auth.service';
import { AssignmentDetail, Submission } from '../../../core/models/assignment.model';

@Component({
  selector: 'app-assignment-detail',
  templateUrl: './assignment-detail.component.html',
  styleUrls: ['./assignment-detail.component.css']
})
export class AssignmentDetailComponent implements OnInit {
  courseId!: number;
  assignmentId!: number;
  assignment: AssignmentDetail | null = null;
  loading = true;
  errorMessage = '';
  isForbidden = false;

  // Submission Form State
  textContent = '';
  selectedFile: File | null = null;
  submitting = false;
  submitSuccess = '';
  submitError = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
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
        this.loadAssignment();
      }
    });
  }

  loadAssignment(): void {
    this.loading = true;
    this.errorMessage = '';
    this.isForbidden = false;

    this.assignmentService.getAssignmentById(this.assignmentId).subscribe({
      next: data => {
        this.assignment = data;
        this.loading = false;
        if (data.mySubmission) {
          this.textContent = data.mySubmission.textContent || '';
        }
      },
      error: err => {
        this.loading = false;
        if (err.status === 403) {
          this.isForbidden = true;
          this.errorMessage = err.error?.message || 'Bạn không có quyền truy cập bài tập này.';
        } else if (err.status === 404) {
          this.errorMessage = 'Không tìm thấy bài tập được yêu cầu.';
        } else {
          this.errorMessage = 'Có lỗi xảy ra khi tải thông tin bài tập.';
        }
      }
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      if (file.size > 25 * 1024 * 1024) {
        this.submitError = 'Kích thước tệp tin không được vượt quá 25MB.';
        this.selectedFile = null;
        event.target.value = '';
        return;
      }
      this.selectedFile = file;
      this.submitError = '';
    }
  }

  onSubmitAssignment(): void {
    if (!this.textContent.trim() && !this.selectedFile && !this.assignment?.mySubmission?.fileName) {
      this.submitError = 'Vui lòng nhập nội dung trả lời hoặc chọn một tệp tin đính kèm.';
      return;
    }

    this.submitting = true;
    this.submitError = '';
    this.submitSuccess = '';

    this.assignmentService.submitAssignment(this.assignmentId, this.textContent, this.selectedFile).subscribe({
      next: sub => {
        this.submitting = false;
        if (this.assignment) {
          this.assignment.mySubmission = sub;
        }
        this.submitSuccess = sub.status === 'SUBMITTED_LATE'
          ? 'Đã nộp bài tập thành công (Hệ thống ghi nhận: NỘP MUỘN/QUÁ HẠN).'
          : 'Đã nộp bài tập thành công đúng hạn!';
        this.selectedFile = null;
      },
      error: err => {
        this.submitting = false;
        this.submitError = err.error?.message || 'Có lỗi xảy ra khi nộp bài tập. Vui lòng thử lại.';
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
        alert('Không thể tải tệp tin bài nộp. Tệp tin có thể không còn tồn tại trên máy chủ.');
      }
    });
  }
}
