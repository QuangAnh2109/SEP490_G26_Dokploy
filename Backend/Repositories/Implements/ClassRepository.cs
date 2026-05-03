using Backend.Constants;
using Backend.DTOs.Class;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Repositories.Implements
{
    public class ClassRepository : IClassRepository
    {
        private readonly MtcaSep490G26Context _context;
        private readonly TimeProvider _timeProvider;

        public ClassRepository(MtcaSep490G26Context context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<List<ClassDTO>> GetClassesForUserAsync(int userId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == userId || c.ClassMembers.Any(m => m.StudentId == userId && (m.MemberStatus == Backend.Constants.MemberStatus.Active || m.MemberStatus == Backend.Constants.MemberStatus.Pending)))
                .Select(c => new ClassDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    // Use the Subject navigation for subject name
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    SubjectCode = c.Subject != null ? c.Subject.Code : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName ?? string.Empty : string.Empty,
                    InvitationCode = c.InvitationCode,
                    InvitationCodeStatus = c.InvitationCodeStatus,
                    // Map Semester from DB
                    SemesterId = c.SemesterId,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Status = c.Status,
                    Role = c.TeacherId == userId ? "Teacher" : (c.ClassMembers.Any(m => m.StudentId == userId && m.MemberStatus == Backend.Constants.MemberStatus.Pending) ? "Pending" : "Student")
                })
                .ToListAsync();
        }

        public async Task<List<ClassDTO>> GetAllAsync()
        {
            return await _context.Classes
                .Select(c => new ClassDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    SubjectCode = c.Subject != null ? c.Subject.Code : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName ?? string.Empty : string.Empty,
                    InvitationCode = c.InvitationCode,
                    InvitationCodeStatus = c.InvitationCodeStatus,
                    SemesterId = c.SemesterId,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Status = c.Status,
                    Role = "Teacher" // When listing all classes, role is not user-specific; consumer can ignore or override.
                })
                .ToListAsync();
        }

        public async Task<ClassDTO?> GetByIdAsync(int classId)
        {
            return await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new ClassDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    SubjectCode = c.Subject != null ? c.Subject.Code : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName ?? string.Empty : string.Empty,
                    InvitationCode = c.InvitationCode,
                    InvitationCodeStatus = c.InvitationCodeStatus,
                    SemesterId = c.SemesterId,
                    Status = c.Status,
                    Chapters = c.Subject != null ? c.Subject.Chapters
                        .Where(ch => ch.Status == ChapterStatus.Active)
                        .Select(ch => new ChapterDTO
                        {
                            ChapterId = ch.ChapterId,
                            SubjectId = ch.SubjectId,
                            Name = ch.Name
                        })
                        .OrderBy(ch => ch.Name)
                        .ToList() : new List<ChapterDTO>()
                })
                .FirstOrDefaultAsync();
        }

        // Return exams that belong to the class and are visible now.
        // Exams with NULL VisibleFrom are treated as immediately visible.
        // Compute Status based on OpenAt / CloseAt:
        // - 1 => Open (now between OpenAt and CloseAt)
        // - 2 => Upcoming (within 30 minutes before OpenAt)
        // - 0 => Closed (otherwise)
        public async Task<List<ExamInClassDTO>> GetExamsByClassAsync(int classId, bool isTeacher = false)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var upcomingThreshold = now.AddMinutes(30);

            IQueryable<Models.Exam> query;

            if (isTeacher)
            {
                // Teachers see all exams except hard-deleted ones
                query = _context.Exams
                    .Where(e => e.ClassId == classId);
            }
            else
            {
                // Students only see Published (1), InProgress (2), Closed (5)
                // and only if VisibleFrom has passed
                query = _context.Exams
                    .Where(e => e.ClassId == classId
                        && (e.Status == Backend.Constants.ExamStatus.Published
                            || e.Status == Backend.Constants.ExamStatus.InProgress
                            || e.Status == Backend.Constants.ExamStatus.Closed)
                        && (e.VisibleFrom == null || e.VisibleFrom <= now));
            }

            // Project to DTO including ChapterId and computed Status
            return await query
                .Select(e => new ExamInClassDTO
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    Code = null,
                    SubjectName = e.Subject != null ? e.Subject.Name : string.Empty,
                    TeacherName = e.Teacher != null ? e.Teacher.FullName : null,
                    ChapterId = _context.ExamBlueprintChapters
                                    .Where(ebc => e.ExamBlueprintId != null && ebc.ExamBlueprintId == e.ExamBlueprintId)
                                    .Select(ebc => (int?)ebc.ChapterId)
                                    .FirstOrDefault(),

                    VisibleFrom = e.VisibleFrom,
                    OpenAt = e.OpenAt,
                    CloseAt = e.CloseAt,
                    DurationMinutes = e.Duration,
                    // For teachers: use the raw DB status directly
                    // For students: compute status from OpenAt/CloseAt
                    Status = isTeacher
                        ? e.Status
                        : (e.OpenAt != null
                            ? ((e.OpenAt <= now && (e.CloseAt == null || e.CloseAt >= now)) ? 1
                                : (e.OpenAt > now && e.OpenAt <= upcomingThreshold) ? 2
                                : 0)
                            : e.Status),
                    ShowScore = e.ShowScore,
                    ShowAnswer = e.ShowAnswer,
                    AnswerTimingMode = e.AnswerTimingMode
                })
                .ToListAsync();
        }

        public async Task<string?> GetDuplicateClassErrorAsync(int teacherId, string className, int semesterId, int subjectId)
        {
            var existingClasses = await _context.Classes
                .Where(c => c.TeacherId == teacherId && c.Name == className)
                .ToListAsync();

            foreach (var c in existingClasses)
            {
                if (c.SemesterId == semesterId)
                {
                    return "Lớp học này đã tồn tại trong học kỳ được chọn";
                }
                if (c.SubjectId == subjectId)
                {
                    return "Lớp học này đã học môn này ở học kỳ khác";
                }
            }
            return null;
        }

        private string GenerateInvitationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return code;
        }

        public async Task<ClassDTO> CreateClassAsync(Class newClass)
        {
            bool isUnique = false;
            string code = "";
            while (!isUnique)
            {
                code = GenerateInvitationCode();
                isUnique = !await _context.Classes.AnyAsync(c => c.InvitationCode.ToLower() == code.ToLower());
            }

            newClass.InvitationCode = code;
            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            // Load Subject and Teacher for the returned DTO
            await _context.Entry(newClass).Reference(c => c.Subject).LoadAsync();
            await _context.Entry(newClass).Reference(c => c.Teacher).LoadAsync();

            return new ClassDTO
            {
                ClassId = newClass.ClassId,
                ClassName = newClass.Name,
                SubjectId = newClass.SubjectId,
                SubjectName = newClass.Subject?.Name ?? string.Empty,
                TeacherName = newClass.Teacher?.FullName ?? string.Empty,
                InvitationCode = newClass.InvitationCode,
                SemesterId = newClass.SemesterId,
                StudentCount = 0,
                ExamCount = 0,
                Role = "Teacher"
            };
        }

        public async Task<Class?> GetClassByInviteCodeAsync(string inviteCode)
        {
            var lowerInviteCode = inviteCode?.ToLower();
            return await _context.Classes
                .FirstOrDefaultAsync(c => c.InvitationCode.ToLower() == lowerInviteCode && c.Status == 1 && c.InvitationCodeStatus == 1);
        }

        public async Task<bool> IsUserInClassAsync(int classId, int userId)
        {
            return await _context.ClassMembers
                .AnyAsync(cm => cm.ClassId == classId && cm.StudentId == userId && cm.MemberStatus == Backend.Constants.MemberStatus.Active);
        }

        public async Task JoinClassAsync(int classId, int userId)
        {
            var membership = await _context.ClassMembers
                .FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == userId);

            if (membership == null)
            {
                membership = new ClassMember
                {
                    ClassId = classId,
                    StudentId = userId,
                    MemberStatus = Backend.Constants.MemberStatus.Pending 
                };
                _context.ClassMembers.Add(membership);
            }
            else
            {
                if (membership.MemberStatus != Backend.Constants.MemberStatus.Active)
                {
                    membership.MemberStatus = Backend.Constants.MemberStatus.Pending;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<ClassMember?> InviteStudentAsync(int classId, int studentId)
        {
            var membership = await _context.ClassMembers
                .FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);

            if (membership == null)
            {
                membership = new ClassMember
                {
                    ClassId = classId,
                    StudentId = studentId,
                    MemberStatus = Backend.Constants.MemberStatus.Invited
                };
                _context.ClassMembers.Add(membership);
            }
            else
            {
                if (membership.MemberStatus == Backend.Constants.MemberStatus.Active)
                    return membership; // already active
                
                membership.MemberStatus = Backend.Constants.MemberStatus.Invited;
            }
            await _context.SaveChangesAsync();
            return membership;
        }

        public async Task<int> AcceptEmailInvitationAsync(int classId, int studentId, byte[] concurrencyStamp)
        {
            return await _context.ClassMembers
                .Where(cm => cm.ClassId == classId 
                             && cm.StudentId == studentId 
                             && cm.ConcurrencyStamp == concurrencyStamp)
                .ExecuteUpdateAsync(s => s.SetProperty(cm => cm.MemberStatus, Backend.Constants.MemberStatus.Active));
        }

        public async Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            return await _context.ClassMembers
                .Where(cm => cm.ClassId == classId && cm.MemberStatus == Backend.Constants.MemberStatus.Pending)
                .Select(cm => new StudentInClassDTO
                {
                    StudentId = cm.StudentId,
                    FullName = cm.Student != null ? cm.Student.FullName ?? string.Empty : string.Empty,
                    Email = cm.Student != null ? cm.Student.Email : string.Empty,
                    StudentCode = cm.Student != null ? cm.Student.StudentId ?? string.Empty : string.Empty,
                    JoinedAtUtc = now
                })
                .ToListAsync();
        }

        public async Task<bool> ApproveStudentAsync(int classId, int studentId)
        {
            var rows = await _context.ClassMembers
                .Where(cm => cm.ClassId == classId 
                             && cm.StudentId == studentId 
                             && cm.MemberStatus == Backend.Constants.MemberStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(cm => cm.MemberStatus, Backend.Constants.MemberStatus.Active));
            return rows > 0;
        }

        public async Task<bool> RejectStudentAsync(int classId, int studentId)
        {
            var rows = await _context.ClassMembers
                .Where(cm => cm.ClassId == classId 
                             && cm.StudentId == studentId 
                             && cm.MemberStatus == Backend.Constants.MemberStatus.Pending)
                .ExecuteDeleteAsync();
            return rows > 0;
        }

        public async Task<bool> RemoveStudentAsync(int classId, int studentId)
        {
            var rows = await _context.ClassMembers
                .Where(cm => cm.ClassId == classId
                             && cm.StudentId == studentId 
                             && cm.MemberStatus == Backend.Constants.MemberStatus.Active)
                .ExecuteDeleteAsync();
            return rows > 0;
        }

        public async Task LeaveClassAsync(int classId, int userId)
        {
            var membership = await _context.ClassMembers
                .FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == userId);

            if (membership != null)
            {
                _context.ClassMembers.Remove(membership);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            return await _context.ClassMembers
                .Where(cm => cm.ClassId == classId && cm.MemberStatus == Backend.Constants.MemberStatus.Active)
                .Select(cm => new StudentInClassDTO
                {
                    StudentId = cm.StudentId,
                    FullName = cm.Student != null ? cm.Student.FullName ?? string.Empty : string.Empty,
                    Email = cm.Student != null ? cm.Student.Email : string.Empty,
                    StudentCode = cm.Student != null ? cm.Student.StudentId ?? string.Empty : string.Empty,
                    JoinedAtUtc = now // Fallback since the DB doesn't track this
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateClassSettingsAsync(int classId, string newName, int invitationStatus)
        {
            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId);
            if (cls == null) return false;

            cls.Name = newName;
            cls.InvitationCodeStatus = invitationStatus;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SubjectOptionDto>> GetSubjectsAsync()
        {
            return await _context.Subjects
                .AsNoTracking()
                .Where(s => s.Status == SubjectStatus.Active)
                .Select(s => new SubjectOptionDto
                {
                    SubjectId = s.SubjectId,
                    Name = s.Name,
                    Code = s.Code
                })
                .ToListAsync();
        }

        public async Task<List<SemesterOptionDto>> GetSemesterOptionsAsync()
        {
            return await _context.Semesters
                .AsNoTracking()
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SemesterOptionDto
                {
                    SemesterId = s.SemesterId,
                    Code = s.Code,
                    Name = s.Name,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status
                })
                .ToListAsync();
        }

        public async Task<(DateOnly StartDate, DateOnly EndDate)?> GetSemesterRangeAsync(int classId)
        {
            return await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => (ValueTuple<DateOnly, DateOnly>?)ValueTuple.Create(c.Semester.StartDate, c.Semester.EndDate))
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserWithRoleByEmailAsync(string email)
        {
            return await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ClassMember?> GetClassMemberAsync(int classId, int studentId)
        {
            return await _context.ClassMembers.FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);
        }

        public async Task UpdateClassMemberStatusAsync(int classId, int studentId, int status)
        {
            var membership = await _context.ClassMembers.FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);
            if (membership != null)
            {
                membership.MemberStatus = status;
                await _context.SaveChangesAsync();
            }
        }

        public Task<bool> IsTeacherOfClassAsync(int classId, int teacherUserId)
        {
            return _context.Classes.AnyAsync(c => c.ClassId == classId && c.TeacherId == teacherUserId);
        }

        public Task<bool> StudentHasInProgressSubmissionInClassAsync(int classId, int studentId)
        {
            return _context.Submissions.AnyAsync(s =>
                s.StudentId == studentId
                && s.Status == SubmissionStatus.InProgress
                && s.Paper != null
                && s.Paper.Exam != null
                && s.Paper.Exam.ClassId == classId);
        }

        public async Task<bool> RemoveActiveStudentFromClassAsync(int classId, int studentId)
        {
            var rows = await _context.ClassMembers
                .Where(cm => cm.ClassId == classId
                             && cm.StudentId == studentId
                             && cm.MemberStatus == Backend.Constants.MemberStatus.Active)
                .ExecuteDeleteAsync();
            return rows > 0;
        }
        public async Task<bool> CloseClassAsync(int classId)
        {
            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId);
            if (cls == null) return false;

            cls.Status = Backend.Constants.ClassStatus.Closed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReopenClassAsync(int classId)
        {
            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId);
            if (cls == null) return false;

            cls.Status = Backend.Constants.ClassStatus.Active;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> BulkCloseBySemesterAsync(int semesterId)
        {
            return await _context.Classes
                .Where(c => c.SemesterId == semesterId && c.Status == Backend.Constants.ClassStatus.Active)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, Backend.Constants.ClassStatus.Closed));
        }
    }
}