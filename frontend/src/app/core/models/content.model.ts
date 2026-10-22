export interface Resource {
  id: number;
  lessonId: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

export interface LessonSummary {
  id: number;
  chapterId: number;
  title: string;
  orderIndex: number;
  isPublished: boolean;
  isCompleted: boolean;
  completedAt?: string;
  resourceCount: number;
}

export interface LessonDetail {
  id: number;
  chapterId: number;
  chapterTitle: string;
  courseId: number;
  courseTitle: string;
  title: string;
  description?: string;
  orderIndex: number;
  videoUrl?: string;
  isPublished: boolean;
  createdAt: string;
  isCompleted: boolean;
  completedAt?: string;
  resources: Resource[];
}

export interface Chapter {
  id: number;
  courseId: number;
  title: string;
  description?: string;
  orderIndex: number;
  createdAt: string;
  lessons: LessonSummary[];
}

export interface CreateChapterRequest {
  title: string;
  description?: string;
  orderIndex?: number;
}

export interface UpdateChapterRequest {
  title: string;
  description?: string;
  orderIndex?: number;
}

export interface CreateLessonRequest {
  title: string;
  description?: string;
  orderIndex?: number;
  videoUrl?: string;
  isPublished?: boolean;
}

export interface UpdateLessonRequest {
  title: string;
  description?: string;
  orderIndex?: number;
  videoUrl?: string;
  isPublished?: boolean;
}

export interface LessonProgress {
  lessonId: number;
  studentId: number;
  isCompleted: boolean;
  completedAt?: string;
}
