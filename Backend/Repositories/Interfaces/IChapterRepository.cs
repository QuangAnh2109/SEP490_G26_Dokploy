using Backend.DTOs.Course;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for accessing chapter data.
    /// </summary>
    public interface IChapterRepository
    {
        Task<List<ChapterDTO>> GetAllAsync();
        Task<ChapterDTO?> GetByIdAsync(int chapterId);
        Task<List<ChapterDTO>> GetBySubjectIdAsync(int subjectId);
    }
}