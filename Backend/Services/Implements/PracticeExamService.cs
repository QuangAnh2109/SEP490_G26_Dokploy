using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.PracticeExam;
using Backend.Common;
using Backend.Common.Models;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Backend.Helpers;

namespace Backend.Services.Implements
{
    public class PracticeExamService(
        IPracticeExamRepository repo,
        ICurrentUserService currentUserService,
        ILogger<PracticeExamService> logger,
        TimeProvider timeProvider,
        IGradingService gradingService) : IPracticeExamService
    {
        private readonly IPracticeExamRepository _repo = repo;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly ILogger<PracticeExamService> _logger = logger;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly IGradingService _gradingService = gradingService;

        private const int MinQuestions = 5;
        private const int MaxQuestions = 30;

        // Ngưỡng phân loại proficiency
        private const double WeakThreshold = 50.0;
        private const double StrongThreshold = 80.0;

        // ════════════════════════════════════════════════════════
        //  LẤY DANH SÁCH CHƯƠNG + PROFICIENCY
        // ════════════════════════════════════════════════════════
        public async Task<Result<List<ChapterProficiencyDto>>> GetChaptersForPracticeAsync(int classId)
        {
            var studentId = _currentUserService.UserId;
            var cls = await _repo.GetClassWithValidationAsync(classId, studentId);
            if (cls == null)
                return PracticeExamErrors.ClassNotFound;

            var chapters = await _repo.GetChaptersBySubjectIdAsync(cls.SubjectId);
            if (chapters.Count == 0)
                return new List<ChapterProficiencyDto>();

            var chapterIds = chapters.Select(c => c.ChapterId).ToList();
            var profData = await _repo.GetStudentProficiencyAsync(studentId, chapterIds);
            var counts = await _repo.GetPracticeQuestionCountsAsync(chapterIds, cls.TeacherId);

            var countsByChapterDiff = counts.ToDictionary(c => (c.ChapterId, c.Difficulty), c => c.Count);

            var result = new List<ChapterProficiencyDto>();
            foreach (var chapter in chapters)
            {
                var chapterProf = profData.Where(p => p.ChapterId == chapter.ChapterId).ToList();

                var diffBreakdown = new List<DifficultyProficiencyDto>();
                int availableCount = 0;
                for (int diff = 1; diff <= 4; diff++)
                {
                    var slot = chapterProf.FirstOrDefault(p => p.Difficulty == diff);
                    var diffAvailable = countsByChapterDiff.TryGetValue((chapter.ChapterId, diff), out var n) ? n : 0;
                    availableCount += diffAvailable;
                    diffBreakdown.Add(new DifficultyProficiencyDto
                    {
                        Difficulty = diff,
                        DifficultyName = DifficultyLevel.GetLabel(diff),
                        TotalAttempted = slot?.TotalAttempted ?? 0,
                        CorrectCount = slot?.CorrectCount ?? 0,
                        AvailableQuestions = diffAvailable
                    });
                }

                var totalAttempted = chapterProf.Sum(p => p.TotalAttempted);
                var totalCorrect = chapterProf.Sum(p => p.CorrectCount);

                result.Add(new ChapterProficiencyDto
                {
                    ChapterId = chapter.ChapterId,
                    ChapterName = chapter.Name,
                    DifficultyBreakdown = diffBreakdown,
                    OverallAccuracyRate = totalAttempted > 0
                        ? Math.Round((double)totalCorrect / totalAttempted * 100, 1) : 0,
                    TotalAttempted = totalAttempted,
                    AvailableQuestions = availableCount
                });
            }

            return result;
        }

        // ════════════════════════════════════════════════════════
        //  TẠO ĐỀ LUYỆN TẬP TỰ ĐỘNG
        // ════════════════════════════════════════════════════════
        public async Task<Result<CreatePracticeExamResponse>> CreatePracticeExamAsync(CreatePracticeExamRequest request)
        {
            var studentId = _currentUserService.UserId;
            
            // ── 1. Validate ──
            var cls = await _repo.GetClassWithValidationAsync(request.ClassId!.Value, studentId);
            if (cls == null)
                return PracticeExamErrors.ClassNotFound;

            if (request.ChapterIds == null || request.ChapterIds.Count == 0)
                return PracticeExamErrors.ChapterRequired;

            if (request.TotalQuestions < MinQuestions || request.TotalQuestions > MaxQuestions)
                return PracticeExamErrors.InvalidQuestionCount;

            // Validate chapters thuộc subject
            var chapters = await _repo.GetChaptersBySubjectIdAsync(cls.SubjectId);
            var validChapterIds = chapters.Select(c => c.ChapterId).ToHashSet();
            foreach (var chId in request.ChapterIds)
            {
                if (!validChapterIds.Contains(chId))
                    return PracticeExamErrors.ChapterNotBelongToSubject;
            }

            // ── 2. Lấy proficiency data ──
            var profData = await _repo.GetStudentProficiencyAsync(studentId, request.ChapterIds);

            // ── 3. Auto-allocate difficulty based on proficiency ──
            int totalQuestions = request.TotalQuestions.Value;
            var difficultyQuotas = ComputeDifficultyQuotas(profData, totalQuestions);

            // ── 4. Spaced Repetition per difficulty slot ──
            var itemHistory = await _repo.GetStudentItemLevelHistoryAsync(studentId, request.ChapterIds);
            var masteredIds = itemHistory.Where(x => x.IsMastered).Select(x => x.QuestionId).ToHashSet();
            var wrongIds = itemHistory.Where(x => !x.IsMastered).Select(x => x.QuestionId).ToHashSet();

            var selectedQuestionIds = new List<int>();

            foreach (var quota in difficultyQuotas)
            {
                if (quota.Value <= 0) continue;

                var diffFilter = new List<int> { quota.Key };
                var poolIds = await _repo.GetAllPracticeQuestionIdsAsync(request.ChapterIds, cls.TeacherId, diffFilter);
                if (poolIds.Count == 0) continue;

                // Phân loại theo spaced repetition
                var wrongPool = poolIds.Where(id => wrongIds.Contains(id)).OrderBy(_ => Guid.NewGuid()).ToList();
                var unseenPool = poolIds.Where(id => !masteredIds.Contains(id) && !wrongIds.Contains(id)).OrderBy(_ => Guid.NewGuid()).ToList();
                var masteredPool = poolIds.Where(id => masteredIds.Contains(id)).OrderBy(_ => Guid.NewGuid()).ToList();

                int slotTarget = Math.Min(quota.Value, poolIds.Count);
                int wrongQuota = (int)Math.Ceiling(slotTarget * 0.6);
                int unseenQuota = slotTarget - wrongQuota;

                var slotSelected = new List<int>();

                // Nhặt từ Wrong
                if (wrongPool.Count >= wrongQuota)
                    slotSelected.AddRange(wrongPool.Take(wrongQuota));
                else
                {
                    slotSelected.AddRange(wrongPool);
                    unseenQuota += (wrongQuota - wrongPool.Count);
                }

                // Nhặt từ Unseen
                if (unseenPool.Count >= unseenQuota)
                    slotSelected.AddRange(unseenPool.Take(unseenQuota));
                else
                {
                    slotSelected.AddRange(unseenPool);
                    int deficit = unseenQuota - unseenPool.Count;
                    var remainingWrong = wrongPool.Where(id => !slotSelected.Contains(id)).Take(deficit).ToList();
                    slotSelected.AddRange(remainingWrong);
                }

                // Fallback Mastered
                if (slotSelected.Count < slotTarget)
                {
                    int deficit = slotTarget - slotSelected.Count;
                    slotSelected.AddRange(masteredPool.Take(deficit));
                }

                selectedQuestionIds.AddRange(slotSelected);
            }

            if (selectedQuestionIds.Count == 0)
                return PracticeExamErrors.NoQuestionsFound;

            // ── 5. Bù nếu thiếu (do pool từng mức không đủ) ──
            if (selectedQuestionIds.Count < request.TotalQuestions!.Value)
            {
                var allIds = await _repo.GetAllPracticeQuestionIdsAsync(request.ChapterIds, cls.TeacherId, null);
                var remaining = allIds.Where(id => !selectedQuestionIds.Contains(id)).OrderBy(_ => Guid.NewGuid()).ToList();
                int deficit = request.TotalQuestions.Value - selectedQuestionIds.Count;
                selectedQuestionIds.AddRange(remaining.Take(deficit));
            }

            // Shuffle danh sách cuối cùng
            selectedQuestionIds = selectedQuestionIds.OrderBy(_ => Guid.NewGuid()).ToList();

            // ── 6. Tạo Paper + Submission ──
            var paper = await _repo.CreatePracticePaperAsync(selectedQuestionIds);
            var submission = await _repo.CreatePracticeSubmissionAsync(studentId, paper.PaperId);

            // ── 7. Map questions sang DTO (không gửi đáp án đúng) ──
            var questions = await BuildPracticeQuestionsDto(paper.PaperId);

            // ── 8. Build proficiency snapshot ──
            var profSnapshot = BuildProficiencySnapshot(request.ChapterIds, chapters, profData);

            _logger.LogInformation(
                "Created practice exam: Student={StudentId}, Class={ClassId}, Paper={PaperId}, Questions={Count}",
                studentId, request.ClassId, paper.PaperId, selectedQuestionIds.Count);

            return new CreatePracticeExamResponse
            {
                PaperId = paper.PaperId,
                SubmissionId = submission.SubmissionId,
                TotalQuestions = selectedQuestionIds.Count,
                Questions = questions,
                Proficiency = profSnapshot
            };
        }

        // ════════════════════════════════════════════════════════
        //  NỘP BÀI LUYỆN TẬP — chấm điểm đồng bộ qua GradingService (dùng chung với TakeExam)
        // ════════════════════════════════════════════════════════
        public async Task<Result<SubmitPracticeExamResponse>> SubmitPracticeExamAsync(SubmitPracticeExamRequest request)
        {
            var prep = await PrepareSubmissionForWriteAsync(request);
            if (prep.IsFailure) return prep.Error;

            var submission = prep.Value;
            submission.Status = SubmissionStatus.Submitted;
            await _repo.SaveChangesAsync();

            // Chấm điểm đồng bộ — cùng cơ chế với bài kiểm tra (GradingService).
            // GradingService persist StudentAnswer.IsCorrect/PointsEarned + Submission.TotalPoints.
            await _gradingService.GradeSubmissionAsync(submission.SubmissionId);

            // Khi grading thành công, GradingService set tracked submission.GradingStatus = Graded
            // và save (PracticeExamService.cs caller share cùng DbContext scope).
            // Khi grading throw, catch block dùng raw UPDATE → tracked entity vẫn = InProgress.
            // Vì vậy check "!= Graded" bắt được mọi trạng thái không thành công.
            if (submission.GradingStatus != GradingStatus.Graded)
            {
                _logger.LogError(
                    "Practice grading did not complete for Submission {SubmissionId} (Student={StudentId}, TrackedGradingStatus={Status}).",
                    submission.SubmissionId, _currentUserService.UserId, submission.GradingStatus);
                return PracticeExamErrors.GradingFailed;
            }

            var results = ComputeQuestionResults(submission);
            int correctCount = results.Count(r => r.IsCorrect);
            int totalQuestions = results.Count;

            return new SubmitPracticeExamResponse
            {
                SubmissionId = submission.SubmissionId,
                TotalQuestions = totalQuestions,
                CorrectCount = correctCount,
                AccuracyRate = totalQuestions > 0
                    ? Math.Round((double)correctCount / totalQuestions * 100, 1)
                    : 0,
                SubmittedAtUtc = submission.UpdatedAtUtc
            };
        }

        // ════════════════════════════════════════════════════════
        //  LƯU CÂU TRẢ LỜI GIỮA CHỪNG (không nộp bài)
        // ════════════════════════════════════════════════════════
        public async Task<Result> SavePracticeAnswersAsync(SubmitPracticeExamRequest request)
        {
            var prep = await PrepareSubmissionForWriteAsync(request);
            if (prep.IsFailure) return prep.Error;

            await _repo.SaveChangesAsync();
            return Result.Success();
        }

        // Tải submission (tracked), validate, sync answers, set UpdatedAtUtc — KHÔNG SaveChanges.
        // Caller quyết định set Status và khi nào commit.
        private async Task<Result<Submission>> PrepareSubmissionForWriteAsync(SubmitPracticeExamRequest request)
        {
            var studentId = _currentUserService.UserId;
            var submission = await _repo.GetPracticeSubmissionFullAsync(
                request.SubmissionId!.Value, studentId, tracked: true);
            if (submission == null)
                return PracticeExamErrors.SubmissionNotFound;

            if (submission.Status != SubmissionStatus.InProgress)
                return PracticeExamErrors.AlreadySubmitted;

            var paper = submission.Paper;
            if (paper == null)
                return PracticeExamErrors.PaperNotFound;

            var validQAIds = paper.Questions
                .SelectMany(q => q.QuestionAnswers)
                .Select(qa => qa.QuestionAnswerId)
                .ToHashSet();

            foreach (var sa in request.StudentAnswers!)
            {
                if (!validQAIds.Contains(sa.QuestionAnswerId!.Value))
                    return PracticeExamErrors.InvalidAnswer;
            }

            var incomingData = request.StudentAnswers!
                .Where(sa => sa.QuestionAnswerId.HasValue)
                .Select(sa => (sa.QuestionAnswerId!.Value, sa.Response));

            StudentAnswerSyncHelper.SyncAnswers(
                submission.StudentAnswers,
                incomingData,
                submission.SubmissionId);

            submission.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return submission;
        }

        private record QuestionResult(Question Question, bool IsCorrect, List<PracticeOptionReviewDto> Options);

        // Đọc kết quả chấm đã persisted bởi GradingService (cùng cơ chế với TakeExam).
        // Câu đúng = mọi QuestionAnswer của câu đó đều khớp:
        //   - QA có sa: sa.IsCorrect == true
        //   - QA không có sa: qa.IsCorrect != true (option đúng mà bỏ → câu sai)
        // Trả về cả option DTOs để Result endpoint reuse, tránh duplicate iteration.
        private static List<QuestionResult> ComputeQuestionResults(Submission submission)
        {
            var questions = submission.Paper?.Questions?.DistinctBy(q => q.QuestionId).ToList()
                            ?? new List<Question>();

            // Dictionary lookup thay vì O(N²) FirstOrDefault.
            var saByQaId = submission.StudentAnswers.ToDictionary(a => a.QuestionAnswerId);

            var results = new List<QuestionResult>(questions.Count);
            foreach (var question in questions)
            {
                bool questionCorrect = true;
                var optionDtos = new List<PracticeOptionReviewDto>(question.QuestionAnswers.Count);
                foreach (var qa in question.QuestionAnswers)
                {
                    saByQaId.TryGetValue(qa.QuestionAnswerId, out var sa);

                    if (sa == null)
                    {
                        if (qa.IsCorrect == true) questionCorrect = false;
                    }
                    else if (sa.IsCorrect != true)
                    {
                        questionCorrect = false;
                    }

                    optionDtos.Add(new PracticeOptionReviewDto
                    {
                        QuestionAnswerId = qa.QuestionAnswerId,
                        Content = qa.Content,
                        StudentResponse = sa?.Response,
                        IsSelected = sa != null,
                        IsCorrect = qa.IsCorrect,
                        CorrectAnswer = qa.CorrectAnswer
                    });
                }

                results.Add(new QuestionResult(question, questionCorrect, optionDtos));
            }

            return results;
        }

        // ════════════════════════════════════════════════════════
        //  RESUME BÀI LUYỆN TẬP ĐANG LÀM DỎ
        // ════════════════════════════════════════════════════════
        public async Task<Result<ResumePracticeExamResponse>> ResumePracticeExamAsync(int submissionId)
        {
            var studentId = _currentUserService.UserId;
            var submission = await _repo.GetPracticeSubmissionFullAsync(submissionId, studentId);
            if (submission == null)
                return PracticeExamErrors.SubmissionNotFound;

            if (submission.Status != SubmissionStatus.InProgress)
                return PracticeExamErrors.AlreadySubmitted;

            // Load đầy đủ câu hỏi từ paper (qua PaperQuestion)
            var questions = await BuildPracticeQuestionsDto(submission.PaperId);

            // Lấy câu trả lời đã lưu trước đó (nếu có)
            var savedAnswers = submission.StudentAnswers
                .Select(sa => new SavedAnswerDto
                {
                    QuestionAnswerId = sa.QuestionAnswerId,
                    Response = sa.Response
                })
                .ToList();

            return new ResumePracticeExamResponse
            {
                PaperId = submission.PaperId,
                SubmissionId = submission.SubmissionId,
                TotalQuestions = questions.Count,
                CreatedAtUtc = submission.CreatedAtUtc,
                Questions = questions,
                SavedAnswers = savedAnswers
            };
        }

        // ════════════════════════════════════════════════════════
        //  XEM KẾT QUẢ + ĐÁP ÁN TỪNG CÂU
        // ════════════════════════════════════════════════════════
        public async Task<Result<PracticeExamResultDto>> GetPracticeResultAsync(int submissionId)
        {
            var studentId = _currentUserService.UserId;
            var submission = await _repo.GetPracticeSubmissionFullAsync(submissionId, studentId);
            if (submission == null)
                return PracticeExamErrors.SubmissionNotFound;

            if (submission.Status != SubmissionStatus.Submitted)
                return PracticeExamErrors.NotSubmitted;

            var results = ComputeQuestionResults(submission);
            int correctCount = results.Count(r => r.IsCorrect);
            int totalQ = results.Count;
            int wrongCount = totalQ - correctCount;

            var answerReview = new List<PracticeAnswerReviewDto>(results.Count);
            var chapterAgg = new Dictionary<int, (string Name, int Total, int Correct)>();

            int questionOrder = 0;
            foreach (var r in results)
            {
                questionOrder++;
                var chapterId = r.Question.Chapter?.ChapterId ?? 0;
                var chapterName = r.Question.Chapter?.Name ?? "N/A";

                var prev = chapterAgg.TryGetValue(chapterId, out var v) ? v : (chapterName, 0, 0);
                chapterAgg[chapterId] = (chapterName, prev.Item2 + 1, prev.Item3 + (r.IsCorrect ? 1 : 0));

                answerReview.Add(new PracticeAnswerReviewDto
                {
                    QuestionId = r.Question.QuestionId,
                    QuestionOrder = questionOrder,
                    QuestionContent = r.Question.QuestionContent,
                    QuestionType = r.Question.QuestionType,
                    ChapterName = chapterName,
                    Difficulty = r.Question.Difficulty,
                    IsCorrect = r.IsCorrect,
                    Options = r.Options
                });
            }
            return new PracticeExamResultDto
            {
                SubmissionId = submission.SubmissionId,
                TotalQuestions = totalQ,
                CorrectCount = correctCount,
                WrongCount = wrongCount,
                AccuracyRate = totalQ > 0 ? Math.Round((double)correctCount / totalQ * 100, 1) : 0,
                CreatedAtUtc = submission.CreatedAtUtc,
                SubmittedAtUtc = submission.UpdatedAtUtc,
                AnswerReview = answerReview,
                ChapterStats = chapterAgg
                    .Select(kv => new ChapterProficiencyDto
                    {
                        ChapterId = kv.Key,
                        ChapterName = kv.Value.Name,
                        TotalAttempted = kv.Value.Total,
                        OverallAccuracyRate = kv.Value.Total > 0
                            ? Math.Round((double)kv.Value.Correct / kv.Value.Total * 100, 1) : 0
                    })
                    .OrderBy(c => c.OverallAccuracyRate)
                    .ToList()
            };
        }

        // ════════════════════════════════════════════════════════
        //  LỊCH SỬ LUYỆN TẬP
        // ════════════════════════════════════════════════════════
        public async Task<Result<List<PracticeHistoryDto>>> GetPracticeHistoryAsync(int? classId)
        {
            var studentId = _currentUserService.UserId;
            var rawHistory = await _repo.GetPracticeHistoryAsync(studentId, classId);

            return rawHistory.Select(h =>
            {
                int totalQ = h.TotalQuestions;
                return new PracticeHistoryDto
                {
                    SubmissionId = h.SubmissionId,
                    PaperId = h.PaperId,
                    SubjectName = h.SubjectName,
                    SubjectCode = h.SubjectCode,
                    ChapterNames = h.ChapterNames,
                    TotalQuestions = totalQ,
                    CorrectCount = h.CorrectCount,
                    AccuracyRate = h.CorrectCount.HasValue && totalQ > 0
                        ? Math.Round((double)h.CorrectCount.Value / totalQ * 100, 1) : null,
                    CreatedAtUtc = h.CreatedAtUtc,
                    CompletedAtUtc = h.Status == SubmissionStatus.Submitted
                        ? h.UpdatedAtUtc : null,
                    Status = h.Status == SubmissionStatus.Submitted ? "Đã nộp" : "Đang làm"
                };
            }).ToList();
        }

        // ════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Tự động phân bổ quota câu hỏi theo mức độ dựa trên proficiency.
        /// Nếu chưa có lịch sử → chia đều 4 mức độ.
        /// Nếu có lịch sử → mức yếu được ưu tiên nhiều hơn (weight: yếu=3, TB/chưa làm=2, mạnh=1).
        /// </summary>
        private Dictionary<int, int> ComputeDifficultyQuotas(List<StudentProficiencyRaw> profData, int totalQuestions)
        {
            var quotas = new Dictionary<int, int>();
            bool hasHistory = profData.Any(p => p.TotalAttempted > 0);

            if (!hasHistory)
            {
                // Chia đều 4 mức độ
                int baseCount = totalQuestions / 4;
                int remainder = totalQuestions - baseCount * 4;
                for (int diff = 1; diff <= 4; diff++)
                {
                    quotas[diff] = baseCount + (diff <= remainder ? 1 : 0);
                }
                return quotas;
            }

            // Tính accuracy rate cho từng mức độ
            var weights = new Dictionary<int, double>();
            for (int diff = 1; diff <= 4; diff++)
            {
                var slot = profData.FirstOrDefault(p => p.Difficulty == diff);
                int attempted = slot?.TotalAttempted ?? 0;
                int correct = slot?.CorrectCount ?? 0;

                if (attempted == 0)
                {
                    weights[diff] = 2.0; // Chưa làm → cần luyện
                }
                else
                {
                    double rate = (double)correct / attempted * 100;
                    if (rate < WeakThreshold)     weights[diff] = 3.0; // Yếu
                    else if (rate < StrongThreshold) weights[diff] = 2.0; // Trung bình
                    else                          weights[diff] = 1.0; // Mạnh
                }
            }

            double totalWeight = weights.Values.Sum();
            var rawQuotas = new List<(int Diff, int Floor, double Frac)>();
            int sumFloor = 0;

            for (int diff = 1; diff <= 4; diff++)
            {
                double raw = totalQuestions * weights[diff] / totalWeight;
                int floor = (int)Math.Floor(raw);
                rawQuotas.Add((diff, floor, raw - floor));
                sumFloor += floor;
            }

            // Phân bổ phần dư cho mức có frac cao nhất
            int leftover = totalQuestions - sumFloor;
            var sorted = rawQuotas.OrderByDescending(q => q.Frac).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int bonus = (i < leftover) ? 1 : 0;
                quotas[sorted[i].Diff] = sorted[i].Floor + bonus;
            }

            return quotas;
        }

