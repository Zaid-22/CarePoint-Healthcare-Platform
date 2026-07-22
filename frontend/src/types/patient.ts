export interface PatientDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth?: string;
  gender?: string;
  bloodType?: string;
  address?: string;
  emergencyContact?: string;
}

export interface UpdatePatientRequest {
  dateOfBirth?: string | null;
  bloodType?: string;
  phoneNumber?: string;
  address?: string;
  gender?: string;
  emergencyContact?: string;
}

export type UpdatePatientDto = UpdatePatientRequest;
