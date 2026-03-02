using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Implements
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepo _repo;

        public CourseService(ICourseRepo repo)
        {
            _repo = repo;
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
        }

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
            var normalizedSemester = dto.Semester?.Trim().ToUpper();

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
            var course = await _repo.GetClassByInviteCodeAsync(inviteCode);
            if (course == null)
            {
                throw new System.Exception("Mã mời không chính xác hoặc lớp học đã bị đóng.");
            }

            bool alreadyJoined = await _repo.IsUserInClassAsync(course.ClassId, studentId);
            if (alreadyJoined)
            {
                throw new System.Exception("Bạn đã ở trong lớp học này rồi.");
            }

            await _repo.JoinClassAsync(course.ClassId, studentId);
        }

        public async Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId)
        {
            return await _repo.GetStudentsInClassAsync(classId);
        }

        public async Task<bool> UpdateClassSettingsAsync(int classId, string newName, int invitationStatus)
        {
            return await _repo.UpdateClassSettingsAsync(classId, newName, invitationStatus);
        }
    }
}
