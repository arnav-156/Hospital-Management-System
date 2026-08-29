namespace Hospital.Application.DTOs.Dashboard;

public sealed record DashboardSummaryDto(
    int UpcomingAppointments,
    int UnreadNotifications,
    decimal OutstandingBills,
    int PendingReviews,
    int PatientsThisMonth,
    int ActiveStaffAccounts,
    int ActiveDoctors,
    int PatientRecords);
