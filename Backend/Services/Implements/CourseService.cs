using Backend.DTOs.Course;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Backend.Constants;

namespace Backend.Services.Implements
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepo _repo;
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public CourseService(ICourseRepo repo, IEmailService emailService, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _repo = repo;
            _emailService = emailService;
            _config = config;
        }

        public Task<List<CourseDTO>> GetCoursesForUserAsync(int userId)
        {
            // Keep business logic minimal for now: delegate to repo.
            // Additional business rules (filtering, sorting, masking invitation codes, etc.) can be added here.
            return _repo.GetCoursesForUserAsync(userId);
        }

        public Task<List<CourseDTO>> GetAllAsync()
        {
            return _repo.GetAllAsync();
        } //k test

        public Task<CourseDTO?> GetByIdAsync(int classId)
        {
            return _repo.GetByIdAsync(classId);
        }

        // New: delegate to repo
        public Task<List<ExamInCourseDTO>> GetExamsByClassAsync(int classId)
        {
            return _repo.GetExamsByClassAsync(classId);
        }
        public async Task<CourseDTO> CreateCourseAsync(int teacherId, CreateCourseRequestDTO dto)
        {
            var normalizedSemester = dto.Semester?.Trim().ToUpper() ?? "";
            
            // Kiểm tra trùng lặp
            var duplicateError = await _repo.GetDuplicateClassErrorAsync(teacherId, dto.ClassName, normalizedSemester, dto.SubjectId);
            if (duplicateError != null)
            {
                throw new System.Exception(duplicateError); // Throw an exception to be caught by the controller
            }

            // Create new Class entity
            var newClass = new Class
            {
                Name = dto.ClassName,
                Semester = normalizedSemester,
                SubjectId = dto.SubjectId,
                TeacherId = teacherId,
                Status = 1, // Mặc định là đang mở/hoạt động
                InvitationCodeStatus = 1, // Mặc định cho phép dùng mã mời
                CreatedAtUtc = System.DateTime.UtcNow
            };

            return await _repo.CreateCourseAsync(newClass);
        }

        public async Task JoinCourseAsync(int studentId, string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode)) 
            {
                throw new Exception("Mã mời không thể trống.");
            }

            var course = await _repo.GetClassByInviteCodeAsync(inviteCode);
            if (course == null)
            {
                throw new Exception("Mã mời không chính xác hoặc lớp học đã bị đóng.");
            }

            bool alreadyJoined = await _repo.IsUserInClassAsync(course.ClassId, studentId);
            if (alreadyJoined)
            {
                throw new Exception("Bạn đã ở trong lớp học này rồi.");
            }

            await _repo.JoinClassAsync(course.ClassId, studentId);
        }

        public async Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId)
        {
            return await _repo.GetStudentsInClassAsync(classId);
        }

        public async Task UpdateClassSettingsAsync(int classId, string newName, int invitationStatus)
        {
            if (string.IsNullOrWhiteSpace(newName)) 
                throw new Exception("Tên lớp không được để trống.");

            var success = await _repo.UpdateClassSettingsAsync(classId, newName, invitationStatus);
            if (!success) throw new Exception("Không tìm thấy lớp học.");
        }

        public async Task LeaveCourseAsync(int classId, int userId)
        {
            await _repo.LeaveClassAsync(classId, userId);
        }

        public async Task<string> InviteStudentByEmailAsync(int teacherId, int classId, string studentEmail)
        {
            var trustedFrontendBase = _config["FrontendSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(trustedFrontendBase))
            {
                throw new Exception("Lỗi khi thêm học sinh vào lớp.");
            }

            var user = await _repo.GetUserWithRoleByEmailAsync(studentEmail);
            if (user == null)
            {
                throw new Exception("Học sinh chưa có tài khoản trong hệ thống.");
            }

            if (user.Role?.Name != UserRoles.Student)
            {
                throw new Exception("Chỉ có thể mời người dùng có vai trò là học sinh tham gia lớp học.");
            }

            var course = await _repo.GetByIdAsync(classId);
            if (course == null) throw new Exception("Không tìm thấy lớp học.");

            var existingMembership = await _repo.GetClassMemberAsync(classId, user.UserId);
            if (existingMembership != null)
            {
                if (existingMembership.MemberStatus == MemberStatus.Active)
                {
                    throw new Exception("Học sinh này đã tham gia lớp học.");
                }
                else if (existingMembership.MemberStatus == MemberStatus.Invited)
                {
                    throw new Exception("Học sinh này đã được gửi lời mời trước đó.");
                }
                else if (existingMembership.MemberStatus == MemberStatus.Pending)
                {
                    // Action becomes auto-approval
                    await _repo.UpdateClassMemberStatusAsync(classId, user.UserId, MemberStatus.Active);
                    throw new Backend.Exceptions.AutoApprovePendingException("Học sinh đang ở trạng thái chờ duyệt và đã được phê duyệt thành công.");
                }
            }

            var membership = await _repo.InviteStudentAsync(classId, user.UserId);
            if (membership == null)
            {
                throw new Exception("Không thể tạo lời mời.");
            }

            // Generate token: {classId}:{concurrencyStamp_base64}
            var stampBase64 = Convert.ToBase64String(membership.ConcurrencyStamp);
            // URL safe base64
            stampBase64 = stampBase64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var plainToken = $"{classId}:{stampBase64}";
            var tokenBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainToken));
            var tokenUrlSafe = tokenBase64.Replace("+", "-").Replace("/", "_").TrimEnd('=');

            // Optionally call IEmailService
            var inviteLink = $"{trustedFrontendBase}/Course/AcceptInvite?token={tokenUrlSafe}"; // Fallback dynamic base
            var emailContent = $"<p>Bạn được mời tham gia lớp học <strong>{course.ClassName}</strong>.</p><p><a href=\"{inviteLink}\">Nhấn vào đây để tham gia</a></p>";
            await _emailService.SendEmailAsync(studentEmail, "Thư mời tham gia lớp học", emailContent);

            return tokenUrlSafe;
        }

        public async Task AcceptInvitationAsync(int studentId, string token)
        {
            // Decode URL safe base64
            string base64 = token.Replace("-", "+").Replace("_", "/");
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var plainToken = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = plainToken.Split(':');
            if (parts.Length != 2) throw new Exception("Token không hợp lệ.");

            if (!int.TryParse(parts[0], out int classId)) throw new Exception("Token không hợp lệ.");

            string stampBase64 = parts[1].Replace("-", "+").Replace("_", "/");
            switch (stampBase64.Length % 4)
            {
                case 2: stampBase64 += "=="; break;
                case 3: stampBase64 += "="; break;
            }
            var concurrencyStamp = Convert.FromBase64String(stampBase64);

            var rows = await _repo.AcceptEmailInvitationAsync(classId, studentId, concurrencyStamp);
            if (rows == 0)
            {
                throw new Exception("Link mời không hợp lệ hoặc đã hết hạn.");
            }
        }

        public async Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId)
        {
            return await _repo.GetPendingStudentsAsync(classId);
        }

        public async Task ApproveStudentAsync(int classId, int studentId)
        {
            var success = await _repo.ApproveStudentAsync(classId, studentId);
            if (!success) throw new Exception("Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt.");
        }

        public async Task RejectStudentAsync(int classId, int studentId)
        {
            var success = await _repo.RejectStudentAsync(classId, studentId);
            if (!success) throw new Exception("Học sinh không tồn tại hoặc không ở trạng thái chờ duyệt.");
        }

        public async Task<List<SubjectOptionDto>> GetSubjectsAsync()
        {
            return await _repo.GetSubjectsAsync();
        }
    }
}
