import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ContentService } from '../../../core/services/content.service';
import { CourseService } from '../../../core/services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { Course } from '../../../core/models/course.model';
import { Chapter, LessonDetail, LessonSummary, Resource } from '../../../core/models/content.model';

@Component({
  selector: 'app-course-learning',
  templateUrl: './course-learning.component.html',
  styleUrls: ['./course-learning.component.css']
})
export class CourseLearningComponent implements OnInit {
  courseId!: number;
  course: Course | null = null;
  chapters: Chapter[] = [];

  activeLessonId: number | null = null;
  activeLesson: LessonDetail | null = null;
  safeVideoUrl: SafeResourceUrl | null = null;

  loading = true;
  lessonLoading = false;
  errorMessage = '';
  isForbidden = false;

  // Student progress
  progressPercentage = 0;
  completedLessonsCount = 0;
  totalLessonsCount = 0;
  completing = false;

  // Chapter Modal
  showChapterModal = false;
  isEditingChapter = false;
  editingChapterId: number | null = null;
  chapterForm!: FormGroup;
  chapterModalLoading = false;
  chapterModalError = '';

  // Lesson Modal
  showLessonModal = false;
  isEditingLesson = false;
  editingLessonId: number | null = null;
  targetChapterId: number | null = null;
  lessonForm!: FormGroup;
  lessonModalLoading = false;
  lessonModalError = '';

