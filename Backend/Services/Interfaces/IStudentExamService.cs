using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;

namespace Backend.Services.Interfaces
{
    public interface IStudentExamService
    {
        Task<TakeExamDto?> TakeExamInClass(int examId, int studentId);
        Task<ExamPreviewDto?> GetExamPreviewAsync(int studentId, int examId);
    }
}
