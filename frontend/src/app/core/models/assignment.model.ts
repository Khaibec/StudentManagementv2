export interface Assignment {
  id: number;
  courseId: number;
  title: string;
  description?: string;
  dueDate: string;
  maxScore: number;
  isActive: boolean;
  createdAt: string;
  isOverdue: boolean;

  // Student specific
  hasSubmitted?: boolean;
  submissionStatus?: string; // NOT_SUBMITTED, SUBMITTED_ON_TIME, SUBMITTED_LATE
  score?: number | null;
  isGraded?: boolean;

  // Teacher specific
  submissionCount?: number;
  gradedCount?: number;
}

export interface AssignmentDetail {
  id: number;
  courseId: number;
  courseTitle: string;
  title: string;
  description?: string;
  dueDate: string;
  maxScore: number;
  isActive: boolean;
  createdAt: string;
  isOverdue: boolean;
  mySubmission?: Submission;
}

export interface CreateAssignmentRequest {
  title: string;
  description?: string;
  dueDate: string;
  maxScore: number;
  isActive?: boolean;
}

export interface UpdateAssignmentRequest {
  title: string;
  description?: string;
  dueDate: string;
  maxScore: number;
  isActive?: boolean;
}

export interface Submission {
  id: number;
  assignmentId: number;
  assignmentTitle?: string;
  studentId: number;
  studentName?: string;
  studentUsername?: string;
  textContent?: string;
  fileName?: string;
  hasFile?: boolean;
  submittedAt: string;
  status: string; // SUBMITTED_ON_TIME, SUBMITTED_LATE
  score?: number | null;
  feedback?: string;
  gradedAt?: string;
  gradedByName?: string;
  isGraded?: boolean;
}

export interface GradeSubmissionRequest {
  score: number;
  feedback?: string;
}
