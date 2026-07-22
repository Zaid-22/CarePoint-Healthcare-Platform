export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: 'Patient' | 'Doctor';
  specialtyIds?: string[];
  consultationFee?: number;
  phoneNumber?: string;
  gender?: string;
  bio?: string;
  profilePictureUrl?: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: string;
  roles?: string[];
  accessToken: string;
  refreshToken: string;
}

export interface AuthUser {
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: string;
  roles: string[];
}
