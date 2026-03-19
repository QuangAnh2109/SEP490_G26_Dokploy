using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements
{
    public class ExamBlueprintService : IExamBlueprintService
    {
        private readonly IExamBlueprintRepository _examBlueprintRepository;

        public ExamBlueprintService(IExamBlueprintRepository examBlueprintRepository)
        {
            _examBlueprintRepository = examBlueprintRepository;
        }

        public Task<List<SubjectOptionDto>> GetSubjectsAsync()
        {
            return _examBlueprintRepository.GetSubjectsAsync();
        }

        public async Task<List<ChapterOptionDto>> GetChaptersBySubjectAsync(int subjectId)
        {
            if (subjectId <= 0)
            {
                throw new ExamBlueprintValidationException(new[] { "Môn học không hợp lệ." });
            }

            var subjectExists = await _examBlueprintRepository.SubjectExistsAsync(subjectId);
            if (!subjectExists)
            {
                throw new KeyNotFoundException("Không tìm thấy môn học.");
            }

            return await _examBlueprintRepository.GetChaptersBySubjectAsync(subjectId);
        }

        public async Task<BlueprintListResponseDto> GetBlueprintsAsync(BlueprintListQueryDto query, int currentUserId)
        {
            if (currentUserId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid user.");
            }

            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

            var (items, totalCount) = await _examBlueprintRepository.GetBlueprintsAsync(query, currentUserId);
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

            return new BlueprintListResponseDto
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<BlueprintDetailDto> GetBlueprintDetailAsync(int id, int currentUserId)
        {
            if (id <= 0)
            {
                throw new ExamBlueprintValidationException(new[] { "Id ma trận đề không hợp lệ." });
            }

            var detail = await _examBlueprintRepository.GetBlueprintDetailAsync(id, currentUserId);
            if (detail == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ma trận đề.");
            }

            return detail;
        }

        public async Task<CreateExamBlueprintResponse> CreateBlueprintAsync(int currentUserId, CreateExamBlueprintRequest request)
        {
            if (currentUserId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid user.");
            }

            var errors = new List<string>();
            var warnings = new List<ValidationWarningDto>();

            var name = (request.Name ?? string.Empty).Trim();
            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            var rows = (request.Rows ?? new List<CreateExamBlueprintRowDto>()).ToList();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Tên ma trận đề là bắt buộc.");
            }
            else if (name.Length > 200)
            {
                errors.Add("Tên ma trận đề không được vượt quá 200 ký tự.");
            }

            if (!string.IsNullOrEmpty(description) && description.Length > 1000)
            {
                errors.Add("Mô tả không được vượt quá 1000 ký tự.");
            }

            if (request.SubjectId <= 0)
            {
                errors.Add("Môn học không hợp lệ.");
            }

            if (request.TargetStatus != ExamBlueprintStatus.NotStarted && request.TargetStatus != ExamBlueprintStatus.Approved)
            {
                errors.Add("Trạng thái mục tiêu không hợp lệ.");
            }

            if (request.TargetTotalQuestions < 0)
            {
                errors.Add("Tổng số câu mục tiêu không được âm.");
            }

            if (errors.Count > 0)
            {
                throw new ExamBlueprintValidationException(errors);
            }

            var subjectExists = await _examBlueprintRepository.SubjectExistsAsync(request.SubjectId);
            if (!subjectExists)
            {
                throw new KeyNotFoundException("Không tìm thấy môn học.");
            }

            var chapterOptions = await _examBlueprintRepository.GetChaptersBySubjectAsync(request.SubjectId);
            var chapterNameMap = chapterOptions.ToDictionary(c => c.ChapterId, c => c.Name);
            var availabilityMap = chapterOptions
                .SelectMany(c => c.AvailabilityByDifficulty.Select(a => new { c.ChapterId, a.Difficulty, a.AvailableQuestions }))
                .ToDictionary(x => (x.ChapterId, x.Difficulty), x => x.AvailableQuestions);

            var duplicateSet = new HashSet<(int ChapterId, int Difficulty)>();
            foreach (var row in rows)
            {
                if (row.ChapterId <= 0)
                {
                    errors.Add("Mỗi dòng ma trận phải có chương hợp lệ.");
                    continue;
                }

                if (!chapterNameMap.ContainsKey(row.ChapterId))
                {
                    errors.Add($"Chương {row.ChapterId} không thuộc môn học đã chọn.");
                }

                if (row.Difficulty < 1 || row.Difficulty > 4)
                {
                    errors.Add($"Mức độ {row.Difficulty} không hợp lệ.");
                }

                if (row.TotalQuestions < 0)
                {
                    errors.Add("Số câu trong từng dòng không được âm.");
                }

                if (!duplicateSet.Add((row.ChapterId, row.Difficulty)))
                {
                    errors.Add($"Trùng dòng ma trận cho chương {row.ChapterId} và mức độ {row.Difficulty}.");
                }
            }

            var rowTotal = rows.Sum(r => r.TotalQuestions);

            if (request.TargetStatus == ExamBlueprintStatus.Approved)
            {
                if (rows.Count == 0)
                {
                    errors.Add("Xuất bản yêu cầu ít nhất một dòng ma trận.");
                }

                if (request.TargetTotalQuestions <= 0)
                {
                    errors.Add("Xuất bản yêu cầu tổng số câu mục tiêu lớn hơn 0.");
                }

                if (request.TargetTotalQuestions != rowTotal)
                {
                    errors.Add("Tổng số câu mục tiêu phải bằng tổng số câu của các dòng ma trận.");
                }

                if (rows.Any(r => r.TotalQuestions <= 0))
                {
                    errors.Add("Xuất bản yêu cầu mỗi dòng ma trận có số câu lớn hơn 0.");
                }
            }
            else
            {
                if (request.TargetTotalQuestions != rowTotal)
                {
                    warnings.Add(new ValidationWarningDto
                    {
                        Code = "TARGET_TOTAL_MISMATCH",
                        Message = "Tổng số câu mục tiêu chưa khớp tổng số câu từ các dòng ma trận."
                    });
                }
            }

            foreach (var row in rows)
            {
                if (row.Difficulty < 1 || row.Difficulty > 4 || !chapterNameMap.ContainsKey(row.ChapterId))
                {
                    continue;
                }

                var available = availabilityMap.TryGetValue((row.ChapterId, row.Difficulty), out var count) ? count : 0;
                if (row.TotalQuestions > available)
                {
                    var message = $"Số câu vượt ngân hàng câu hỏi cho '{chapterNameMap[row.ChapterId]}' - {GetDifficultyLabel(row.Difficulty)} (yêu cầu {row.TotalQuestions}, hiện có {available}).";
                    if (request.TargetStatus == ExamBlueprintStatus.Approved)
                    {
                        errors.Add(message);
                    }
                    else
                    {
                        warnings.Add(new ValidationWarningDto
                        {
                            Code = "INSUFFICIENT_QUESTION_BANK",
                            Message = message,
                            ChapterId = row.ChapterId,
                            Difficulty = row.Difficulty
                        });
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new ExamBlueprintValidationException(errors);
            }

            var now = DateTime.UtcNow;
            var blueprint = new ExamBlueprint
            {
                TeacherId = currentUserId,
                SubjectId = request.SubjectId,
                Name = name,
                Description = description,
                Status = request.TargetStatus,
                TotalQuestions = request.TargetTotalQuestions,
                UpdatedAtUtc = now
            };

            var rowEntities = rows.Select(r => new ExamBlueprintChapter
            {
                ChapterId = r.ChapterId,
                Difficulty = r.Difficulty,
                TotalOfQuestions = r.TotalQuestions
            }).ToList();

            var created = await _examBlueprintRepository.CreateBlueprintAsync(blueprint, rowEntities);

            return new CreateExamBlueprintResponse
            {
                ExamBlueprintId = created.ExamBlueprintId,
                Status = created.Status,
                StatusLabel = ExamBlueprintStatus.GetLabel(created.Status),
                UpdatedAtUtc = created.UpdatedAtUtc == default ? now : created.UpdatedAtUtc,
                Message = created.Status == ExamBlueprintStatus.Approved
                    ? "Tạo và xuất bản ma trận đề thành công."
                    : "Lưu nháp ma trận đề thành công.",
                Warnings = warnings
            };
        }

        public async Task<CreateExamBlueprintResponse> UpdateBlueprintAsync(int id, int currentUserId, CreateExamBlueprintRequest request)
        {
            if (id <= 0 || currentUserId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid user or blueprint id.");
            }

            var (errors, warnings, rows) = await ValidateAndPrepareBlueprintAsync(request);
            if (errors.Count > 0)
            {
                throw new ExamBlueprintValidationException(errors);
            }

            var blueprint = new ExamBlueprint
            {
                Name = (request.Name ?? string.Empty).Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                SubjectId = request.SubjectId,
                TotalQuestions = request.TargetTotalQuestions,
                Status = request.TargetStatus
            };

            var rowEntities = rows.Select(r => new ExamBlueprintChapter
            {
                ChapterId = r.ChapterId,
                Difficulty = r.Difficulty,
                TotalOfQuestions = r.TotalQuestions
            }).ToList();

            var updated = await _examBlueprintRepository.UpdateBlueprintAsync(id, currentUserId, blueprint, rowEntities);
            if (updated == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ma trận đề.");
            }

            return new CreateExamBlueprintResponse
            {
                ExamBlueprintId = updated.ExamBlueprintId,
                Status = updated.Status,
                StatusLabel = ExamBlueprintStatus.GetLabel(updated.Status),
                UpdatedAtUtc = updated.UpdatedAtUtc,
                Message = updated.Status == ExamBlueprintStatus.Approved
                    ? "Cập nhật và xuất bản ma trận đề thành công."
                    : "Cập nhật nháp ma trận đề thành công.",
                Warnings = warnings
            };
        }

        private async Task<(List<string> Errors, List<ValidationWarningDto> Warnings, List<CreateExamBlueprintRowDto> Rows)> ValidateAndPrepareBlueprintAsync(CreateExamBlueprintRequest request)
        {
            var errors = new List<string>();
            var warnings = new List<ValidationWarningDto>();
            var rows = (request.Rows ?? new List<CreateExamBlueprintRowDto>()).ToList();

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Tên ma trận đề là bắt buộc.");
            else if (name.Length > 200)
                errors.Add("Tên ma trận đề không được vượt quá 200 ký tự.");

            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            if (!string.IsNullOrEmpty(description) && description.Length > 1000)
                errors.Add("Mô tả không được vượt quá 1000 ký tự.");

            if (request.SubjectId <= 0)
                errors.Add("Môn học không hợp lệ.");

            if (request.TargetStatus != ExamBlueprintStatus.NotStarted && request.TargetStatus != ExamBlueprintStatus.Approved)
                errors.Add("Trạng thái mục tiêu không hợp lệ.");

            if (request.TargetTotalQuestions < 0)
                errors.Add("Tổng số câu mục tiêu không được âm.");

            var subjectExists = await _examBlueprintRepository.SubjectExistsAsync(request.SubjectId);
            if (!subjectExists)
                throw new KeyNotFoundException("Không tìm thấy môn học.");

            var chapterOptions = await _examBlueprintRepository.GetChaptersBySubjectAsync(request.SubjectId);
            var chapterNameMap = chapterOptions.ToDictionary(c => c.ChapterId, c => c.Name);
            var availabilityMap = chapterOptions
                .SelectMany(c => c.AvailabilityByDifficulty.Select(a => new { c.ChapterId, a.Difficulty, a.AvailableQuestions }))
                .ToDictionary(x => (x.ChapterId, x.Difficulty), x => x.AvailableQuestions);

            var duplicateSet = new HashSet<(int ChapterId, int Difficulty)>();
            foreach (var row in rows)
            {
                if (row.ChapterId <= 0) { errors.Add("Mỗi dòng ma trận phải có chương hợp lệ."); continue; }
                if (!chapterNameMap.ContainsKey(row.ChapterId))
                    errors.Add($"Chương {row.ChapterId} không thuộc môn học đã chọn.");
                if (row.Difficulty < 1 || row.Difficulty > 4)
                    errors.Add($"Mức độ {row.Difficulty} không hợp lệ.");
                if (row.TotalQuestions < 0)
                    errors.Add("Số câu trong từng dòng không được âm.");
                if (!duplicateSet.Add((row.ChapterId, row.Difficulty)))
                    errors.Add($"Trùng dòng ma trận cho chương {row.ChapterId} và mức độ {row.Difficulty}.");
            }

            var rowTotal = rows.Sum(r => r.TotalQuestions);

            if (request.TargetStatus == ExamBlueprintStatus.Approved)
            {
                if (rows.Count == 0) errors.Add("Xuất bản yêu cầu ít nhất một dòng ma trận.");
                if (request.TargetTotalQuestions <= 0) errors.Add("Xuất bản yêu cầu tổng số câu mục tiêu lớn hơn 0.");
                if (request.TargetTotalQuestions != rowTotal) errors.Add("Tổng số câu mục tiêu phải bằng tổng số câu của các dòng ma trận.");
                if (rows.Any(r => r.TotalQuestions <= 0)) errors.Add("Xuất bản yêu cầu mỗi dòng ma trận có số câu lớn hơn 0.");
            }
            else
            {
                if (request.TargetTotalQuestions != rowTotal)
                    warnings.Add(new ValidationWarningDto { Code = "TARGET_TOTAL_MISMATCH", Message = "Tổng số câu mục tiêu chưa khớp tổng số câu từ các dòng ma trận." });
            }

            foreach (var row in rows)
            {
                if (row.Difficulty < 1 || row.Difficulty > 4 || !chapterNameMap.ContainsKey(row.ChapterId)) continue;
                var available = availabilityMap.TryGetValue((row.ChapterId, row.Difficulty), out var count) ? count : 0;
                if (row.TotalQuestions > available)
                {
                    var message = $"Số câu vượt ngân hàng câu hỏi cho '{chapterNameMap[row.ChapterId]}' - {GetDifficultyLabel(row.Difficulty)} (yêu cầu {row.TotalQuestions}, hiện có {available}).";
                    if (request.TargetStatus == ExamBlueprintStatus.Approved)
                        errors.Add(message);
                    else
                        warnings.Add(new ValidationWarningDto { Code = "INSUFFICIENT_QUESTION_BANK", Message = message, ChapterId = row.ChapterId, Difficulty = row.Difficulty });
                }
            }

            return (errors, warnings, rows);
        }

        public async Task<int> UpdateBlueprintStatusAsync(IEnumerable<int> examBlueprintIds, int currentUserId, int status)
        {
            if (status != ExamBlueprintStatus.Archived)
            {
                throw new ExamBlueprintValidationException(new[] { "Chỉ hỗ trợ chuyển trạng thái sang Lưu trữ." });
            }

            var ids = examBlueprintIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return 0;

            return await _examBlueprintRepository.UpdateBlueprintStatusAsync(ids, currentUserId, status);
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
