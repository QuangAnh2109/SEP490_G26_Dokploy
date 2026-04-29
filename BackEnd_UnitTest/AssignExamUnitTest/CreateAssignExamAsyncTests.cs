// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Backend.DTOs;
// using Backend.Models;
// using Backend.Repositories.Interfaces;
// using Backend.Services.Implements;
// using Microsoft.EntityFrameworkCore.Storage;
// using Moq;
// using Xunit;
// 
// namespace Backend_UnitTest.AssignExamTests
// {
//     public class CreateAssignExamAsyncTests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public CreateAssignExamAsyncTests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         private static CreateAssignExamRequest BaseRequest()
//         {
//             return new CreateAssignExamRequest
//             {
//                 
//                 Title = "Midterm Test",
//                 Duration = 60,
//                 MaxAttempts = 1,
//                 PaperCount = 2,
//                 PaperCode = 1,
//                 VisibleFrom = new DateTime(2026, 4, 1, 8, 0, 0),
//                 OpenAt = new DateTime(2026, 4, 1, 9, 0, 0),
//                 CloseAt = new DateTime(2026, 4, 1, 10, 0, 0),
//                 ShuffleQuestion = false,
//                 IsPublic = false,
//                 ClassId = 10,
//                 GenerationMode = "blueprint",
//                 ExamBlueprintId = 100
//             };
//         }
// 
//         private void SetupTransaction(out Mock<IDbContextTransaction> txMock)
//         {
//             txMock = new Mock<IDbContextTransaction>(MockBehavior.Strict);
//             txMock.Setup(t => t.CommitAsync(_ct)).Returns(Task.CompletedTask);
//             txMock.Setup(t => t.RollbackAsync(_ct)).Returns(Task.CompletedTask);
//             txMock.Setup(t => t.Dispose());
// 
//             _repoMock.Setup(r => r.BeginTransactionAsync(_ct))
//                      .ReturnsAsync(txMock.Object);
//         }
// 
//         private void SetupSaveExamAndPapers(int paperCount, int startPaperId = 1000, int examId = 500)
//         {
//             _repoMock.Setup(r => r.SaveExamAsync(It.IsAny<Exam>(), _ct))
//                      .ReturnsAsync((Exam e, CancellationToken _) =>
//                      {
//                          e.ExamId = examId;
//                          return e;
//                      });
// 
//             int nextPaperId = startPaperId;
//             _repoMock.Setup(r => r.SavePaperAsync(It.IsAny<Paper>(), _ct))
//                      .ReturnsAsync((Paper p, CancellationToken _) =>
//                      {
//                          p.PaperId = nextPaperId++;
//                          return p;
//                      });
// 
//             _repoMock.Setup(r => r.AddPaperQuestionsAsync(It.IsAny<int>(), It.IsAny<List<int>>(), _ct))
//                      .Returns(Task.CompletedTask);
//         }
// 
//         private static ExamBlueprint MakeBlueprint(int blueprintId, int subjectId, params ExamBlueprintChapter[] rows)
//         {
//             var bp = new ExamBlueprint
//             {
//                 ExamBlueprintId = blueprintId,
//                 SubjectId = subjectId,
//                 Name = "BP",
//                 
//                 ConcurrencyStamp = new byte[] { 1 }
//             };
// 
//             foreach (var r in rows)
//             {
//                 bp.ExamBlueprintChapters.Add(r);
//             }
// 
//             return bp;
//         }
// 
//         private static ExamBlueprintChapter MakeRow(int chapterId, int difficulty, int totalQuestions)
//         {
//             return new ExamBlueprintChapter
//             {
//                 ExamBlueprintId = 100,
//                 ChapterId = chapterId,
//                 Difficulty = difficulty,
//                 TotalOfQuestions = totalQuestions,
//                 ConcurrencyStamp = new byte[] { 1 }
//             };
//         }
// 
//         // UTCID01 – blueprint + lớp riêng (class hợp lệ) => tạo exam thành công
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID01 - Blueprint + class hợp lệ => tạo exam thành công")]
//         public async Task CreateAssignExamAsync_UTCID01_Blueprint_PrivateClass_Valid_ShouldCreateExam()
//         {
//             // Arrange
//             var r = BaseRequest(); // teacherId=1, classId=10, blueprintId=100
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 1));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2, 3, 4 });
// 
//             _repoMock.Setup(x => x.GetClassByIdAsync(10, _ct))
//                      .ReturnsAsync(new Class { ClassId = 10,  SubjectId = 5, Name = "C", Semester = "S", InvitationCode = "X", ConcurrencyStamp = new byte[] { 1 } });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount);
// 
//             // Act
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             // Assert
//             Assert.True(res.ExamId > 0);
//             Assert.Equal(r.PaperCount, res.Papers.Count);
//             Assert.True(res.TotalQuestions > 0);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             VerifyPersistenceCommitted(r.PaperCount);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID02 – blueprint + public (classId=null) => tạo exam thành công
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID02 - Blueprint + public => tạo exam thành công")]
//         public async Task CreateAssignExamAsync_UTCID02_Blueprint_Public_Valid_ShouldCreateExam()
//         {
//             // Arrange
//             var r = BaseRequest();
//             r.IsPublic = true;
//             r.ClassId = null;
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 1));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2, 3, 4 });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount, startPaperId: 2000, examId: 600);
// 
//             // Act
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             // Assert
//             Assert.True(res.ExamId > 0);
//             Assert.Equal(r.PaperCount, res.Papers.Count);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             VerifyPersistenceCommitted(r.PaperCount);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID03 – manual + lớp riêng (class hợp lệ) => tạo exam thành công
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID03 - Manual + class hợp lệ => tạo exam thành công")]
//         public async Task CreateAssignExamAsync_UTCID03_Manual_PrivateClass_Valid_ShouldCreateExam()
//         {
//             // Arrange
//             var r = BaseRequest();
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = 5;
//             r.QuestionIds = new List<int> { 1, 2, 3 };
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             _repoMock.Setup(x => x.GetQuestionsWithSubjectByIdsAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2, 3 })), It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<QuestionSubjectDto>
//                      {
//                          new QuestionSubjectDto(1, 5),
//                          new QuestionSubjectDto(2, 5),
//                          new QuestionSubjectDto(3, 5),
//                      });
// 
//             _repoMock.Setup(x => x.GetClassByIdAsync(10, _ct))
//                      .ReturnsAsync(new Class { ClassId = 10,  SubjectId = 5, Name = "C", Semester = "S", InvitationCode = "X", ConcurrencyStamp = new byte[] { 1 } });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount, startPaperId: 3000, examId: 700);
// 
//             // Act
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             // Assert
//             Assert.True(res.ExamId > 0);
//             Assert.Equal(r.PaperCount, res.Papers.Count);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             VerifyPersistenceCommitted(r.PaperCount);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID04 – manual + public => tạo exam thành công
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID04 - Manual + public => tạo exam thành công")]
//         public async Task CreateAssignExamAsync_UTCID04_Manual_Public_Valid_ShouldCreateExam()
//         {
//             // Arrange
//             var r = BaseRequest();
//             r.IsPublic = true;
//             r.ClassId = null;
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = 5;
//             r.QuestionIds = new List<int> { 1, 2, 3 };
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             _repoMock.Setup(x => x.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<QuestionSubjectDto>
//                      {
//                          new QuestionSubjectDto(1, 5),
//                          new QuestionSubjectDto(2, 5),
//                          new QuestionSubjectDto(3, 5),
//                      });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount, startPaperId: 4000, examId: 800);
// 
//             // Act
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             // Assert
//             Assert.True(res.ExamId > 0);
//             Assert.Equal(r.PaperCount, res.Papers.Count);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             VerifyPersistenceCommitted(r.PaperCount);
//             _repoMock.VerifyAll();
//         }
// 
//         private void VerifyPersistenceCommitted(int paperCount)
//         {
//             _repoMock.Verify(r => r.SaveExamAsync(It.IsAny<Exam>(), _ct), Times.Once);
//             _repoMock.Verify(r => r.SavePaperAsync(It.IsAny<Paper>(), _ct), Times.Exactly(paperCount));
//             _repoMock.Verify(r => r.AddPaperQuestionsAsync(It.IsAny<int>(), It.IsAny<List<int>>(), _ct), Times.Exactly(paperCount));
//         }
// 
//         // UTCID05 – invalid teacher id (0) => ArgumentException("TeacherId is required.")
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID05 - TeacherId=0 => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID05_TeacherIdZero_ShouldThrow()
//         {
//             // Arrange
//             var r = BaseRequest();
//             r = 0;
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             // Assert
//             Assert.Equal("TeacherId is required.", ex.Message);
//             _repoMock.Verify(x => x.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID06 – VisibleFrom > OpenAt => ArgumentException("VisibleFrom > OpenAt.")
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID06 - VisibleFrom > OpenAt => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID06_VisibleFromAfterOpenAt_ShouldThrow()
//         {
//             // Arrange
//             var r = BaseRequest();
//             r.VisibleFrom = new DateTime(2026, 4, 1, 10, 0, 0);
//             r.OpenAt = new DateTime(2026, 4, 1, 9, 0, 0);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             // Assert
//             Assert.Equal("VisibleFrom > OpenAt.", ex.Message);
//             _repoMock.Verify(x => x.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID07 – Blueprint nhưng không đủ câu hỏi cho 1 row => InvalidOperationException
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID07 - Blueprint row không đủ câu hỏi => InvalidOperationException")]
//         public async Task CreateAssignExamAsync_UTCID07_BlueprintNotEnoughQuestions_ShouldThrow()
//         {
//             // Arrange
//             var r = BaseRequest();
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 5));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
// 
//             // Bank chỉ có 2 câu, nhưng cần 5
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2 });
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             // Assert
//             Assert.Contains("Not enough questions for chapter 10 with difficulty 2.", ex.Message);
//             _repoMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID08 – Lớp học không hợp lệ (không tìm thấy lớp) => KeyNotFoundException("Class not found.")
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID08 - ClassId không hợp lệ => KeyNotFoundException")]
//         public async Task CreateAssignExamAsync_UTCID08_ClassNotFound_ShouldThrow()
//         {
//             // Arrange
//             var r = BaseRequest();
//             r.ClassId = 999;
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 1));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2, 3 });
// 
//             _repoMock.Setup(x => x.GetClassByIdAsync(999, _ct))
//                      .ReturnsAsync((Class?)null);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             // Assert
//             Assert.Equal("Class not found.", ex.Message);
//             _repoMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID09 - Blueprint không tồn tại => KeyNotFoundException")]
//         public async Task CreateAssignExamAsync_UTCID09_BlueprintNotFound_ShouldThrow()
//         {
//             var r = BaseRequest();
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync((ExamBlueprint?)null);
// 
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("Blueprint not found.", ex.Message);
//             _repoMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID10 - Blueprint wrap bank + PaperCode=0 => tạo exam thành công")]
//         public async Task CreateAssignExamAsync_UTCID10_BlueprintWrapAroundAndPaperCodeZero_ShouldCreateExam()
//         {
//             var r = BaseRequest();
//             r.IsPublic = true;
//             r.ClassId = null;
//             r.PaperCode = 0;
//             r.PaperCount = 2;
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 2));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2, 3 });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount, startPaperId: 5000, examId: 900);
// 
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             Assert.Equal(900, res.ExamId);
//             Assert.Equal(2, res.Papers.Count);
//             Assert.Equal(1, res.Papers[0].Code);
//             Assert.Equal(2, res.Papers[1].Code);
//             Assert.Equal(2, res.TotalQuestions);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID11 - Manual + shuffle + SubjectId null => tạo exam thành công")]
//         public async Task CreateAssignExamAsync_UTCID11_ManualShuffleWithNullSubjectId_ShouldCreateExam()
//         {
//             var r = BaseRequest();
//             r.IsPublic = true;
//             r.ClassId = null;
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = null;
//             r.ShuffleQuestion = true;
//             r.QuestionIds = new List<int> { 1, 2, 3 };
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
//             _repoMock.Setup(x => x.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<QuestionSubjectDto>
//                      {
//                          new QuestionSubjectDto(1, 5),
//                          new QuestionSubjectDto(2, 5),
//                          new QuestionSubjectDto(3, 5),
//                      });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount, startPaperId: 6000, examId: 901);
// 
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             Assert.Equal(901, res.ExamId);
//             Assert.Equal(r.PaperCount, res.Papers.Count);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID12 - GenerationMode null và thời gian null => mặc định blueprint")]
//         public async Task CreateAssignExamAsync_UTCID12_NullGenerationModeAndNullDates_ShouldDefaultToBlueprint()
//         {
//             var r = BaseRequest();
//             r.IsPublic = true;
//             r.ClassId = null;
//             r.GenerationMode = null!;
//             r.VisibleFrom = null;
//             r.OpenAt = null;
//             r.CloseAt = null;
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 1));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2, 3 });
// 
//             SetupTransaction(out var txMock);
//             SetupSaveExamAndPapers(r.PaperCount, startPaperId: 7000, examId: 902);
// 
//             var res = await _service.CreateAssignExamAsync(r, _ct);
// 
//             Assert.Equal(902, res.ExamId);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID13 - Manual với QuestionIds null => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID13_ManualQuestionIdsNull_ShouldThrow()
//         {
//             var r = BaseRequest();
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = 5;
//             r.QuestionIds = null!;
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("QuestionIds required.", ex.Message);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID14 - Manual với QuestionIds rỗng => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID14_ManualQuestionIdsEmpty_ShouldThrow()
//         {
//             var r = BaseRequest();
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = 5;
//             r.QuestionIds = new List<int>();
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("QuestionIds required.", ex.Message);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID15 - Manual có question invalid/inactive => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID15_ManualInvalidQuestionIds_ShouldThrow()
//         {
//             var r = BaseRequest();
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = 5;
//             r.QuestionIds = new List<int> { 1, 2 };
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
//             _repoMock.Setup(x => x.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<QuestionSubjectDto>
//                      {
//                          new QuestionSubjectDto(1, 5)
//                      });
// 
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("One or more invalid or inactive questions.", ex.Message);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID16 - Manual khác subject => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID16_ManualDifferentSubjects_ShouldThrow()
//         {
//             var r = BaseRequest();
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = null;
//             r.QuestionIds = new List<int> { 1, 2 };
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
//             _repoMock.Setup(x => x.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<QuestionSubjectDto>
//                      {
//                          new QuestionSubjectDto(1, 5),
//                          new QuestionSubjectDto(2, 6)
//                      });
// 
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("Questions must belong to the same subject.", ex.Message);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID17 - Manual SubjectId mismatch => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID17_ManualSubjectMismatch_ShouldThrow()
//         {
//             var r = BaseRequest();
//             r.GenerationMode = "manual";
//             r.ExamBlueprintId = null;
//             r.SubjectId = 99;
//             r.QuestionIds = new List<int> { 1, 2 };
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
//             _repoMock.Setup(x => x.GetQuestionsWithSubjectByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<QuestionSubjectDto>
//                      {
//                          new QuestionSubjectDto(1, 5),
//                          new QuestionSubjectDto(2, 5)
//                      });
// 
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("Subject mismatch.", ex.Message);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID18 - OpenAt >= CloseAt => ArgumentException")]
//         public async Task CreateAssignExamAsync_UTCID18_OpenAtEqualsCloseAt_ShouldThrow()
//         {
//             var r = BaseRequest();
//             r.OpenAt = new DateTime(2026, 4, 1, 10, 0, 0);
//             r.CloseAt = new DateTime(2026, 4, 1, 10, 0, 0);
// 
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("OpenAt >= CloseAt.", ex.Message);
//             _repoMock.Verify(x => x.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         [Fact(DisplayName = "CreateAssignExamAsync - UTCID19 - Lỗi khi lưu paper => rollback transaction")]
//         public async Task CreateAssignExamAsync_UTCID19_SavePaperFails_ShouldRollback()
//         {
//             var r = BaseRequest();
//             r.IsPublic = true;
//             r.ClassId = null;
// 
//             _repoMock.Setup(x => x.IsUserActiveAsync(r, _ct)).ReturnsAsync(true);
// 
//             var bp = MakeBlueprint(
//                 blueprintId: 100,
//                 subjectId: 5,
//                 MakeRow(chapterId: 10, difficulty: 2, totalQuestions: 1));
// 
//             _repoMock.Setup(x => x.GetBlueprintWithChaptersAsync(100, _ct)).ReturnsAsync(bp);
//             _repoMock.Setup(x => x.GetAllQuestionIdsForBlueprintRowAsync(10, 2, It.IsAny<string[]>(), _ct))
//                      .ReturnsAsync(new List<int> { 1, 2, 3 });
// 
//             SetupTransaction(out var txMock);
// 
//             _repoMock.Setup(rp => rp.SaveExamAsync(It.IsAny<Exam>(), _ct))
//                      .ReturnsAsync((Exam e, CancellationToken _) =>
//                      {
//                          e.ExamId = 903;
//                          return e;
//                      });
// 
//             _repoMock.Setup(rp => rp.SavePaperAsync(It.IsAny<Paper>(), _ct))
//                      .ThrowsAsync(new InvalidOperationException("save paper failed"));
// 
//             var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAssignExamAsync(r, _ct));
// 
//             Assert.Equal("save paper failed", ex.Message);
//             txMock.Verify(t => t.RollbackAsync(_ct), Times.Once);
//             txMock.Verify(t => t.CommitAsync(_ct), Times.Never);
//             _repoMock.VerifyAll();
//         }
//     }
// }
// 
