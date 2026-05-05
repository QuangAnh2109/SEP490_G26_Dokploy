// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Backend.Models;
// using Backend.Repositories.Interfaces;
// using Backend.Services.Implements;
// using Moq;
// using Xunit;
// 
// namespace Backend_UnitTest.AssignExamTests
// {
//     public class GetExamReviewAsync_UTCID_Tests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public GetExamReviewAsync_UTCID_Tests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         // UTCID01 – Exam tồn tại, có dữ liệu đầy đủ -> map đúng
//         [Fact(DisplayName = "GetExamReviewAsync - UTCID01 - Exam tồn tại, có dữ liệu -> trả ExamReviewDto")]
//         public async Task GetExamReviewAsync_UTCID01_ExamExists_ShouldReturnReviewDto()
//         {
//             // Arrange
//             int examId = 1;
// 
//             var subject = new Subject { SubjectId = 5, Name = "Math", Code = "MAE" };
//             var teacher = new User { UserId = 1, Email = "t@x.com", FullName = "Teacher A", ConcurrencyStamp = Array.Empty<byte>() };
// 
//             var chapter1 = new Chapter { ChapterId = 10, SubjectId = 5, Name = "Chương 1" };
//             var chapter2 = new Chapter { ChapterId = 11, SubjectId = 5, Name = "Chương 2" };
// 
//             var blueprint = new ExamBlueprint
//             {
//                 ExamBlueprintId = 100,
//                 SubjectId = 5,
//                 Name = "BP",
//                 TotalQuestions = 0,
//                 TeacherId = 1,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = 1,
//                 ConcurrencyStamp = Array.Empty<byte>(),
//                 ExamBlueprintChapters = new List<ExamBlueprintChapter>
//                 {
//                     new ExamBlueprintChapter { ExamBlueprintId = 100, ChapterId = 10, Chapter = chapter1, Difficulty = 1, TotalOfQuestions = 2, ConcurrencyStamp = Array.Empty<byte>() },
//                     new ExamBlueprintChapter { ExamBlueprintId = 100, ChapterId = 10, Chapter = chapter1, Difficulty = 2, TotalOfQuestions = 1, ConcurrencyStamp = Array.Empty<byte>() },
//                     new ExamBlueprintChapter { ExamBlueprintId = 100, ChapterId = 11, Chapter = chapter2, Difficulty = 3, TotalOfQuestions = 3, ConcurrencyStamp = Array.Empty<byte>() },
//                 }
//             };
// 
//             var q1 = new Question
//             {
//                 QuestionId = 1001,
//                 CreatedByUserId = 1,
//                 CreatedByUser = teacher,
//                 QuestionType = "MCQ",
//                 QuestionContent = "Q1",
//                 ChapterId = 10,
//                 Chapter = chapter1,
//                 Difficulty = 1,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = "Active",
//                 ConcurrencyStamp = Array.Empty<byte>(),
//                 QuestionAnswers = new List<QuestionAnswer>
//                 {
//                     // IsCorrect null -> service map thành false
//                     new QuestionAnswer { QuestionAnswerId = 1, QuestionId = 1001, Content = "A", CorrectAnswer = "A", IsCorrect = null, ConcurrencyStamp = Array.Empty<byte>() },
//                     new QuestionAnswer { QuestionAnswerId = 2, QuestionId = 1001, Content = "B", CorrectAnswer = "A", IsCorrect = true, ConcurrencyStamp = Array.Empty<byte>() },
//                 }
//             };
// 
//             var q2 = new Question
//             {
//                 QuestionId = 1002,
//                 CreatedByUserId = 1,
//                 CreatedByUser = teacher,
//                 QuestionType = "MCQ",
//                 QuestionContent = "Q2",
//                 ChapterId = 11,
//                 Chapter = chapter2,
//                 Difficulty = 3,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = "Inprogress",
//                 ConcurrencyStamp = Array.Empty<byte>(),
//                 QuestionAnswers = new List<QuestionAnswer>
//                 {
//                     new QuestionAnswer { QuestionAnswerId = 3, QuestionId = 1002, Content = "C", CorrectAnswer = "C", IsCorrect = false, ConcurrencyStamp = Array.Empty<byte>() },
//                 }
//             };
// 
//             var paper1 = new Paper { PaperId = 5001, ExamId = examId, Code = 1, Questions = new List<Question> { q1, q2 } };
//             var paper2 = new Paper { PaperId = 5002, ExamId = examId, Code = 2, Questions = new List<Question> { q1 } };
// 
//             var exam = new Exam
//             {
//                 ExamId = examId,
//                 ClassId = 10,
//                 Title = "Midterm",
//                 Description = "Desc",
//                 Duration = 60,
//                 OpenAt = new DateTime(2026, 4, 1, 9, 0, 0),
//                 CloseAt = new DateTime(2026, 4, 1, 10, 0, 0),
//                 UpdatedAtUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
//                 Status = 0,
//                 Subject = subject,
//                 Teacher = teacher,
//                 ExamBlueprint = blueprint,
//                 Papers = new List<Paper> { paper1, paper2 },
//                 ConcurrencyStamp = Array.Empty<byte>()
//             };
// 
//             _repoMock.Setup(r => r.GetExamReviewDataAsync(examId, _ct))
//                      .ReturnsAsync(exam);
// 
//             // Act
//             var result = await _service.GetExamReviewAsync(examId, _ct);
// 
//             // Assert (Return)
//             Assert.NotNull(result);
//             Assert.Equal(examId, result.ExamId);
//             Assert.Equal("Midterm", result.Title);
//             Assert.Equal("MAE", result.SubjectCode);
//             Assert.Equal("Teacher A", result.TeacherName);
// 
//             // TotalQuestions lấy theo paper đầu tiên
//             Assert.Equal(2, result.TotalQuestions);
// 
//             // BlueprintMatrix: group theo ChapterName và sum theo difficulty
//             var rowCh1 = result.BlueprintMatrix.Single(x => x.ChapterName == "Chương 1");
//             Assert.Equal(2, rowCh1.Recognize);   // diff=1
//             Assert.Equal(1, rowCh1.Understand);  // diff=2
//             Assert.Equal(0, rowCh1.Apply);       // diff=3
//             Assert.Equal(0, rowCh1.AdvancedApply); // diff=4
//             Assert.Equal(3, rowCh1.Total);
// 
//             // Answers: IsCorrect null -> false
//             var firstPaper = result.Papers.Single(p => p.PaperId == 5001);
//             var firstQuestion = firstPaper.Questions.Single(q => q.QuestionId == 1001);
//             var ansNull = firstQuestion.Answers.Single(a => a.AnswerId == 1);
//             Assert.False(ansNull.IsCorrect);
// 
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID02 – Exam không tồn tại -> KeyNotFoundException
//         [Fact(DisplayName = "GetExamReviewAsync - UTCID02 - Exam không tồn tại -> KeyNotFoundException")]
//         public async Task GetExamReviewAsync_UTCID02_ExamNotFound_ShouldThrowKeyNotFoundException()
//         {
//             // Arrange
//             int examId = 999;
// 
//             _repoMock.Setup(r => r.GetExamReviewDataAsync(examId, _ct))
//                      .ReturnsAsync((Exam?)null);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.GetExamReviewAsync(examId, _ct));
// 
//             // Assert
//             Assert.Equal("Exam not found.", ex.Message);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID03 – Subject.Code null hoặc Teacher.FullName null -> fallback "N/A"
//         [Fact(DisplayName = "GetExamReviewAsync - UTCID03 - SubjectCode/TeacherName null -> fallback N/A")]
//         public async Task GetExamReviewAsync_UTCID03_NullSubjectCodeOrTeacherName_ShouldFallbackNA()
//         {
//             // Arrange
//             int examId = 2;
// 
//             var subject = new Subject { SubjectId = 5, Name = "Math", Code = null };
//             var teacher = new User { UserId = 1, Email = "t@x.com", FullName = null, ConcurrencyStamp = Array.Empty<byte>() };
// 
//             var exam = new Exam
//             {
//                 ExamId = examId,
//                 ClassId = null,
//                 Title = "Exam",
//                 Description = null,
//                 Duration = 30,
//                 OpenAt = null,
//                 CloseAt = null,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = 0,
//                 Subject = subject,
//                 Teacher = teacher,
//                 ExamBlueprint = null,
//                 Papers = new List<Paper>
//                 {
//                     new Paper { PaperId = 1, ExamId = examId, Code = 1, Questions = new List<Question>() }
//                 },
//                 ConcurrencyStamp = Array.Empty<byte>()
//             };
// 
//             _repoMock.Setup(r => r.GetExamReviewDataAsync(examId, _ct))
//                      .ReturnsAsync(exam);
// 
//             // Act
//             var result = await _service.GetExamReviewAsync(examId, _ct);
// 
//             // Assert
//             Assert.Equal("N/A", result.SubjectCode);
//             Assert.Equal("N/A", result.TeacherName);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "GetExamReviewAsync - UTCID04 - Chapter null, Subject null, Teacher null, không có paper -> fallback đầy đủ")]
//         public async Task GetExamReviewAsync_UTCID04_NullChapterSubjectTeacherAndNoPapers_ShouldFallback()
//         {
//             int examId = 4;
// 
//             var blueprint = new ExamBlueprint
//             {
//                 ExamBlueprintId = 100,
//                 SubjectId = 5,
//                 Name = "BP",
//                 TeacherId = 1,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = 1,
//                 ConcurrencyStamp = Array.Empty<byte>(),
//                 ExamBlueprintChapters = new List<ExamBlueprintChapter>
//                 {
//                     new ExamBlueprintChapter
//                     {
//                         ExamBlueprintId = 100,
//                         ChapterId = 10,
//                         Chapter = null!,
//                         Difficulty = 4,
//                         TotalOfQuestions = 2,
//                         ConcurrencyStamp = Array.Empty<byte>()
//                     }
//                 }
//             };
// 
//             var exam = new Exam
//             {
//                 ExamId = examId,
//                 ClassId = null,
//                 Title = "Exam",
//                 Description = null,
//                 Duration = 30,
//                 OpenAt = null,
//                 CloseAt = null,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = 0,
//                 Subject = null!,
//                 Teacher = null!,
//                 ExamBlueprint = blueprint,
//                 Papers = new List<Paper>(),
//                 ConcurrencyStamp = Array.Empty<byte>()
//             };
// 
//             _repoMock.Setup(r => r.GetExamReviewDataAsync(examId, _ct))
//                      .ReturnsAsync(exam);
// 
//             var result = await _service.GetExamReviewAsync(examId, _ct);
// 
//             Assert.Equal("N/A", result.SubjectCode);
//             Assert.Equal("N/A", result.TeacherName);
//             Assert.Equal(0, result.TotalQuestions);
//             Assert.Single(result.BlueprintMatrix);
//             Assert.Equal("N/A", result.BlueprintMatrix[0].ChapterName);
//             Assert.Equal(2, result.BlueprintMatrix[0].AdvancedApply);
//             Assert.Empty(result.Papers);
//             _repoMock.VerifyAll();
//         }
//     }
// }
// 
