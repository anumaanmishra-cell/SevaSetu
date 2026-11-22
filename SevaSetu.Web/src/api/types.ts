export type Role = "Family" | "Caregiver" | "AgencyAdmin" | "HospitalCoordinator" | "PlatformAdmin" | "Elder";

export const ROLES: Role[] = ["Family", "Caregiver", "AgencyAdmin", "HospitalCoordinator", "PlatformAdmin", "Elder"];

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  role: Role;
  fullName: string;
  email: string;
}

export interface ElderDto {
  id: number;
  fullName: string;
  age: number;
  address: string;
  city: string;
  conditions?: string | null;
  consentHealth: boolean;
}

export interface DirectoryCaregiverDto {
  id: number;
  fullName: string;
  city: string;
  kind: number;
  languages: string;
  hourlyRate: number;
  liveInDayRate: number;
  liveInOk: boolean;
  rating?: number | null;
}

export interface DirectoryAgencyDto {
  id: number;
  name: string;
  city: string;
  hourlyRate: number;
  liveInDayRate: number;
}

export interface CareRequestDto {
  id: number;
  elderId: number;
  elderName: string;
  caregiverId?: number | null;
  agencyId?: number | null;
  mode: number;
  startAt: string;
  endAt: string;
  notes?: string | null;
  status: number;
  instantBook: boolean;
}

export interface EpisodeDto {
  id: number;
  elderId: number;
  elderName: string;
  agencyId?: number | null;
  planTemplate: string;
  planNotes?: string | null;
  active: boolean;
  startDate: string;
}

export interface ShiftDto {
  id: number;
  episodeId: number;
  caregiverId: number;
  caregiverName: string;
  slotStart: string;
  slotEnd: string;
  status: number;
  replacementRequestedAt?: string | null;
}

export interface CareLogDto {
  id: number;
  logDate: string;
  comfortScore: number;
  meals?: string | null;
  mobility?: string | null;
  note?: string | null;
}

export interface VitalDto {
  id: number;
  logDate: string;
  systolic?: number | null;
  diastolic?: number | null;
  pulse?: number | null;
  tempC?: number | null;
  sugar?: number | null;
}

export interface AdlDto {
  id: number;
  at: string;
  activity: string;
}

export interface MarDto {
  id: number;
  drug: string;
  dose: string;
  scheduledAt: string;
  status: number;
  witnessName?: string | null;
}

export interface IncidentDto {
  id: number;
  kind: number;
  note: string;
  at: string;
}

export interface InvoiceDto {
  id: number;
  number: string;
  total: number;
  tax: number;
  paymentStatus: number;
  paymentMode?: number | null;
  gstEnabledSnapshot: boolean;
  issuedAt: string;
  isCommission: boolean;
}

export const requestStatus = ["Open", "Accepted", "Declined", "Cancelled", "Booked"];
export const caregiverKind = ["Nurse", "Attendant", "Ayah", "Physio"];
export const visitStatus = ["Scheduled", "Checked in", "Checked out", "No-show", "Cancelled"];
