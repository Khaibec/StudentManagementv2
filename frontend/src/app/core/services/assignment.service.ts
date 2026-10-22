import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Assignment,
  AssignmentDetail,
  CreateAssignmentRequest,
  UpdateAssignmentRequest,
  Submission,
  GradeSubmissionRequest
} from '../models/assignment.model';

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Assignments
  getCourseAssignments(courseId: number): Observable<Assignment[]> {
    return this.http.get<Assignment[]>(`${this.apiUrl}/courses/${courseId}/assignments`);
  }

  getAssignmentById(id: number): Observable<AssignmentDetail> {
    return this.http.get<AssignmentDetail>(`${this.apiUrl}/assignments/${id}`);
  }

  createAssignment(courseId: number, data: CreateAssignmentRequest): Observable<Assignment> {
    return this.http.post<Assignment>(`${this.apiUrl}/courses/${courseId}/assignments`, data);
  }

  updateAssignment(id: number, data: UpdateAssignmentRequest): Observable<Assignment> {
    return this.http.put<Assignment>(`${this.apiUrl}/assignments/${id}`, data);
  }

  deleteAssignment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/assignments/${id}`);
  }

  // Submissions
  submitAssignment(id: number, textContent: string, file: File | null): Observable<Submission> {
    const formData = new FormData();
    if (textContent) {
      formData.append('textContent', textContent);
    }
    if (file) {
      formData.append('file', file, file.name);
    }
    return this.http.post<Submission>(`${this.apiUrl}/assignments/${id}/submissions`, formData);
  }

  getMySubmission(id: number): Observable<Submission> {
    return this.http.get<Submission>(`${this.apiUrl}/assignments/${id}/submissions/me`);
  }

  getAssignmentSubmissions(id: number): Observable<Submission[]> {
    return this.http.get<Submission[]>(`${this.apiUrl}/assignments/${id}/submissions`);
  }

  gradeSubmission(submissionId: number, data: GradeSubmissionRequest): Observable<Submission> {
    return this.http.put<Submission>(`${this.apiUrl}/submissions/${submissionId}/grade`, data);
  }

  downloadSubmissionFile(submissionId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/submissions/${submissionId}/download`, {
      responseType: 'blob'
    });
  }
}