  // Resource Upload
  uploadingFile = false;
  uploadError = '';
  uploadSuccess = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private contentService: ContentService,
    private courseService: CourseService,
    public authService: AuthService,
    private sanitizer: DomSanitizer,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.courseId = +id;
        this.loadData();
      } else {
        this.router.navigate(['/courses']);
      }
    });
  }

  initForms(): void {
    this.chapterForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(250)]],
      description: [''],
      orderIndex: [0, [Validators.required]]
    });

    this.lessonForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(250)]],
      description: [''],
      orderIndex: [0, [Validators.required]],
      videoUrl: [''],
      isPublished: [true]
    });
  }

  loadData(): void {
    this.loading = true;
    this.errorMessage = '';
    this.isForbidden = false;

    // Load course info first to check scope
    this.courseService.getCourseById(this.courseId).subscribe({
      next: (courseData) => {
        this.course = courseData;
        this.loadChapters();
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 403) {
          this.isForbidden = true;
          this.errorMessage = '403 - Bạn không có quyền truy cập khóa học này.';
        } else if (err.status === 404) {
          this.errorMessage = '404 - Không tìm thấy khóa học.';
        } else {
          this.errorMessage = 'Lỗi kết nối máy chủ khi tải khóa học.';
        }
      }
    });
  }

  loadChapters(): void {
    this.contentService.getCourseChapters(this.courseId).subscribe({
      next: (chaptersData) => {
        this.chapters = chaptersData;
        this.calculateProgress();
        this.loading = false;

        // Auto select first lesson if none selected
        if (!this.activeLessonId) {
          const firstLesson = this.findFirstLesson();
          if (firstLesson) {
            this.selectLesson(firstLesson.id);
          }
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Không thể tải danh sách chương & bài học.';
      }
    });
  }

  calculateProgress(): void {
    let total = 0;
    let completed = 0;
    for (const ch of this.chapters) {
      for (const l of ch.lessons) {
        total++;
        if (l.isCompleted) {
          completed++;
        }
      }
    }
    this.totalLessonsCount = total;
    this.completedLessonsCount = completed;
    this.progressPercentage = total > 0 ? Math.round((completed / total) * 100) : 0;
  }

  findFirstLesson(): LessonSummary | null {
    for (const ch of this.chapters) {
      if (ch.lessons && ch.lessons.length > 0) {
        return ch.lessons[0];
      }
    }
    return null;
  }

  selectLesson(lessonId: number): void {
    this.activeLessonId = lessonId;
    this.lessonLoading = true;
    this.uploadError = '';
    this.uploadSuccess = '';

    this.contentService.getLessonDetail(lessonId).subscribe({
      next: (detail) => {
        this.activeLesson = detail;
        this.safeVideoUrl = this.prepareVideoUrl(detail.videoUrl);
        this.lessonLoading = false;
      },
      error: (err) => {
        this.lessonLoading = false;
        this.activeLesson = null;
        this.safeVideoUrl = null;
      }
    });
  }

  prepareVideoUrl(url?: string): SafeResourceUrl | null {
    if (!url) return null;

    let embedUrl = url;
    // YouTube watch URL pattern
    if (url.includes('youtube.com/watch?v=')) {
      const videoId = url.split('v=')[1]?.split('&')[0];
      if (videoId) {
        embedUrl = `https://www.youtube.com/embed/${videoId}`;
      }
    } else if (url.includes('youtu.be/')) {
      const videoId = url.split('youtu.be/')[1]?.split('?')[0];
      if (videoId) {
        embedUrl = `https://www.youtube.com/embed/${videoId}`;
      }
    }

    return this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl);
  }

  completeLesson(): void {
    if (!this.activeLesson || this.completing) return;

    this.completing = true;
    this.contentService.completeLesson(this.activeLesson.id).subscribe({
      next: (progress) => {
        this.completing = false;
        if (this.activeLesson) {
          this.activeLesson.isCompleted = true;
          this.activeLesson.completedAt = progress.completedAt;
        }

        // Update in sidebar list
        for (const ch of this.chapters) {
          const found = ch.lessons.find((l) => l.id === this.activeLessonId);
          if (found) {
            found.isCompleted = true;
            found.completedAt = progress.completedAt;
            break;
          }
        }
        this.calculateProgress();
      },
      error: () => {
        this.completing = false;
      }
    });
  }

  downloadResource(resource: Resource): void {
    this.contentService.downloadResource(resource.id).subscribe({
      next: (blob) => {
        const downloadUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = resource.fileName;
        link.click();
        window.URL.revokeObjectURL(downloadUrl);
      },
      error: () => {
        alert('Không thể tải xuống tệp tin. Vui lòng kiểm tra quyền truy cập.');
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file || !this.activeLesson) return;

    this.uploadingFile = true;
    this.uploadError = '';
    this.uploadSuccess = '';

    this.contentService.uploadResource(this.activeLesson.id, file).subscribe({
      next: (newResource) => {
        this.uploadingFile = false;
        this.uploadSuccess = `Đã tải lên tệp: ${newResource.fileName}`;
        this.activeLesson?.resources.push(newResource);

        // Update resource count in sidebar
        for (const ch of this.chapters) {
          const l = ch.lessons.find((x) => x.id === this.activeLessonId);
          if (l) {
            l.resourceCount++;
            break;
          }
        }
        event.target.value = '';
      },
      error: (err) => {
        this.uploadingFile = false;
        this.uploadError = err.error?.message || 'Lỗi khi tải tệp lên.';
        event.target.value = '';
      }
    });
  }

  deleteResource(resourceId: number): void {
    if (!confirm('Bạn có chắc chắn muốn xóa tài liệu này?')) return;

    this.contentService.deleteResource(resourceId).subscribe({
      next: () => {
        if (this.activeLesson) {
          this.activeLesson.resources = this.activeLesson.resources.filter((r) => r.id !== resourceId);
        }
        for (const ch of this.chapters) {
          const l = ch.lessons.find((x) => x.id === this.activeLessonId);
          if (l && l.resourceCount > 0) {
            l.resourceCount--;
            break;
          }
        }
      },
      error: (err) => {
        alert(err.error?.message || 'Không thể xóa tài liệu.');
      }
    });
  }

  // Navigation
  goToNextLesson(): void {
    const all = this.getAllLessonsList();
    const idx = all.findIndex((l) => l.id === this.activeLessonId);
    if (idx !== -1 && idx < all.length - 1) {
      this.selectLesson(all[idx + 1].id);
    }
  }

  goToPrevLesson(): void {
    const all = this.getAllLessonsList();
    const idx = all.findIndex((l) => l.id === this.activeLessonId);
    if (idx > 0) {
      this.selectLesson(all[idx - 1].id);
    }
  }

  hasNextLesson(): boolean {
    const all = this.getAllLessonsList();
    const idx = all.findIndex((l) => l.id === this.activeLessonId);
    return idx !== -1 && idx < all.length - 1;
  }

  hasPrevLesson(): boolean {
    const all = this.getAllLessonsList();
    const idx = all.findIndex((l) => l.id === this.activeLessonId);
    return idx > 0;
  }

  private getAllLessonsList(): LessonSummary[] {
    const list: LessonSummary[] = [];
    for (const ch of this.chapters) {
      list.push(...ch.lessons);
    }
    return list;
  }

  // Chapter Modal
  openCreateChapterModal(): void {
    this.isEditingChapter = false;
    this.editingChapterId = null;
    this.chapterModalError = '';
    const nextOrder = this.chapters.length + 1;
    this.chapterForm.reset({
      title: '',
      description: '',
      orderIndex: nextOrder
    });
    this.showChapterModal = true;
  }

  openEditChapterModal(chapter: Chapter, event: Event): void {
    event.stopPropagation();
    this.isEditingChapter = true;
    this.editingChapterId = chapter.id;
    this.chapterModalError = '';
    this.chapterForm.patchValue({
      title: chapter.title,
      description: chapter.description || '',
      orderIndex: chapter.orderIndex
    });
    this.showChapterModal = true;
  }

  saveChapter(): void {
    if (this.chapterForm.invalid) {
      this.chapterForm.markAllAsTouched();
      return;
    }

    this.chapterModalLoading = true;
    this.chapterModalError = '';

    if (this.isEditingChapter && this.editingChapterId) {
      this.contentService.updateChapter(this.editingChapterId, this.chapterForm.value).subscribe({
        next: () => {
          this.chapterModalLoading = false;
          this.showChapterModal = false;
          this.loadChapters();
        },
        error: (err) => {
          this.chapterModalLoading = false;
          this.chapterModalError = err.error?.message || 'Lỗi khi cập nhật chương.';
        }
      });
    } else {
      this.contentService.createChapter(this.courseId, this.chapterForm.value).subscribe({
        next: () => {
          this.chapterModalLoading = false;
          this.showChapterModal = false;
          this.loadChapters();
        },
        error: (err) => {
          this.chapterModalLoading = false;
          this.chapterModalError = err.error?.message || 'Lỗi khi tạo chương mới.';
        }
      });
    }
  }

  deleteChapter(chapter: Chapter, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Bạn có chắc chắn muốn xóa chương "${chapter.title}" và toàn bộ bài học bên trong?`)) {
      return;
    }

    this.contentService.deleteChapter(chapter.id).subscribe({
      next: () => {
        this.loadChapters();
      },
      error: (err) => {
        alert(err.error?.message || 'Lỗi khi xóa chương.');
      }
    });
  }

  // Lesson Modal
  openCreateLessonModal(chapterId: number, event: Event): void {
    event.stopPropagation();
    this.targetChapterId = chapterId;
    this.isEditingLesson = false;
    this.editingLessonId = null;
    this.lessonModalError = '';

    const ch = this.chapters.find((c) => c.id === chapterId);
    const nextOrder = (ch?.lessons.length || 0) + 1;

    this.lessonForm.reset({
      title: '',
      description: '',
      orderIndex: nextOrder,
      videoUrl: '',
      isPublished: true
    });
    this.showLessonModal = true;
  }

  openEditLessonModal(lesson: LessonSummary, event: Event): void {
    event.stopPropagation();
    this.isEditingLesson = true;
    this.editingLessonId = lesson.id;
    this.lessonModalError = '';

    // If active lesson matches, pull detail; otherwise fetch detail
    if (this.activeLesson && this.activeLesson.id === lesson.id) {
      this.lessonForm.patchValue({
        title: this.activeLesson.title,
        description: this.activeLesson.description || '',
        orderIndex: this.activeLesson.orderIndex,
        videoUrl: this.activeLesson.videoUrl || '',
        isPublished: this.activeLesson.isPublished
      });
      this.showLessonModal = true;
    } else {
      this.contentService.getLessonDetail(lesson.id).subscribe({
        next: (detail) => {
          this.lessonForm.patchValue({
            title: detail.title,
            description: detail.description || '',
            orderIndex: detail.orderIndex,
            videoUrl: detail.videoUrl || '',
            isPublished: detail.isPublished
          });
          this.showLessonModal = true;
        }
      });
    }
  }

  saveLesson(): void {
    if (this.lessonForm.invalid) {
      this.lessonForm.markAllAsTouched();
      return;
    }

    this.lessonModalLoading = true;
    this.lessonModalError = '';

    if (this.isEditingLesson && this.editingLessonId) {
      this.contentService.updateLesson(this.editingLessonId, this.lessonForm.value).subscribe({
        next: (updated) => {
          this.lessonModalLoading = false;
          this.showLessonModal = false;
          this.loadChapters();
          if (this.editingLessonId && this.activeLessonId === this.editingLessonId) {
            this.selectLesson(this.editingLessonId);
          }
        },
        error: (err) => {
          this.lessonModalLoading = false;
          this.lessonModalError = err.error?.message || 'Lỗi khi cập nhật bài học.';
        }
      });
    } else if (this.targetChapterId) {
      this.contentService.createLesson(this.targetChapterId, this.lessonForm.value).subscribe({
        next: (created) => {
          this.lessonModalLoading = false;
          this.showLessonModal = false;
          this.loadChapters();
          this.selectLesson(created.id);
        },
        error: (err) => {
          this.lessonModalLoading = false;
          this.lessonModalError = err.error?.message || 'Lỗi khi tạo bài học mới.';
        }
      });
    }
  }

  deleteLesson(lesson: LessonSummary, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Bạn có chắc chắn muốn xóa bài học "${lesson.title}"?`)) return;

    this.contentService.deleteLesson(lesson.id).subscribe({
      next: () => {
        if (this.activeLessonId === lesson.id) {
          this.activeLessonId = null;
          this.activeLesson = null;
        }
        this.loadChapters();
      },
      error: (err) => {
        alert(err.error?.message || 'Lỗi khi xóa bài học.');
      }
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  canManage(): boolean {
    if (this.authService.isAdmin) return true;
    if (this.authService.isTeacher && this.course && this.course.teacherId === this.authService.currentUserValue?.userId) {
      return true;
    }
    return false;
  }
}
