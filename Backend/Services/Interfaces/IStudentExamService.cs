using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs.StudentExam;

namespace Backend.Services.Interfaces;

public interface IStudentExamService
{
    Task<Result<TakeExamDto>> TakeExamInClass(int examId);
    Task<Result<ExamPreviewDto>> GetExamPreviewAsync(int examId);

    /// <summary>
    /// Lấy lịch sử bài nộp tổng hợp (cả kiểm tra + luyện tập) từ tất cả khóa học.
    /// </summary>
    Task<Result<List<StudentSubmissionHistoryDto>>> GetAllSubmissionHistoryAsync(int? classId = null);
}
