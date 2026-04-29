using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Repositories.Implements
{
    /// <summary>
    /// Concrete repository implementation for chapters.
    /// </summary>
    public class ChapterRepository : IChapterRepository
    {
        private readonly MtcaSep490G26Context _context;

        public ChapterRepository(MtcaSep490G26Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Return all chapters mapped to DTOs.
        /// </summary>
        public async Task<List<ChapterDTO>> GetAllAsync()
        {
            return await _context.Chapters
                .AsNoTracking()
                .Select(c => new ChapterDTO
                {
                    ChapterId = c.ChapterId,
                    SubjectId = c.SubjectId,
                    Name = c.Name
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Return a single chapter by id, or null if not found.
        /// </summary>
        public async Task<ChapterDTO?> GetByIdAsync(int chapterId)
        {
            var chapter = await _context.Chapters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ChapterId == chapterId);

            if (chapter == null) return null;

            return new ChapterDTO
            {
                ChapterId = chapter.ChapterId,
                SubjectId = chapter.SubjectId,
                Name = chapter.Name
            };
        }
        public async Task<List<ChapterDTO>> GetBySubjectIdAsync(int subjectId)
        {
            return await _context.Chapters
            .AsNoTracking()
            .Where(c => c.SubjectId == subjectId)
            .Select(c => new ChapterDTO
            {
                ChapterId = c.ChapterId,
                SubjectId = c.SubjectId,
                Name = c.Name
            })
            .OrderBy(c => c.Name)
            .ToListAsync();
        }
    }
}
