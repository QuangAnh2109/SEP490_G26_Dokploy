using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements
{
    public class ExamBlueprintService(IExamBlueprintRepository examBlueprintRepository, ICurrentUserService currentUserService, TimeProvider timeProvider) : IExamBlueprintService
    {
        private readonly IExamBlueprintRepository _examBlueprintRepository = examBlueprintRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<Result<List<SubjectOptionDto>>> GetSubjectsAsync()
        {
            var result = await _examBlueprintRepository.GetSubjectsAsync();
            return Result<List<SubjectOptionDto>>.Success(result);
        }

        public async Task<Result<List<ChapterOptionDto>>> GetChaptersBySubjectAsync(int subjectId)
        {
            var subjectExists = await _examBlueprintRepository.SubjectExistsAsync(subjectId);
            if (!subjectExists)
            {
                return ExamBlueprintErrors.SubjectNotFound;
            }

            var result = await _examBlueprintRepository.GetChaptersBySubjectAsync(subjectId);
            return Result<List<ChapterOptionDto>>.Success(result);
        }

        public async Task<Result<BlueprintListResponseDto>> GetBlueprintsAsync(BlueprintListQueryDto query)
        {
            var userId = _currentUserService.UserId;

            query.Page = query.Page ?? 1;
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize ?? 10;
            query.PageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize.Value, 100);

            var (items, totalCount) = await _examBlueprintRepository.GetBlueprintsAsync(query, userId);
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize.Value);

            return Result<BlueprintListResponseDto>.Success(new BlueprintListResponseDto
            {
                Items = items,
                Page = query.Page.Value,
                PageSize = query.PageSize.Value,
                TotalItems = totalCount,
                TotalPages = totalPages
            });
        }

        public async Task<Result<BlueprintDetailDto>> GetBlueprintDetailAsync(int id)
        {
            var userId = _currentUserService.UserId;

            var detail = await _examBlueprintRepository.GetBlueprintDetailAsync(id, userId);
            if (detail == null)
            {
                return ExamBlueprintErrors.NotFound;
            }

            return Result<BlueprintDetailDto>.Success(detail);
        }

        public async Task<Result<CreateExamBlueprintResponse>> CreateBlueprintAsync(CreateExamBlueprintRequest request)
        {
            var userId = _currentUserService.UserId;
            
            var validateResult = await ValidateAndPrepareBlueprintAsync(request);
            if (validateResult.IsFailure)
            {
                return Result<CreateExamBlueprintResponse>.Failure(validateResult.Error);
            }

            var (warnings, rows) = validateResult.Value;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var blueprint = new ExamBlueprint
            {
                TeacherId = userId,
                SubjectId = request.SubjectId!.Value,
                Name = request.Name!.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = request.TargetStatus!.Value,
                TotalQuestions = request.TargetTotalQuestions!.Value,
                UpdatedAtUtc = now
            };

            var rowEntities = rows.Select(r => new ExamBlueprintChapter
            {
                ChapterId = r.ChapterId!.Value,
                Difficulty = r.Difficulty!.Value,
                TotalOfQuestions = r.TotalQuestions!.Value
            }).ToList();

            var created = await _examBlueprintRepository.CreateBlueprintAsync(blueprint, rowEntities);

            return Result<CreateExamBlueprintResponse>.Success(new CreateExamBlueprintResponse
            {
                ExamBlueprintId = created.ExamBlueprintId,
                Status = created.Status,
                StatusLabel = ExamBlueprintStatus.GetLabel(created.Status),
                UpdatedAtUtc = created.UpdatedAtUtc == default ? now : created.UpdatedAtUtc,
                Message = created.Status == ExamBlueprintStatus.Active
                    ? "Tạo và xuất bản ma trận đề thành công."
                    : "Lưu nháp ma trận đề thành công.",
                Warnings = warnings
            });
        }

        public async Task<Result<CreateExamBlueprintResponse>> UpdateBlueprintAsync(int id, CreateExamBlueprintRequest request)
        {
            var userId = _currentUserService.UserId;

            var validateResult = await ValidateAndPrepareBlueprintAsync(request);
            if (validateResult.IsFailure)
            {
                return Result<CreateExamBlueprintResponse>.Failure(validateResult.Error);
            }

            var (warnings, rows) = validateResult.Value;

            var existing = await _examBlueprintRepository.GetBlueprintDetailAsync(id, userId);
            if (existing == null)
            {
                return ExamBlueprintErrors.NotFound;
            }

            var isUsed = await _examBlueprintRepository.IsBlueprintUsedAsync(id);

            var blueprint = new ExamBlueprint
            {
                Name = request.Name!.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                SubjectId = request.SubjectId!.Value,
                TotalQuestions = request.TargetTotalQuestions!.Value,
                Status = request.TargetStatus!.Value
            };

            var rowEntities = rows.Select(r => new ExamBlueprintChapter
            {
                ChapterId = r.ChapterId!.Value,
                Difficulty = r.Difficulty!.Value,
                TotalOfQuestions = r.TotalQuestions!.Value
            }).ToList();

            ExamBlueprint updated;

            if (existing.Status == ExamBlueprintStatus.Inprogress || existing.Status == ExamBlueprintStatus.Archived || isUsed)
            {
                await _examBlueprintRepository.UpdateBlueprintStatusAsync(new List<int> { id }, userId, ExamBlueprintStatus.Archived);
                blueprint.TeacherId = userId;
                blueprint.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                updated = await _examBlueprintRepository.CreateBlueprintAsync(blueprint, rowEntities);
            }
            else
            {
                updated = await _examBlueprintRepository.UpdateBlueprintAsync(id, userId, blueprint, rowEntities);
                if (updated == null)
                {
                    return ExamBlueprintErrors.NotFound;
                }
            }

            return Result<CreateExamBlueprintResponse>.Success(new CreateExamBlueprintResponse
            {
                ExamBlueprintId = updated.ExamBlueprintId,
                Status = updated.Status,
                StatusLabel = ExamBlueprintStatus.GetLabel(updated.Status),
                UpdatedAtUtc = updated.UpdatedAtUtc,
                Message = updated.Status == ExamBlueprintStatus.Active
                    ? "Cập nhật và xuất bản ma trận đề thành công."
                    : "Cập nhật nháp ma trận đề thành công.",
                Warnings = warnings
            });
        }

        private async Task<Result<(List<ValidationWarningDto> Warnings, List<CreateExamBlueprintRowDto> Rows)>> ValidateAndPrepareBlueprintAsync(CreateExamBlueprintRequest request)
        {
            var warnings = new List<ValidationWarningDto>();
            var rows = request.Rows ?? new List<CreateExamBlueprintRowDto>();

            var subjectExists = await _examBlueprintRepository.SubjectExistsAsync(request.SubjectId!.Value);
            if (!subjectExists)
            {
                return ExamBlueprintErrors.SubjectNotFound;
            }

            var chapterOptions = await _examBlueprintRepository.GetChaptersBySubjectAsync(request.SubjectId.Value);
            var chapterNameMap = chapterOptions.ToDictionary(c => c.ChapterId, c => c.Name);
            var availabilityMap = chapterOptions
                .SelectMany(c => c.AvailabilityByDifficulty.Select(a => new { c.ChapterId, a.Difficulty, a.AvailableQuestions }))
                .ToDictionary(x => (x.ChapterId, x.Difficulty), x => x.AvailableQuestions);

            var duplicateSet = new HashSet<(int ChapterId, int Difficulty)>();
            foreach (var row in rows)
            {
                if (!chapterNameMap.ContainsKey(row.ChapterId!.Value))
                {
                    return ExamBlueprintErrors.ValidationFailed;
                }

                if (!duplicateSet.Add((row.ChapterId.Value, row.Difficulty!.Value)))
                {
                    return ExamBlueprintErrors.DuplicateRow;
                }
            }

            var rowTotal = rows.Sum(r => r.TotalQuestions!.Value);

            if (request.TargetStatus == ExamBlueprintStatus.Active)
            {
                if (rows.Count == 0) return ExamBlueprintErrors.EmptyRows;
                if (request.TargetTotalQuestions != rowTotal) return ExamBlueprintErrors.TargetTotalMismatch;
            }
            else
            {
                if (request.TargetTotalQuestions != rowTotal)
                {
                    warnings.Add(new ValidationWarningDto { Code = "TARGET_TOTAL_MISMATCH", Message = "Tổng số câu mục tiêu chưa khớp tổng số câu từ các dòng ma trận." });
                }
            }

            foreach (var row in rows)
            {
                if (!chapterNameMap.ContainsKey(row.ChapterId!.Value)) continue;
                
                var available = availabilityMap.TryGetValue((row.ChapterId.Value, row.Difficulty!.Value), out var count) ? count : 0;
                if (row.TotalQuestions!.Value > available)
                {
                    var message = $"Số câu vượt ngân hàng câu hỏi cho '{chapterNameMap[row.ChapterId.Value]}' - {GetDifficultyLabel(row.Difficulty.Value)} (yêu cầu {row.TotalQuestions.Value}, hiện có {available}).";
                    if (request.TargetStatus == ExamBlueprintStatus.Active)
                    {
                        return ExamBlueprintErrors.InsufficientQuestionBank;
                    }
                    else
                    {
                        warnings.Add(new ValidationWarningDto { Code = "INSUFFICIENT_QUESTION_BANK", Message = message, ChapterId = row.ChapterId.Value, Difficulty = row.Difficulty.Value });
                    }
                }
            }

            return Result<(List<ValidationWarningDto>, List<CreateExamBlueprintRowDto>)>.Success((warnings, rows));
        }

        public async Task<Result<int>> UpdateBlueprintStatusAsync(IEnumerable<int> examBlueprintIds, int status)
        {
            var userId = _currentUserService.UserId;

            if (status != ExamBlueprintStatus.Archived)
            {
                return ExamBlueprintErrors.InvalidUpdateStatus;
            }

            var ids = examBlueprintIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return Result<int>.Success(0);

            var result = await _examBlueprintRepository.UpdateBlueprintStatusAsync(ids, userId, status);
            return Result<int>.Success(result);
        }

        public async Task<Result> DeleteBlueprintAsync(int id)
        {
            var userId = _currentUserService.UserId;

            var existing = await _examBlueprintRepository.GetBlueprintDetailAsync(id, userId);
            if (existing == null)
            {
                return ExamBlueprintErrors.NotFound;
            }

            if (existing.Status != ExamBlueprintStatus.Draft && existing.Status != ExamBlueprintStatus.Active)
            {
                return ExamBlueprintErrors.CannotDelete;
            }

            if (existing.Status == ExamBlueprintStatus.Active)
            {
                var isUsed = await _examBlueprintRepository.IsBlueprintUsedAsync(id);
                if (isUsed)
                {
                    return ExamBlueprintErrors.InUse;
                }
            }

            await _examBlueprintRepository.DeleteBlueprintAsync(id, userId);
            return Result.Success();
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
