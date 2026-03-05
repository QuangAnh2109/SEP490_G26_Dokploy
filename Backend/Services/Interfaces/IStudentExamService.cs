using Backend.DTOs.StudentExam;
using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface IStudentExamService
    {
        Task<TakeExamDto?> TakeExamInClass(int examId, int studentId);

    }
}
