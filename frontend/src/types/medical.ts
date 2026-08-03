export interface MedicalRecordDto {
  id: string;
  appointmentId: string;
  doctorName?: string;
  patientName?: string;
  appointmentDate?: string;
  diagnosis: string;
  treatment?: string;
  notes?: string;
  createdAt: string;
}

export interface PrescriptionDto {
  id: string;
  appointmentId: string;
  doctorProfileId: string;
  doctorName: string;
  patientProfileId: string;
  patientName: string;
  notes?: string;
  items: PrescriptionItemDto[];
  createdAt: string;
}

export interface PrescriptionItemDto {
  id: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  duration?: string;
  instructions?: string;
}

export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  type: number;
  referenceId?: string;
  createdAt: string;
}

export interface MedicalDocumentDto {
  id: string;
  patientProfileId: string;
  appointmentId?: string;
  fileName: string;
  downloadUrl: string;
  contentType: string;
  documentType?: string;
  fileSizeBytes: number;
  createdAt: string;
}
