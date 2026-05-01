using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class ExamBlueprintRepository : IExamBlueprintRepository
    {
        private readonly MtcaSep490G26Context _context;
        private readonly TimeProvider _timeProvider;

        public ExamBlueprintRepository(MtcaSep490G26Context context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<List<SubjectOptionDto>> GetSubjectsAsync()
        {
            return await _context.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Code)
                .ThenBy(s => s.Name)
                .Select(s => new SubjectOptionDto
                {
                    SubjectId = s.SubjectId,
                    Name = s.Name,
                    Code = s.Code
                })
                .ToListAsync();
        }

        public Task<bool> SubjectExistsAsync(int subjectId)
        {
            return _context.Subjects.AsNoTracking().AnyAsync(s => s.SubjectId == subjectId);
        }

        public async Task<List<ChapterOptionDto>> GetChaptersBySubjectAsync(int subjectId)
        {
            var chapters = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.SubjectId == subjectId)
                .OrderBy(c => c.ChapterId)
                .Select(c => new { c.ChapterId, c.Name })
                .ToListAsync();

            var chapterIds = chapters.Select(c => c.ChapterId).ToList();
            var counts = new List<(int ChapterId, int Difficulty, int Count)>();

            if (chapterIds.Count > 0)
            {
                var rawCounts = await _context.Questions
                    .AsNoTracking()
                    .Where(q => chapterIds.Contains(q.ChapterId) && q.Status == "Active")
                    .GroupBy(q => new { q.ChapterId, q.Difficulty })
                    .Select(g => new
                    {
                        g.Key.ChapterId,
                        g.Key.Difficulty,
                        Count = g.Count()
                    })
                    .ToListAsync();

                counts = rawCounts
                    .Select(x => (x.ChapterId, x.Difficulty, x.Count))
                    .ToList();
            }

            var countMap = counts.ToDictionary(
                x => (x.ChapterId, x.Difficulty),
                x => x.Count);

            return chapters.Select(c => new ChapterOptionDto
            {
                ChapterId = c.ChapterId,
                Name = c.Name,
                AvailabilityByDifficulty = Enumerable.Range(1, 4)
                    .Select(d => new ChapterAvailabilityDto
                    {
                        Difficulty = d,
                        AvailableQuestions = countMap.TryGetValue((c.ChapterId, d), out var count) ? count : 0
                    })
                    .ToList()
            }).ToList();
        }

        public async Task<(List<BlueprintListItemDto> Items, int TotalCount)> GetBlueprintsAsync(BlueprintListQueryDto query, int currentUserId)
        {
            var page = query.Page ?? 1;
            page = page < 1 ? 1 : page;
            var pageSize = query.PageSize ?? 10;
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var blueprints = _context.ExamBlueprints
                .AsNoTracking()
                .Include(b => b.Subject)
                .AsQueryable();

            blueprints = blueprints.Where(b => b.TeacherId == currentUserId);

            if (query.SubjectId.HasValue && query.SubjectId.Value > 0)
            {
                blueprints = blueprints.Where(b => b.SubjectId == query.SubjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                blueprints = blueprints.Where(b => b.Name.Contains(keyword));
            }

            var totalCount = await blueprints.CountAsync();

            var rawItems = await blueprints
                .OrderByDescending(b => b.UpdatedAtUtc)
                .ThenByDescending(b => b.ExamBlueprintId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    ExamBlueprintId = b.ExamBlueprintId,
                    Name = b.Name,
                    TotalQuestions = b.TotalQuestions,
                    SubjectId = b.SubjectId,
                    SubjectName = b.Subject.Name,
                    SubjectCode = b.Subject.Code,
                    Status = b.Status,
                    UpdatedAtUtc = b.UpdatedAtUtc
                })
                .ToListAsync();

            var items = rawItems.Select(b => new BlueprintListItemDto
            {
                ExamBlueprintId = b.ExamBlueprintId,
                Name = b.Name,
                TotalQuestions = b.TotalQuestions,
                SubjectId = b.SubjectId,
                SubjectName = b.SubjectName,
                SubjectCode = b.SubjectCode,
                Status = b.Status,
                StatusLabel = ExamBlueprintStatus.GetLabel(b.Status),
                UpdatedAtUtc = b.UpdatedAtUtc
            }).ToList();

            return (items, totalCount);
        }

        public async Task<BlueprintDetailDto?> GetBlueprintDetailAsync(int id, int currentUserId)
        {
            var query = _context.ExamBlueprints
                .AsNoTracking()
                .Include(b => b.Subject)
                .Include(b => b.ExamBlueprintChapters)
                    .ThenInclude(r => r.Chapter)
                .Where(b => b.ExamBlueprintId == id);

            query = query.Where(b => b.TeacherId == currentUserId);

            var entity = await query.FirstOrDefaultAsync();
            if (entity == null)
            {
                return null;
            }

            return new BlueprintDetailDto
            {
                ExamBlueprintId = entity.ExamBlueprintId,
                Name = entity.Name,
                Description = entity.Description,
                TotalQuestions = entity.TotalQuestions,
                SubjectId = entity.SubjectId,
                SubjectName = entity.Subject.Name,
                SubjectCode = entity.Subject.Code,
                Status = entity.Status,
                StatusLabel = ExamBlueprintStatus.GetLabel(entity.Status),
                UpdatedAtUtc = entity.UpdatedAtUtc,
                Rows = entity.ExamBlueprintChapters
                    .OrderBy(r => r.Chapter.Name)
                    .ThenBy(r => r.Difficulty)
                    .Select(r => new BlueprintDetailRowDto
                    {
                        ChapterId = r.ChapterId,
                        ChapterName = r.Chapter.Name,
                        Difficulty = r.Difficulty,
                        DifficultyLabel = GetDifficultyLabel(r.Difficulty),
                        TotalQuestions = r.TotalOfQuestions
                    })
                    .ToList()
            };
        }

        public async Task<ExamBlueprint> CreateBlueprintAsync(ExamBlueprint blueprint, IEnumerable<ExamBlueprintChapter> rows)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.ExamBlueprints.Add(blueprint);
            await _context.SaveChangesAsync();

            var rowEntities = rows.ToList();
            foreach (var row in rowEntities)
            {
                row.ExamBlueprintId = blueprint.ExamBlueprintId;
            }

            if (rowEntities.Count > 0)
            {
                _context.ExamBlueprintChapters.AddRange(rowEntities);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return blueprint;
        }

        public async Task<bool> IsBlueprintUsedAsync(int blueprintId)
        {
            return await _context.Exams.AnyAsync(e => e.ExamBlueprintId == blueprintId);
        }

        public async Task<ExamBlueprint?> UpdateBlueprintAsync(int id, int currentUserId, ExamBlueprint blueprint, IEnumerable<ExamBlueprintChapter> rows)
        {
            var entity = await _context.ExamBlueprints
                .Include(b => b.ExamBlueprintChapters)
                .FirstOrDefaultAsync(b => b.ExamBlueprintId == id && b.TeacherId == currentUserId);

            if (entity == null) return null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            entity.Name = blueprint.Name;
            entity.Description = blueprint.Description;
            entity.SubjectId = blueprint.SubjectId;
            entity.TotalQuestions = blueprint.TotalQuestions;
            entity.Status = blueprint.Status;
            entity.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            _context.ExamBlueprintChapters.RemoveRange(entity.ExamBlueprintChapters);

            var rowEntities = rows.Select(r => new ExamBlueprintChapter
            {
                ExamBlueprintId = id,
                ChapterId = r.ChapterId,
                Difficulty = r.Difficulty,
                TotalOfQuestions = r.TotalOfQuestions
            }).ToList();

            if (rowEntities.Count > 0)
            {
                await _context.ExamBlueprintChapters.AddRangeAsync(rowEntities);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return entity;
        }

        public async Task<int> UpdateBlueprintStatusAsync(IEnumerable<int> examBlueprintIds, int currentUserId, int status)
        {
            var ids = examBlueprintIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return 0;

            var entities = await _context.ExamBlueprints
                .Where(b => ids.Contains(b.ExamBlueprintId) && b.TeacherId == currentUserId)
                .ToListAsync();

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var e in entities)
            {
                e.Status = status;
                e.UpdatedAtUtc = now;
            }

            await _context.SaveChangesAsync();
            return entities.Count;
        }

        public async Task DeleteBlueprintAsync(int blueprintId, int currentUserId)
        {
            var entity = await _context.ExamBlueprints
                .Include(b => b.ExamBlueprintChapters)
                .FirstOrDefaultAsync(b => b.ExamBlueprintId == blueprintId && b.TeacherId == currentUserId);

            if (entity != null)
            {
                _context.ExamBlueprintChapters.RemoveRange(entity.ExamBlueprintChapters);
                _context.ExamBlueprints.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        private static string GetDifficultyLabel(int difficulty)
        {
            return difficulty switch
            {
                1 => "Nhận biết",
                2 => "Thông hiểu",
                3 => "Vận dụng",
                4 => "Vận dụng cao",
                _ => difficulty.ToString()
            };
        }
    }
}
