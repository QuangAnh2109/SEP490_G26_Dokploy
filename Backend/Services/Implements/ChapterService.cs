using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Implements
{
    /// <summary>
    /// Minimal service implementation that delegates to repository.
    /// </summary>
    public class ChapterService(IChapterRepository repo) : IChapterService
    {
        private readonly IChapterRepository _repo = repo;

        public Task<List<ChapterDTO>> GetAllAsync() => _repo.GetAllAsync();

        public Task<ChapterDTO?> GetByIdAsync(int chapterId) => _repo.GetByIdAsync(chapterId);
        public async Task<List<ChapterDTO>> GetBySubjectIdAsync(int subjectId)
        {
            return await _repo.GetBySubjectIdAsync(subjectId);
        }
    }
}
