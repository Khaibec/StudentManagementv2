export interface Course {
  id: number;
  code: string;
  title: string;
  description?: string;
  teacherId: number;
  teacherName: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
  isEnrolled: boolean;
  enrollmentCount: number;
}

export interface CreateCourseRequest {
  code: string;
  title: string;
  description?: string;
  teacherId?: number;
}

export interface UpdateCourseRequest {
  title: string;
  description?: string;
  status: string;
}
