using Backend.DTOs.Course;
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
    public class CourseRepo : ICourseRepo
    {
        private readonly MtcaSep490G26Context _context;

        public CourseRepo(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<List<CourseDTO>> GetCoursesForUserAsync(int userId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == userId || c.ClassMembers.Any(m => m.StudentId == userId && (m.MemberStatus == Backend.Constants.MemberStatus.Active || m.MemberStatus == Backend.Constants.MemberStatus.Pending)))
                .Select(c => new CourseDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    // Use the Subject navigation for subject name
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    SubjectCode = c.Subject != null ? c.Subject.Code : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    InvitationCodeStatus = c.InvitationCodeStatus,
                    // Map Semester from DB
                    Semester = c.Semester ?? string.Empty,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Role = c.TeacherId == userId ? "Teacher" : (c.ClassMembers.Any(m => m.StudentId == userId && m.MemberStatus == Backend.Constants.MemberStatus.Pending) ? "Pending" : "Student")
                })
                .ToListAsync();
        }

        public async Task<List<CourseDTO>> GetAllAsync()
        {
            return await _context.Classes
                .Select(c => new CourseDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    SubjectCode = c.Subject != null ? c.Subject.Code : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    InvitationCodeStatus = c.InvitationCodeStatus,
                    Semester = c.Semester ?? string.Empty,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Role = "Teacher" // When listing all classes, role is not user-specific; consumer can ignore or override.
                })
                .ToListAsync();
        }

        public async Task<CourseDTO?> GetByIdAsync(int classId)
        {
            return await _context.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new CourseDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    SubjectId = c.SubjectId,
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    SubjectCode = c.Subject != null ? c.Subject.Code : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    InvitationCodeStatus = c.InvitationCodeStatus,
                    Semester = c.Semester ?? string.Empty,
                    Chapters = c.Subject.Chapters
                        .Select(ch => new ChapterDTO
                        {
                            ChapterId = ch.ChapterId,
                            SubjectId = ch.SubjectId,
                            Name = ch.Name
                        })
                        .OrderBy(ch => ch.Name)
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        // Return exams that belong to the class and are visible now.
        // Exams with NULL VisibleFrom are treated as immediately visible.
        // Compute Status based on OpenAt / CloseAt:
        // - 1 => Open (now between OpenAt and CloseAt)
        // - 2 => Upcoming (within 30 minutes before OpenAt)
        // - 0 => Closed (otherwise)
        public async Task<List<ExamInCourseDTO>> GetExamsByClassAsync(int classId)
        {
            var now = DateTime.UtcNow;
            var upcomingThreshold = now.AddMinutes(30);

            // Include exams where VisibleFrom is null (considered visible immediately)
            // or VisibleFrom is in the past (<= now).
            var query = _context.Exams
                .Where(e => e.ClassId == classId && (e.VisibleFrom == null || e.VisibleFrom <= now));

            // Project to DTO including ChapterId and computed Status
            return await query
                .Select(e => new ExamInCourseDTO
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
                    // Compute status using OpenAt/CloseAt where available, otherwise fall back to stored Status.
                    Status = e.OpenAt != null
                        ? ((e.OpenAt <= now && (e.CloseAt == null || e.CloseAt >= now)) ? 1
                            : (e.OpenAt > now && e.OpenAt <= upcomingThreshold) ? 2
                            : 0)
                        : e.Status,
                    ShowScore = e.ShowScore,
                    ShowAnswer = e.ShowAnswer,
                    AnswerTimingMode = e.AnswerTimingMode
                })
                .ToListAsync();
        }

        public async Task<string?> GetDuplicateClassErrorAsync(int teacherId, string className, string semester, int subjectId)
        {
            var existingClasses = await _context.Classes
                .Where(c => c.TeacherId == teacherId && c.Name == className)
                .ToListAsync();

            foreach (var c in existingClasses)
            {
                if (c.Semester == semester)
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

        public async Task<CourseDTO> CreateCourseAsync(Class newClass)
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

            return new CourseDTO
            {
                ClassId = newClass.ClassId,
                ClassName = newClass.Name,
                SubjectId = newClass.SubjectId,
                SubjectName = newClass.Subject?.Name ?? string.Empty,
                TeacherName = newClass.Teacher?.FullName ?? string.Empty,
                InvitationCode = newClass.InvitationCode,
                Semester = newClass.Semester ?? string.Empty,
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
            return await _context.ClassMembers
                .Where(cm => cm.ClassId == classId && cm.MemberStatus == Backend.Constants.MemberStatus.Pending)
                .Select(cm => new StudentInClassDTO
                {
                    StudentId = cm.StudentId,
                    FullName = cm.Student != null ? cm.Student.FullName : string.Empty,
                    Email = cm.Student != null ? cm.Student.Email : string.Empty,
                    StudentCode = cm.Student != null ? cm.Student.StudentId : string.Empty,
                    JoinedAtUtc = DateTime.UtcNow
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
            return await _context.ClassMembers
                .Where(cm => cm.ClassId == classId && cm.MemberStatus == Backend.Constants.MemberStatus.Active)
                .Select(cm => new StudentInClassDTO
                {
                    StudentId = cm.StudentId,
                    FullName = cm.Student != null ? cm.Student.FullName : string.Empty,
                    Email = cm.Student != null ? cm.Student.Email : string.Empty,
                    StudentCode = cm.Student != null ? cm.Student.StudentId : string.Empty,
                    JoinedAtUtc = DateTime.UtcNow // Fallback since the DB doesn't track this
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateClassSettingsAsync(int classId, string newName, int invitationStatus)
        {
            var course = await _context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId);
            if (course == null) return false;

            course.Name = newName;
            course.InvitationCodeStatus = invitationStatus;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SubjectOptionDto>> GetSubjectsAsync()
        {
            return await _context.Subjects
                .AsNoTracking()
                .Select(s => new SubjectOptionDto
                {
                    SubjectId = s.SubjectId,
                    Name = s.Name,
                    Code = s.Code
                })
                .ToListAsync();
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
    }
}