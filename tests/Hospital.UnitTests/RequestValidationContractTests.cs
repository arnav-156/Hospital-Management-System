using System.ComponentModel.DataAnnotations;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Billing;
using Hospital.Application.DTOs.Feedback;
using Hospital.Application.Rules;

namespace Hospital.UnitTests;

public sealed class RequestValidationContractTests
{
    [Fact]
    public void AppointmentRequestRequiresDoctorAndDepartment()
    {
        var results = Validate(new CreateAppointmentRequest());

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateAppointmentRequest.DoctorId)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateAppointmentRequest.DepartmentId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(10_000_000_000d)]
    public void BillRequestRejectsAmountsOutsideSupportedRange(double amount)
    {
        var results = Validate(new CreateBillRequest { Amount = (decimal)amount });

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateBillRequest.Amount)));
    }

    [Fact]
    public void BillRequestAcceptsPositiveAmountWithinSupportedRange()
    {
        var results = Validate(new CreateBillRequest { Amount = 1200m, Description = "Consultation" });

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("UPI", true)]
    [InlineData("Cash", true)]
    [InlineData("WireTransfer", false)]
    public void PaymentRequestRestrictsMethodsToTheSupportedLedgerValues(string paymentMethod, bool expected)
    {
        var results = Validate(new RecordPaymentRequest { PaymentMethod = paymentMethod });

        Assert.Equal(expected, results.Count == 0);
    }

    [Theory]
    [InlineData("Duplicate charge", true)]
    [InlineData("", false)]
    public void VoidBillRequestRequiresAnExplanation(string reason, bool expected)
    {
        var results = Validate(new VoidBillRequest { Reason = reason });

        Assert.Equal(expected, results.Count == 0);
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)6)]
    public void FeedbackRequestRestrictsRatingToOneThroughFive(byte rating)
    {
        var results = Validate(new CreateFeedbackRequest { AppointmentId = 1, Rating = rating });

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateFeedbackRequest.Rating)));
    }

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(16, 30, true)]
    [InlineData(8, 30, false)]
    [InlineData(17, 0, false)]
    [InlineData(10, 15, false)]
    public void BookingRuleAllowsOnlyFutureHalfHourSlotsDuringClinicHours(int hour, int minute, bool expected)
    {
        var now = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var slot = new DateTime(2030, 1, 2, hour, minute, 0, DateTimeKind.Utc);

        Assert.Equal(expected, AppointmentWorkflowRules.IsBookableSlot(slot, now));
    }

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Accepted", false)]
    [InlineData("Rejected", false)]
    [InlineData("Completed", false)]
    public void OnlyPendingAppointmentsCanBeReviewed(string status, bool expected)
    {
        Assert.Equal(expected, AppointmentWorkflowRules.CanReview(status));
    }

    [Theory]
    [InlineData("Accepted", true)]
    [InlineData("Pending", false)]
    [InlineData("Completed", false)]
    public void TreatmentAndBillingRequireTheirExpectedLifecycleStates(string status, bool canRecordTreatment)
    {
        Assert.Equal(canRecordTreatment, AppointmentWorkflowRules.CanRecordTreatment(status));
        Assert.Equal(status == "Completed", AppointmentWorkflowRules.CanGenerateBill(status));
    }

    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
