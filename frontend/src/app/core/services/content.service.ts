import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Chapter,
  CreateChapterRequest,
  UpdateChapterRequest,
  LessonDetail,
  CreateLessonRequest,
  UpdateLessonRequest,
  Resource,
  LessonProgress
} from '../models/content.model';

@Injectable({
  providedIn: 'root'
})
export class ContentService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Chapters
  getCourseChapters(courseId: number): Observable<Chapter[]> {
    return this.http.get<Chapter[]>(`${this.apiUrl}/courses/${courseId}/chapters`);
  }

  createChapter(courseId: number, data: CreateChapterRequest): Observable<Chapter> {
    return this.http.post<Chapter>(`${this.apiUrl}/courses/${courseId}/chapters`, data);
  }

  updateChapter(chapterId: number, data: UpdateChapterRequest): Observable<Chapter> {
    return this.http.put<Chapter>(`${this.apiUrl}/chapters/${chapterId}`, data);
  }

  deleteChapter(chapterId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/chapters/${chapterId}`);
  }

  // Lessons
  createLesson(chapterId: number, data: CreateLessonRequest): Observable<LessonDetail> {
    return this.http.post<LessonDetail>(`${this.apiUrl}/chapters/${chapterId}/lessons`, data);
  }

  getLessonDetail(lessonId: number): Observable<LessonDetail> {
    return this.http.get<LessonDetail>(`${this.apiUrl}/lessons/${lessonId}`);
  }

  updateLesson(lessonId: number, data: UpdateLessonRequest): Observable<LessonDetail> {
    return this.http.put<LessonDetail>(`${this.apiUrl}/lessons/${lessonId}`, data);
  }

  deleteLesson(lessonId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/lessons/${lessonId}`);
  }

  // Progress
  completeLesson(lessonId: number): Observable<LessonProgress> {
    return this.http.post<LessonProgress>(`${this.apiUrl}/lessons/${lessonId}/complete`, {});
  }

  // Resources
  uploadResource(lessonId: number, file: File): Observable<Resource> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<Resource>(`${this.apiUrl}/lessons/${lessonId}/resources`, formData);
  }

  downloadResource(resourceId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/resources/${resourceId}/download`, {
      responseType: 'blob'
    });
  }

  deleteResource(resourceId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/resources/${resourceId}`);
  }
}
