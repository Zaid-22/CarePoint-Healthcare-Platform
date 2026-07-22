export interface AppointmentDto {
  id: string;
  patientProfileId: string;
  patientName: string;
  doctorProfileId: string;
  doctorName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: number;
  notes?: string;
  cancellationReason?: string;
  createdAt: string;
}

export interface CreateAppointmentRequest {
  doctorProfileId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  notes?: string;
}
