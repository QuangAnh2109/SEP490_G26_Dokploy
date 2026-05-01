using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.Course;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class CourseService(
    ICourseRepository repo,
    IEmailService emailService,
    IConfiguration config,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : ICourseService
{
    // CourseDTO.Role values projected by repo (CourseRepository.GetCoursesForUserAsync).
    private const string RoleTeacher = "Teacher";
    private const string RolePending = "Pending";

    // ── Read (no failure path at service level) ────────────────────────────

    public Task<List<CourseDTO>> GetCoursesForUserAsync(int userId) => repo.GetCoursesForUserAsync(userId);
    public Task<List<CourseDTO>> GetAllAsync() => repo.GetAllAsync();
    public Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId) => repo.GetStudentsInClassAsync(classId);
    public Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId) => repo.GetPendingStudentsAsync(classId);
    public Task<List<SubjectOptionDto>> GetSubjectsAsync() => repo.GetSubjectsAsync();

    // ── Read with membership check ─────────────────────────────────────────

    public async Task<Result<List<ExamInCourseDTO>>> GetExamsForCurrentUserAsync(int classId)
    {
        var membership = await GetActiveMembershipAsync(classId);
        if (membership.IsFailure) return membership.Error;

        var isTeacher = membership.Value.Role == RoleTeacher;
        return await repo.GetExamsByClassAsync(classId, isTeacher);
    }

    public async Task<Result<List<ChapterDTO>>> GetChaptersForCurrentUserAsync(int classId)
    {
        var membership = await GetActiveMembershipAsync(classId);
        if (membership.IsFailure) return membership.Error;

        var course = await repo.GetByIdAsync(classId);
        if (course == null) return CourseErrors.NotFound;
        return course.Chapters;
    }

    public async Task<Result<CourseDTO>> GetClassSettingsAsync(int classId)
    {
        var course = await repo.GetByIdAsync(classId);
        if (course == null) return CourseErrors.NotFound;
        return course;
    }

    private async Task<Result<CourseDTO>> GetActiveMembershipAsync(int classId)
    {
        var myCourses = await repo.GetCoursesForUserAsync(currentUser.UserId);
        var membership = myCourses.FirstOrDefault(c => c.ClassId == classId);
        if (membership == null || membership.Role == RolePending)
            return CourseErrors.AccessDenied;
        return membership;
    }

    // ── Mutating ───────────────────────────────────────────────────────────

    public async Task<Result<CourseDTO>> CreateCourseAsync(int teacherId, CreateCourseRequestDTO dto)
    {
        var normalizedSemester = dto.Semester?.Trim().ToUpper() ?? string.Empty;

        var subjectId = dto.SubjectId!.Value;
        var duplicateError = await repo.GetDuplicateClassErrorAsync(teacherId, dto.ClassName!, normalizedSemester, subjectId);
        if (duplicateError != null)
            return CourseErrors.Duplicate;

        var newClass = new Class
        {
            Name = dto.ClassName,
            Semester = normalizedSemester,
            SubjectId = subjectId,
            TeacherId = teacherId,
            Status = 1,
            InvitationCodeStatus = 1,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        var created = await repo.CreateCourseAsync(newClass);
        return Result<CourseDTO>.Success(created);
    }

    public async Task<Result> JoinCourseAsync(int studentId, string? inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return CourseErrors.InviteCodeInvalid;

        var course = await repo.GetClassByInviteCodeAsync(inviteCode);
        if (course == null)
            return CourseErrors.InviteCodeInvalid;

        if (await repo.IsUserInClassAsync(course.ClassId, studentId))
            return CourseErrors.AlreadyMember;

        await repo.JoinClassAsync(course.ClassId, studentId);
        return Result.Success();
    }

    public async Task<Result> LeaveCourseAsync(int classId, int userId)
    {
        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;
        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        await repo.LeaveClassAsync(classId, userId);
        return Result.Success();
    }

    public async Task<Result> UpdateClassSettingsAsync(int classId, string? newName, int? invitationStatus)
    {
        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;
        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        var success = await repo.UpdateClassSettingsAsync(classId, newName!, invitationStatus!.Value);
        return success ? Result.Success() : CourseErrors.NotFound;
    }

    public async Task<Result<InviteStudentResultDTO>> InviteStudentByEmailAsync(int teacherId, int classId, string? studentEmail)
    {
        var frontendBase = config["FrontendSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBase))
            return CourseErrors.ConfigError;

        var user = await repo.GetUserWithRoleByEmailAsync(studentEmail!);
        if (user == null)
            return CourseErrors.StudentNotFound;

        if (user.RoleId.ToString() != RoleIds.Student)
            return CourseErrors.UserNotStudent;

        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;

        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        var existing = await repo.GetClassMemberAsync(classId, user.UserId);
        if (existing != null)
        {
            if (existing.MemberStatus == MemberStatus.Active)
                return CourseErrors.AlreadyMember;

            if (existing.MemberStatus == MemberStatus.Invited)
                return CourseErrors.AlreadyInvited;

            if (existing.MemberStatus == MemberStatus.Pending)
            {
                await repo.UpdateClassMemberStatusAsync(classId, user.UserId, MemberStatus.Active);
                return new InviteStudentResultDTO { AutoApproved = true };
            }
        }

        var membership = await repo.InviteStudentAsync(classId, user.UserId);
        if (membership == null)
            return CourseErrors.NotFound;

        var token = BuildInviteToken(classId, membership.ConcurrencyStamp);
        var inviteLink = $"{frontendBase}/Course/AcceptInvite?token={token}";
        var body = $"<p>Bạn được mời tham gia lớp học <strong>{course.ClassName}</strong>.</p>" +
                   $"<p><a href=\"{inviteLink}\">Nhấn vào đây để tham gia</a></p>";
        await emailService.SendEmailAsync(studentEmail!, "Thư mời tham gia lớp học", body);

        return new InviteStudentResultDTO { Token = token, AutoApproved = false };
    }

    public async Task<Result> AcceptInvitationAsync(int studentId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return CourseErrors.InviteTokenInvalid;

        if (!TryDecodeInviteToken(token, out int classId, out byte[]? stamp) || stamp is null)
            return CourseErrors.InviteTokenInvalid;

        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;
        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        var rows = await repo.AcceptEmailInvitationAsync(classId, studentId, stamp);
        return rows == 0 ? CourseErrors.InviteTokenInvalid : Result.Success();
    }

    public async Task<Result> ApproveStudentAsync(int classId, int studentId)
    {
        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;
        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        var success = await repo.ApproveStudentAsync(classId, studentId);
        return success ? Result.Success() : CourseErrors.NotMember;
    }

    public async Task<Result> RejectStudentAsync(int classId, int studentId)
    {
        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;
        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        var success = await repo.RejectStudentAsync(classId, studentId);
        return success ? Result.Success() : CourseErrors.NotMember;
    }

    public async Task<Result> RemoveStudentAsync(int classId, int studentId)
    {
        var course = await repo.GetByIdAsync(classId);
        if (course == null)
            return CourseErrors.NotFound;
        if (course.Status == ClassStatus.Closed)
            return CourseErrors.Closed;

        var success = await repo.RemoveStudentAsync(classId, studentId);
        return success ? Result.Success() : CourseErrors.NotMember;
    }

    public async Task<Result> CloseClassAsync(int classId)
    {
        var success = await repo.CloseClassAsync(classId);
        return success ? Result.Success() : CourseErrors.NotFound;
    }

    public async Task<Result> ReopenClassAsync(int classId)
    {
        var success = await repo.ReopenClassAsync(classId);
        return success ? Result.Success() : CourseErrors.NotFound;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string BuildInviteToken(int classId, byte[] concurrencyStamp)
    {
        var stampBase64 = ToUrlSafeBase64(concurrencyStamp);
        var plain = $"{classId}:{stampBase64}";
        return ToUrlSafeBase64(System.Text.Encoding.UTF8.GetBytes(plain));
    }

    private static bool TryDecodeInviteToken(string token, out int classId, out byte[]? stamp)
    {
        classId = 0;
        stamp = null;
        try
        {
            var plain = System.Text.Encoding.UTF8.GetString(FromUrlSafeBase64(token));
            var parts = plain.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out classId))
                return false;

            stamp = FromUrlSafeBase64(parts[1]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ToUrlSafeBase64(byte[] data) =>
        Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static byte[] FromUrlSafeBase64(string s)
    {
        var base64 = s.Replace("-", "+").Replace("_", "/");
        base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(base64);
    }
}
