export interface SpecialtyDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  doctorCount?: number;
}

export interface ClinicDto {
  id: string;
  name: string;
  address: string;
  phoneNumber: string;
  city: string;
  isActive: boolean;
}

export interface DoctorDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  bio: string;
  consultationFee: number;
  phoneNumber: string;
  gender: string;
  profilePictureUrl?: string;
  approvalStatus: number;
  specialties: SpecialtyDto[];
  clinics: ClinicDto[];
}

export interface DoctorAdminSummaryDto {
  totalRegistered: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
}

export interface UpdateDoctorRequest {
  bio?: string;
  consultationFee?: number;
  phoneNumber?: string;
  gender?: string;
  profilePictureUrl?: string;
  specialtyIds: string[];
}

export interface AvailableSlotDto {
  date: string;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface DoctorAvailabilityDto {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}
