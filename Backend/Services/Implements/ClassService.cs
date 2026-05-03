using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.Class;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class ClassService(
    IClassRepository repo,
    IEmailService emailService,
    IConfiguration config,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    ISemesterRepository semesterRepo,
    ISubjectRepository subjectRepo) : IClassService
{
    // ClassDTO.Role values projected by repo (ClassRepository.GetClassesForUserAsync).
    private const string RoleTeacher = "Teacher";
    private const string RolePending = "Pending";

    // ── Read (no failure path at service level) ────────────────────────────

    public Task<List<ClassDTO>> GetClassesForUserAsync(int userId) => repo.GetClassesForUserAsync(userId);
    public Task<List<ClassDTO>> GetAllAsync() => repo.GetAllAsync();
    public Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId) => repo.GetStudentsInClassAsync(classId);
    public Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId) => repo.GetPendingStudentsAsync(classId);
    public Task<List<SubjectOptionDto>> GetSubjectsAsync() => repo.GetSubjectsAsync();
    public Task<List<SemesterOptionDto>> GetSemestersAsync() => repo.GetSemesterOptionsAsync();

    // ── Read with membership check ─────────────────────────────────────────

    public async Task<Result<List<ExamInClassDTO>>> GetExamsForCurrentUserAsync(int classId)
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

        var cls = await repo.GetByIdAsync(classId);
        if (cls == null) return ClassErrors.NotFound;
        return cls.Chapters;
    }

    public async Task<Result<ClassDTO>> GetClassSettingsAsync(int classId)
    {
        var cls = await repo.GetByIdAsync(classId);
        if (cls == null) return ClassErrors.NotFound;
        return cls;
    }

    private async Task<Result<ClassDTO>> GetActiveMembershipAsync(int classId)
    {
        var myClasses = await repo.GetClassesForUserAsync(currentUser.UserId);
        var membership = myClasses.FirstOrDefault(c => c.ClassId == classId);
        if (membership == null || membership.Role == RolePending)
            return ClassErrors.AccessDenied;
        return membership;
    }

    // ── Mutating ───────────────────────────────────────────────────────────

    public async Task<Result<ClassDTO>> CreateClassAsync(int teacherId, CreateClassRequestDTO dto)
    {
        var semester = await semesterRepo.GetByIdAsync(dto.SemesterId!.Value);
        if (semester is null) return SemesterErrors.NotFound;
        if (semester.Status == SemesterStatus.Closed) return SemesterErrors.Closed;

        var subject = await subjectRepo.GetByIdAsync(dto.SubjectId!.Value);
        if (subject is null) return SubjectErrors.NotFound;
        if (subject.Status == SubjectStatus.Closed) return SubjectErrors.Closed;

        var duplicateError = await repo.GetDuplicateClassErrorAsync(teacherId, dto.ClassName!, semester.SemesterId, subject.SubjectId);
        if (duplicateError != null)
            return ClassErrors.Duplicate;

        var newClass = new Class
        {
            Name = dto.ClassName,
            SemesterId = semester.SemesterId,
            SubjectId = subject.SubjectId,
            TeacherId = teacherId,
            Status = 1,
            InvitationCodeStatus = 1,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        var created = await repo.CreateClassAsync(newClass);
        return Result<ClassDTO>.Success(created);
    }

    public async Task<Result> JoinClassAsync(int studentId, string? inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return ClassErrors.InviteCodeInvalid;

        var cls = await repo.GetClassByInviteCodeAsync(inviteCode);
        if (cls == null)
            return ClassErrors.InviteCodeInvalid;

        if (await repo.IsUserInClassAsync(cls.ClassId, studentId))
            return ClassErrors.AlreadyMember;

        await repo.JoinClassAsync(cls.ClassId, studentId);
        return Result.Success();
    }

    public async Task<Result> LeaveClassAsync(int classId, int userId)
    {
        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;
        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        await repo.LeaveClassAsync(classId, userId);
        return Result.Success();
    }

    public async Task<Result> UpdateClassSettingsAsync(int classId, string? newName, int? invitationStatus)
    {
        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;
        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        var success = await repo.UpdateClassSettingsAsync(classId, newName!, invitationStatus!.Value);
        return success ? Result.Success() : ClassErrors.NotFound;
    }

    public async Task<Result<InviteStudentResultDTO>> InviteStudentByEmailAsync(int teacherId, int classId, string? studentEmail)
    {
        var frontendBase = config["FrontendSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBase))
            return ClassErrors.ConfigError;

        var user = await repo.GetUserWithRoleByEmailAsync(studentEmail!);
        if (user == null)
            return ClassErrors.StudentNotFound;

        if (user.RoleId.ToString() != RoleIds.Student)
            return ClassErrors.UserNotStudent;

        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;

        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        var existing = await repo.GetClassMemberAsync(classId, user.UserId);
        if (existing != null)
        {
            if (existing.MemberStatus == MemberStatus.Active)
                return ClassErrors.AlreadyMember;

            if (existing.MemberStatus == MemberStatus.Invited)
                return ClassErrors.AlreadyInvited;

            if (existing.MemberStatus == MemberStatus.Pending)
            {
                await repo.UpdateClassMemberStatusAsync(classId, user.UserId, MemberStatus.Active);
                return new InviteStudentResultDTO { AutoApproved = true };
            }
        }

        var membership = await repo.InviteStudentAsync(classId, user.UserId);
        if (membership == null)
            return ClassErrors.NotFound;

        var token = BuildInviteToken(classId, membership.ConcurrencyStamp);
        var inviteLink = $"{frontendBase}/Class/AcceptInvite?token={token}";
        var body = $"<p>Bạn được mời tham gia lớp học <strong>{cls.ClassName}</strong>.</p>" +
                   $"<p><a href=\"{inviteLink}\">Nhấn vào đây để tham gia</a></p>";
        await emailService.SendEmailAsync(studentEmail!, "Thư mời tham gia lớp học", body);

        return new InviteStudentResultDTO { Token = token, AutoApproved = false };
    }

    public async Task<Result> AcceptInvitationAsync(int studentId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ClassErrors.InviteTokenInvalid;

        if (!TryDecodeInviteToken(token, out int classId, out byte[]? stamp) || stamp is null)
            return ClassErrors.InviteTokenInvalid;

        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;
        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        var rows = await repo.AcceptEmailInvitationAsync(classId, studentId, stamp);
        return rows == 0 ? ClassErrors.InviteTokenInvalid : Result.Success();
    }

    public async Task<Result> ApproveStudentAsync(int classId, int studentId)
    {
        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;
        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        var success = await repo.ApproveStudentAsync(classId, studentId);
        return success ? Result.Success() : ClassErrors.NotMember;
    }

    public async Task<Result> RejectStudentAsync(int classId, int studentId)
    {
        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;
        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        var success = await repo.RejectStudentAsync(classId, studentId);
        return success ? Result.Success() : ClassErrors.NotMember;
    }

    public async Task<Result> RemoveStudentAsync(int classId, int studentId)
    {
        var cls = await repo.GetByIdAsync(classId);
        if (cls == null)
            return ClassErrors.NotFound;
        if (cls.Status == ClassStatus.Closed)
            return ClassErrors.Closed;

        var success = await repo.RemoveStudentAsync(classId, studentId);
        return success ? Result.Success() : ClassErrors.NotMember;
    }

    public async Task<Result> CloseClassAsync(int classId)
    {
        var success = await repo.CloseClassAsync(classId);
        return success ? Result.Success() : ClassErrors.NotFound;
    }

    public async Task<Result> ReopenClassAsync(int classId)
    {
        var success = await repo.ReopenClassAsync(classId);
        return success ? Result.Success() : ClassErrors.NotFound;
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
