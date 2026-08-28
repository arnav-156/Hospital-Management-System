namespace Hospital.Application.Rules;

public static class AppointmentWorkflowRules
{
    public static bool IsBookableSlot(DateTime slot, DateTime utcNow) =>
        slot > utcNow && slot.Minute % 30 == 0 && slot.Hour >= 9 && slot.Hour < 17;

    public static bool CanReview(string? status) => status == "Pending";

    public static bool CanRecordTreatment(string? status) => status == "Accepted";

    public static bool CanGenerateBill(string? status) => status == "Completed";

    public static bool CanCancel(string? status) => status is "Pending" or "Accepted";
}
