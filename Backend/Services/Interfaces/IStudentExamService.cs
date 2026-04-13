using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;

namespace Backend.Services.Interfaces
{
    public interface IStudentExamService
    {
        Task<TakeExamDto?> TakeExamInClass(int examId, int studentId);
        Task<ExamPreviewDto?> GetExamPreviewAsync(int userId, int examId, bool isTeacher = false);

        /// <summary>
        /// Lấy lịch sử bài nộp tổng hợp (cả kiểm tra + luyện tập) từ tất cả khóa học.
        /// </summary>
        Task<List<StudentSubmissionHistoryDto>> GetAllSubmissionHistoryAsync(int studentId, int? classId = null);
    }
}
