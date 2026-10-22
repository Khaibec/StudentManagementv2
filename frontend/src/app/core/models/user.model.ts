export type UserRole = 'STUDENT' | 'TEACHER' | 'ADMIN';

export interface User {
  userId: number;
  username: string;
  fullName: string;
  email?: string;
  role: UserRole;
  token?: string;
  expiresAt?: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: number;
  username: string;
  fullName: string;
  role: UserRole;
  expiresAt: string;
}