        private async Task<List<PracticeQuestionDto>> BuildPracticeQuestionsDto(int paperId)
        {
            // Reload paper với đầy đủ QuestionAnswers + BlankInputs + InputType
            var paper = await _repo.GetPracticePaperWithQuestionsAsync(paperId);
            if (paper == null) return new List<PracticeQuestionDto>();

            return paper.Questions.Select(q => new PracticeQuestionDto
            {
                QuestionId = q.QuestionId,
                QuestionType = q.QuestionType,
                QuestionContent = q.QuestionContent,
                Difficulty = q.Difficulty,
                Answers = q.QuestionAnswers.Select(qa => new PracticeAnswerOptionDto
                {
                    QuestionAnswerId = qa.QuestionAnswerId,
                    Content = qa.Content,
                    GroupAnswerId = qa.GroupAnswerId,
                    InputTypes = qa.BlankInputs.Select(bi => new PracticeInputTypeDto
                    {
                        InputTypeId = bi.InputTypeId,
                        Name = bi.InputType?.Name ?? string.Empty,
                        GroupType = bi.InputType?.GroupType
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        private PracticeExamProficiencySnapshot BuildProficiencySnapshot(
            List<int> chapterIds, List<Chapter> allChapters, List<StudentProficiencyRaw> profData)
        {
            var snapshot = new PracticeExamProficiencySnapshot();
            foreach (var chId in chapterIds)
            {
                var chapter = allChapters.FirstOrDefault(c => c.ChapterId == chId);
                if (chapter == null) continue;

                var chapterProf = profData.Where(p => p.ChapterId == chId).ToList();
                var totalAttempted = chapterProf.Sum(p => p.TotalAttempted);
                var totalCorrect = chapterProf.Sum(p => p.CorrectCount);

                snapshot.ChapterProficiencies.Add(new ChapterProficiencyDto
                {
                    ChapterId = chId,
                    ChapterName = chapter.Name,
                    TotalAttempted = totalAttempted,
                    OverallAccuracyRate = totalAttempted > 0
                        ? Math.Round((double)totalCorrect / totalAttempted * 100, 1) : 0,
                    DifficultyBreakdown = Enumerable.Range(1, 4).Select(diff =>
                    {
                        var slot = chapterProf.FirstOrDefault(p => p.Difficulty == diff);
                        return new DifficultyProficiencyDto
                        {
                            Difficulty = diff,
                            DifficultyName = DifficultyLevel.GetLabel(diff),
                            TotalAttempted = slot?.TotalAttempted ?? 0,
                            CorrectCount = slot?.CorrectCount ?? 0
                        };
                    }).ToList()
                });
            }
            return snapshot;
        }
    }
}
