using Backend.DTOs.Class;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for chapter business logic.
    /// </summary>
    public interface IChapterService
    {
        Task<List<ChapterDTO>> GetAllAsync();
        Task<ChapterDTO?> GetByIdAsync(int chapterId);
        Task<List<ChapterDTO>> GetBySubjectIdAsync(int subjectId);
    }
}
