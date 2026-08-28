using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Hospital.Application.DTOs;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Ai;
using Hospital.Application.DTOs.Treatments;
using Hospital.Application.DTOs.Billing;
using Hospital.Application.DTOs.Notifications;
using Hospital.Application.DTOs.Feedback;
using Hospital.Application.DTOs.Profiles;
using Hospital.Application.Security;
using Hospital.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Hospital.ApiTests;

public sealed class AuthenticationEndpointTests
{
    [Fact]
    public async Task HealthEndpointReturnsHealthyStatusWhenDatabaseIsAvailable()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var health = await response.Content.ReadFromJsonAsync<HealthStatusDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(health);
        Assert.Equal("healthy", health.Status);
        Assert.True(health.DatabaseConnected);
    }

    [Fact]
    public async Task ApiResponsesUseSecurityHeadersAndOnlyAllowConfiguredDevelopmentOrigins()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        allowedRequest.Headers.Add("Origin", "http://127.0.0.1:5173");

        var allowedResponse = await client.SendAsync(allowedRequest);

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Equal("nosniff", Assert.Single(allowedResponse.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(allowedResponse.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(allowedResponse.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("no-store", string.Join(",", allowedResponse.Headers.GetValues("Cache-Control")), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("http://127.0.0.1:5173", Assert.Single(allowedResponse.Headers.GetValues("Access-Control-Allow-Origin")));

        using var blockedRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        blockedRequest.Headers.Add("Origin", "https://untrusted.example");
        var blockedResponse = await client.SendAsync(blockedRequest);

        Assert.False(blockedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task ListEndpointsApplyBoundedPagination()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "admin@hospital.example", Password = "DevelopmentOnly!123" });
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var firstPage = await client.GetFromJsonAsync<List<DoctorProfileDto>>("/api/admin/doctors?page=1&pageSize=1");
        var oversizedPage = await client.GetAsync("/api/admin/doctors?page=1&pageSize=101");
        var invalidPage = await client.GetAsync("/api/admin/doctors?page=0&pageSize=1");

        Assert.NotNull(firstPage);
        Assert.True(firstPage.Count <= 1);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedPage.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
    }

    [Fact]
    public async Task RegistrationLoginAndAuthenticatedSessionWorkWithHashedPassword()
    {
        var email = NewTestEmail();
        const string password = "RegistrationTestPassword!1";
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        try
        {
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password });
            var registration = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
            Assert.NotNull(registration);
            Assert.Equal(email, registration.User.Email);
            Assert.Equal(UserRoles.Patient, registration.User.Role);
            Assert.False(string.IsNullOrWhiteSpace(registration.AccessToken));

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementDbContext>();
                var user = await dbContext.Users.SingleAsync(candidate => candidate.Email == email);
                Assert.NotEqual(password, user.PasswordHash);
                Assert.StartsWith("AQAAAA", user.PasswordHash, StringComparison.Ordinal);
            }

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email.ToUpperInvariant(), Password = password });
            var login = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            Assert.NotNull(login);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            var meResponse = await client.GetAsync("/api/auth/me");
            var me = await meResponse.Content.ReadFromJsonAsync<AuthenticatedUserDto>();

            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
            Assert.NotNull(me);
            Assert.Equal(email, me.Email);
            Assert.Equal(UserRoles.Patient, me.Role);
        }
        finally
        {
            await DeleteUserAsync(factory, email);
        }
    }

    [Fact]
    public async Task InvalidPasswordAndUnknownUserAreRejected()
    {
        var email = NewTestEmail();
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        try
        {
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "RegistrationTestPassword!1" });
            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

            var invalidPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "WrongPassword!123" });
            var unknownUser = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = NewTestEmail(), Password = "RegistrationTestPassword!1" });
            var duplicateRegistration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "RegistrationTestPassword!1" });

            Assert.Equal(HttpStatusCode.Unauthorized, invalidPassword.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, duplicateRegistration.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(factory, email);
        }
    }

    [Fact]
    public async Task MissingAndExpiredTokensAreRejected()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(UserRoles.Patient, DateTime.UtcNow.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task SeedAdministratorCanAccessAdministratorResource()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@hospital.example",
            Password = "DevelopmentOnly!123",
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(login);
        Assert.Equal(UserRoles.Administrator, login.User.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/auth/admin")).StatusCode);
    }

    [Fact]
    public async Task PatientCanCreateAndViewOnlyTheirOwnProfile()
    {
        var email = NewTestEmail();
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        try
        {
            var registrationResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "ProfileTestPassword!1" });
            var registration = await registrationResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(registration);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.AccessToken);

            var updateResponse = await client.PutAsJsonAsync("/api/profile/me", new UpdatePatientProfileRequest
            {
                FirstName = "Test",
                LastName = "Patient",
                DateOfBirth = new DateOnly(1995, 5, 20),
                Gender = "Undisclosed",
                PhoneNumber = "555-0199",
            });
            var updated = await updateResponse.Content.ReadFromJsonAsync<PatientProfileDto>();
            var currentResponse = await client.GetAsync("/api/profile/me");
            var current = await currentResponse.Content.ReadFromJsonAsync<PatientProfileDto>();

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.NotNull(updated);
            Assert.StartsWith("PAT-", updated.MedicalRecordNumber, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
            Assert.NotNull(current);
            Assert.Equal(updated.PatientId, current.PatientId);
            Assert.Equal(email, current.Email);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/profile/me", new UpdatePatientProfileRequest { FirstName = "Test", LastName = "Patient", DateOfBirth = new DateOnly(1995, 5, 20), Gender = "Not a valid option" })).StatusCode);
        }
        finally
        {
            await DeleteUserAsync(factory, email);
        }
    }

    [Fact]
    public async Task AdministratorCanSearchProfilesAndDeactivateAnAccount()
    {
        var email = NewTestEmail();
        using var factory = new TestWebApplicationFactory();
        using var patientClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();

        try
        {
            var registrationResponse = await patientClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "ProfileTestPassword!1" });
            var registration = await registrationResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(registration);

            var adminLogin = await adminClient.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "admin@hospital.example", Password = "DevelopmentOnly!123" });
            var admin = await adminLogin.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(admin);
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

            Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/api/admin/doctors?search=Ada")).StatusCode);
            var staffResponse = await adminClient.GetAsync("/api/admin/staff?search=Jamie");
            var staff = await staffResponse.Content.ReadFromJsonAsync<List<StaffProfileDto>>();
            Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
            Assert.NotNull(staff);
            Assert.Contains(staff, member => member.IsAccountActive);
            var statusResponse = await adminClient.PatchAsJsonAsync($"/api/admin/accounts/{registration.User.UserId}/status", new UpdateAccountStatusRequest { IsActive = false });
            var status = await statusResponse.Content.ReadFromJsonAsync<UserAccountDto>();
            var blockedLogin = await patientClient.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "ProfileTestPassword!1" });

            Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
            Assert.NotNull(status);
            Assert.False(status.IsActive);
            Assert.Equal(HttpStatusCode.Unauthorized, blockedLogin.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(factory, email);
        }
    }

    [Fact]
    public async Task DepartmentAndDoctorCatalogSupportsSelectionAndAdministratorChanges()
    {
        var departmentCode = $"P6{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        using var factory = new TestWebApplicationFactory();
        using var publicClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        int? createdDepartmentId = null;

        try
        {
            var departments = await publicClient.GetFromJsonAsync<List<DepartmentDto>>("/api/departments");
            var doctors = await publicClient.GetFromJsonAsync<List<DoctorSummaryDto>>("/api/departments/1/doctors");
            Assert.NotNull(departments);
            Assert.Contains(departments, department => department.DepartmentCode == "CARD");
            Assert.NotNull(doctors);
            Assert.Contains(doctors, doctor => doctor.Specialization == "Cardiology");

            var adminLogin = await adminClient.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "admin@hospital.example", Password = "DevelopmentOnly!123" });
            var admin = await adminLogin.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(admin);
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
            var createResponse = await adminClient.PostAsJsonAsync("/api/departments", new SaveDepartmentRequest { DepartmentCode = departmentCode, Name = "Temporary Department", Description = "Temporary integration-test department" });
            var created = await createResponse.Content.ReadFromJsonAsync<DepartmentDto>();
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(created);
            createdDepartmentId = created.DepartmentId;

            var updateResponse = await adminClient.PutAsJsonAsync($"/api/departments/{createdDepartmentId}", new SaveDepartmentRequest { DepartmentCode = departmentCode, Name = "Updated Temporary Department", IsActive = true });
            var updated = await updateResponse.Content.ReadFromJsonAsync<DepartmentDto>();
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.NotNull(updated);
            Assert.Equal("Updated Temporary Department", updated.Name);

            var patientToken = CreateToken(UserRoles.Patient, DateTime.UtcNow.AddMinutes(10));
            publicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await publicClient.PostAsJsonAsync("/api/departments", new SaveDepartmentRequest { DepartmentCode = "DENIED", Name = "Denied" })).StatusCode);
        }
        finally
        {
            if (createdDepartmentId.HasValue)
            {
                await DeleteDepartmentAsync(factory, createdDepartmentId.Value);
            }
        }
    }

    [Fact]
    public async Task InactiveDoctorAccountIsNotAvailableForPatientSelection()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        bool? originalAccountIsActive = null;

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementDbContext>();
                var doctor = await dbContext.Doctors.SingleAsync(candidate => candidate.DoctorId == 1);
                var user = await dbContext.Users.SingleAsync(candidate => candidate.UserId == doctor.UserId);
                originalAccountIsActive = user.IsActive;
                user.IsActive = false;
                await dbContext.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/doctors/1")).StatusCode);
            var doctors = await client.GetFromJsonAsync<List<DoctorSummaryDto>>("/api/departments/1/doctors");
            Assert.NotNull(doctors);
            Assert.DoesNotContain(doctors, doctor => doctor.DoctorId == 1);
        }
        finally
        {
            if (originalAccountIsActive.HasValue)
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementDbContext>();
                var doctor = await dbContext.Doctors.SingleAsync(candidate => candidate.DoctorId == 1);
                var user = await dbContext.Users.SingleAsync(candidate => candidate.UserId == doctor.UserId);
                user.IsActive = originalAccountIsActive.Value;
                await dbContext.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task PatientCanBookAndDoctorCanAcceptAppointment()
    {
        var email = NewTestEmail(); using var factory = new TestWebApplicationFactory(); using var patient = factory.CreateClient(); using var doctor = factory.CreateClient();
        try
        {
            var registration = await patient.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "AppointmentTestPassword!1" }); var auth = await registration.Content.ReadFromJsonAsync<AuthenticationResponse>(); Assert.NotNull(auth); patient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var profileResponse = await patient.PutAsJsonAsync("/api/profile/me", new UpdatePatientProfileRequest { FirstName = "Test", LastName = "Patient", DateOfBirth = new DateOnly(1990, 1, 1) }); var profile = await profileResponse.Content.ReadFromJsonAsync<PatientProfileDto>(); Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode); Assert.NotNull(profile);
            var slot = DateTime.UtcNow.Date.AddDays(2).AddHours(10);
            var createdResponse = await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, DepartmentId = 1, AppointmentDateTime = slot, Reason = "Test consultation" }); var created = await createdResponse.Content.ReadFromJsonAsync<AppointmentDto>(); Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode); Assert.NotNull(created); Assert.Equal("Pending", created.Status);
            var appointmentSummary = Assert.Single((await patient.GetFromJsonAsync<List<PatientAppointmentSummaryDto>>("/api/appointments/my/summaries?pageSize=100") ?? []).Where(summary => summary.AppointmentId == created.AppointmentId));
            Assert.Equal("Ada Khan", appointmentSummary.DoctorName);
            Assert.Equal("Cardiology", appointmentSummary.DepartmentName);
            var doctorLogin = await doctor.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "dr.ada@hospital.example", Password = "DevelopmentOnly!123" }); var doctorAuth = await doctorLogin.Content.ReadFromJsonAsync<AuthenticationResponse>(); Assert.NotNull(doctorAuth); doctor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorAuth.AccessToken);
            Assert.Contains(await doctor.GetFromJsonAsync<List<AppointmentDto>>("/api/doctor/appointments/pending") ?? [], appointment => appointment.AppointmentId == created.AppointmentId);
            var workItem = Assert.Single((await doctor.GetFromJsonAsync<List<DoctorAppointmentWorkItemDto>>("/api/doctor/appointments/work-items?pageSize=100") ?? []).Where(item => item.AppointmentId == created.AppointmentId));
            Assert.Equal(profile.PatientId, workItem.PatientId);
            Assert.Equal("Test Patient", workItem.PatientName);
            Assert.False(string.IsNullOrWhiteSpace(workItem.MedicalRecordNumber));
            Assert.False(workItem.HasBill);
            var accepted = await doctor.PutAsJsonAsync($"/api/appointments/{created.AppointmentId}/accept", new AppointmentDecisionRequest { Note = "Confirmed" }); var decision = await accepted.Content.ReadFromJsonAsync<AppointmentDto>(); Assert.Equal(HttpStatusCode.OK, accepted.StatusCode); Assert.NotNull(decision); Assert.Equal("Accepted", decision.Status);
            Assert.Equal("Accepted", (await patient.GetFromJsonAsync<AppointmentDto>($"/api/appointments/{created.AppointmentId}"))!.Status);
            var treatmentResponse = await doctor.PostAsJsonAsync($"/api/appointments/{created.AppointmentId}/treatment", new CreateTreatmentRequest { Diagnosis = "Test diagnosis", Prescription = "Test prescription", ProgressNotes = "Stable" }); var treatment = await treatmentResponse.Content.ReadFromJsonAsync<TreatmentDto>(); Assert.Equal(HttpStatusCode.OK, treatmentResponse.StatusCode); Assert.NotNull(treatment);
            Assert.Contains(await patient.GetFromJsonAsync<List<TreatmentDto>>($"/api/patients/{profile.PatientId}/history") ?? [], item => item.TreatmentId == treatment.TreatmentId);
            var summaryResponse = await doctor.PostAsync($"/api/patients/{profile.PatientId}/history-summary", null); var summary = await summaryResponse.Content.ReadFromJsonAsync<MedicalHistorySummaryDto>(); Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode); Assert.NotNull(summary); Assert.False(summary.AiAvailable); Assert.False(summary.IsAiGenerated); Assert.Null(summary.Summary); Assert.Contains("AI summary is currently unavailable", summary.Disclaimer); Assert.Contains(summary.History, item => item.TreatmentId == treatment.TreatmentId);
            await using (var scope = factory.Services.CreateAsyncScope()) { var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementDbContext>(); Assert.Contains(await dbContext.AiSummaryAudits.Where(audit => audit.PatientId == profile.PatientId).ToListAsync(), audit => audit.Outcome == "Unavailable" && audit.RecordCount == summary.History.Count); }
            var billResponse = await doctor.PostAsJsonAsync($"/api/appointments/{created.AppointmentId}/bill", new CreateBillRequest { Amount = 1200m, Description = "Consultation" }); var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>(); Assert.Equal(HttpStatusCode.OK, billResponse.StatusCode); Assert.NotNull(bill); Assert.Equal(1200m, bill.Amount);
            Assert.True((await doctor.GetFromJsonAsync<List<DoctorAppointmentWorkItemDto>>("/api/doctor/appointments/work-items?pageSize=100") ?? []).Single(item => item.AppointmentId == created.AppointmentId).HasBill);
            Assert.Equal(bill.BillId, (await patient.GetFromJsonAsync<BillDto>($"/api/bills/{bill.BillId}"))!.BillId);
            var notifications = await patient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications"); Assert.NotNull(notifications); var notification = Assert.Single(notifications, item => item.NotificationType == "BillGenerated"); var read = await patient.PutAsync($"/api/notifications/{notification.NotificationId}/read", null); Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var feedbackResponse = await patient.PostAsJsonAsync("/api/feedback", new CreateFeedbackRequest { AppointmentId = created.AppointmentId, Rating = 5, Comments = "Excellent" }); var feedback = await feedbackResponse.Content.ReadFromJsonAsync<FeedbackDto>(); Assert.Equal(HttpStatusCode.OK, feedbackResponse.StatusCode); Assert.NotNull(feedback); Assert.Equal((byte)5, feedback.Rating); Assert.Contains(await patient.GetFromJsonAsync<List<FeedbackDto>>("/api/feedback") ?? [], item => item.FeedbackId == feedback.FeedbackId);
            using var unrelatedDoctor = factory.CreateClient(); unrelatedDoctor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(UserRoles.Doctor, DateTime.UtcNow.AddMinutes(10))); Assert.Equal(HttpStatusCode.NotFound, (await unrelatedDoctor.GetAsync($"/api/patients/{profile.PatientId}/history")).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await unrelatedDoctor.PostAsync($"/api/patients/{profile.PatientId}/history-summary", null)).StatusCode);
        }
        finally { await DeleteUserAsync(factory, email); }
    }

    [Fact]
    public async Task AppointmentAndBillingRejectInvalidRequestsAndProtectOtherPatientsData()
    {
        var email = NewTestEmail();
        var otherEmail = NewTestEmail();
        using var factory = new TestWebApplicationFactory();
        using var patient = factory.CreateClient();
        using var otherPatient = factory.CreateClient();
        using var doctor = factory.CreateClient();

        try
        {
            var registration = await patient.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "AuthorizationTestPassword!1" });
            var patientAuth = await registration.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(patientAuth);
            patient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientAuth.AccessToken);
            Assert.Equal(HttpStatusCode.OK, (await patient.PutAsJsonAsync("/api/profile/me", new UpdatePatientProfileRequest { FirstName = "Test", LastName = "Patient", DateOfBirth = new DateOnly(1990, 1, 1) })).StatusCode);

            var otherRegistration = await otherPatient.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = otherEmail, Password = "AuthorizationTestPassword!1" });
            var otherAuth = await otherRegistration.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(otherAuth);
            otherPatient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherAuth.AccessToken);
            Assert.Equal(HttpStatusCode.OK, (await otherPatient.PutAsJsonAsync("/api/profile/me", new UpdatePatientProfileRequest { FirstName = "Other", LastName = "Patient", DateOfBirth = new DateOnly(1991, 1, 1) })).StatusCode);

            var doctorLogin = await doctor.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "dr.ada@hospital.example", Password = "DevelopmentOnly!123" });
            var doctorAuth = await doctorLogin.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Assert.NotNull(doctorAuth);
            doctor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorAuth.AccessToken);

            var validSlot = DateTime.UtcNow.Date.AddDays(20).AddHours(10);
            Assert.Equal(HttpStatusCode.Forbidden, (await doctor.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, DepartmentId = 1, AppointmentDateTime = validSlot })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, AppointmentDateTime = validSlot })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 999999, DepartmentId = 1, AppointmentDateTime = validSlot })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, DepartmentId = 1, AppointmentDateTime = DateTime.UtcNow.Date.AddDays(20).AddHours(8).AddMinutes(15) })).StatusCode);

            var createResponse = await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, DepartmentId = 1, AppointmentDateTime = validSlot, Reason = "Authorization test appointment" });
            var appointment = await createResponse.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(appointment);
            Assert.Equal(HttpStatusCode.Conflict, (await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, DepartmentId = 1, AppointmentDateTime = validSlot })).StatusCode);

            var rejected = await doctor.PutAsJsonAsync($"/api/appointments/{appointment.AppointmentId}/reject", new AppointmentDecisionRequest { Note = "Unavailable" });
            Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
            Assert.Equal("Rejected", (await rejected.Content.ReadFromJsonAsync<AppointmentDto>())!.Status);
            Assert.Equal(HttpStatusCode.Conflict, (await doctor.PutAsJsonAsync($"/api/appointments/{appointment.AppointmentId}/reject", new AppointmentDecisionRequest())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await patient.PostAsJsonAsync($"/api/appointments/{appointment.AppointmentId}/treatment", new CreateTreatmentRequest())).StatusCode);

            var billValidation = await doctor.PostAsJsonAsync($"/api/appointments/{appointment.AppointmentId}/bill", new CreateBillRequest { Amount = 0m });
            Assert.Equal(HttpStatusCode.BadRequest, billValidation.StatusCode);

            var completedAppointmentResponse = await patient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest { DoctorId = 1, DepartmentId = 1, AppointmentDateTime = validSlot.AddHours(1), Reason = "Billing test" });
            var completedAppointment = await completedAppointmentResponse.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(HttpStatusCode.Created, completedAppointmentResponse.StatusCode);
            Assert.NotNull(completedAppointment);
            Assert.Equal(HttpStatusCode.OK, (await doctor.PutAsJsonAsync($"/api/appointments/{completedAppointment.AppointmentId}/accept", new AppointmentDecisionRequest())).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await doctor.PostAsJsonAsync($"/api/appointments/{completedAppointment.AppointmentId}/treatment", new CreateTreatmentRequest { TreatmentNotes = "Complete appointment for billing validation" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await patient.PostAsJsonAsync($"/api/appointments/{completedAppointment.AppointmentId}/bill", new CreateBillRequest { Amount = 99.99m })).StatusCode);

            var billResponse = await doctor.PostAsJsonAsync($"/api/appointments/{completedAppointment.AppointmentId}/bill", new CreateBillRequest { Amount = 99.99m, Description = "Authorization test bill" });
            var bill = await billResponse.Content.ReadFromJsonAsync<BillDto>();
            Assert.Equal(HttpStatusCode.OK, billResponse.StatusCode);
            Assert.NotNull(bill);
            Assert.Equal(HttpStatusCode.NotFound, (await otherPatient.GetAsync($"/api/bills/{bill.BillId}")).StatusCode);
        }
        finally
        {
            await DeleteUserAsync(factory, otherEmail);
            await DeleteUserAsync(factory, email);
        }
    }

    [Theory]
    [InlineData(UserRoles.Patient)]
    [InlineData(UserRoles.Doctor)]
    public async Task NonAdministratorsCannotAccessAdministratorResource(string role)
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role, DateTime.UtcNow.AddMinutes(10)));

        var response = await client.GetAsync("/api/auth/admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/patients")).StatusCode);
    }

    private static async Task DeleteUserAsync(WebApplicationFactory<Program> factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementDbContext>();
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email);
        if (user is not null)
        {
            dbContext.Notifications.RemoveRange(await dbContext.Notifications.Where(notification => notification.UserId == user.UserId).ToListAsync());
            dbContext.Notifications.RemoveRange(await dbContext.Notifications.Where(notification => notification.Message.StartsWith("New appointment request for")).ToListAsync());
            var patient = await dbContext.Patients.SingleOrDefaultAsync(candidate => candidate.UserId == user.UserId);
            if (patient is not null)
            {
                dbContext.AiSummaryAudits.RemoveRange(await dbContext.AiSummaryAudits.Where(audit => audit.PatientId == patient.PatientId).ToListAsync());
                var appointments = await dbContext.Appointments.Where(candidate => candidate.PatientId == patient.PatientId).ToListAsync();
                var appointmentIds = appointments.Select(appointment => appointment.AppointmentId).ToList();
                dbContext.Feedbacks.RemoveRange(await dbContext.Feedbacks.Where(feedback => appointmentIds.Contains(feedback.AppointmentId)).ToListAsync());
                dbContext.Bills.RemoveRange(await dbContext.Bills.Where(bill => appointmentIds.Contains(bill.AppointmentId)).ToListAsync());
                dbContext.Treatments.RemoveRange(await dbContext.Treatments.Where(treatment => appointmentIds.Contains(treatment.AppointmentId)).ToListAsync());
                dbContext.Appointments.RemoveRange(appointments);
                dbContext.Patients.Remove(patient);
            }
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task DeleteDepartmentAsync(WebApplicationFactory<Program> factory, int departmentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementDbContext>();
        var department = await dbContext.Departments.SingleOrDefaultAsync(candidate => candidate.DepartmentId == departmentId);
        if (department is not null)
        {
            dbContext.Departments.Remove(department);
            await dbContext.SaveChangesAsync();
        }
    }

    private static string NewTestEmail() => $"hospital.test.{Guid.NewGuid():N}@example.test";

    private static string CreateToken(string role, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestWebApplicationFactory.SigningKey));
        var token = new JwtSecurityToken(
            TestWebApplicationFactory.Issuer,
            TestWebApplicationFactory.Audience,
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "999999"),
                new Claim(ClaimTypes.Email, "token@example.test"),
                new Claim(ClaimTypes.Role, role),
            },
            DateTime.UtcNow.AddMinutes(-10),
            expiresAt,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string Issuer = "HospitalManagementSystem.Tests";
    internal const string Audience = "HospitalManagementSystem.Tests";
    internal const string SigningKey = "hospital-api-test-signing-key-at-least-32-characters-long";

    public TestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Jwt__AccessTokenLifetimeMinutes", "60");
        Environment.SetEnvironmentVariable("ConnectionStrings__HospitalManagementDb", Environment.GetEnvironmentVariable("ConnectionStrings__HospitalManagementDb") ?? "Server=localhost,1433;Database=HospitalManagementDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("OpenAi__Enabled", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
