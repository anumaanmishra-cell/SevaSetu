namespace SevaSetu.Domain;

public static class AppRoles
{
    public const string Family = "Family";
    public const string Caregiver = "Caregiver";
    public const string AgencyAdmin = "AgencyAdmin";
    public const string HospitalCoordinator = "HospitalCoordinator";
    public const string PlatformAdmin = "PlatformAdmin";
    public const string Elder = "Elder";
}

public enum KycStatus { Pending = 0, Passed = 1, Failed = 2 }
public enum CaregiverKind { Nurse = 0, Attendant = 1, Ayah = 2, Physio = 3 }
public enum CareMode { Hourly = 0, TwelveHour = 1, LiveIn = 2 }
public enum RequestStatus { Open = 0, Accepted = 1, Declined = 2, Cancelled = 3, Booked = 4 }
public enum VisitStatus { Scheduled = 0, CheckedIn = 1, CheckedOut = 2, NoShow = 3, Cancelled = 4 }
public enum PaymentStatus { Unpaid = 0, Paid = 1 }
public enum PaymentMode { Cash = 0, Upi = 1 }
public enum MarStatus { Given = 0, Refused = 1, Held = 2 }
public enum IncidentKind { Fall = 0, Hospitalisation = 1, Other = 2 }
public enum ShiftStatus { Planned = 0, Replaced = 1, Cancelled = 2 }
