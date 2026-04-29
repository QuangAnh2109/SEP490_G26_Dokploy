// using System;
// using System.Collections.Generic;
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
//     public class SwapPaperQuestionAsync_UTCID_Tests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public SwapPaperQuestionAsync_UTCID_Tests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID01 - Valid request -> swap in paper successfully")]
//         public async Task SwapPaperQuestionAsync_UTCID01_ValidRequest_ShouldSwapInPaper()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(
//                 PaperId: 1,
//                 OldQuestionId: 100,
//                 NewQuestionId: 200,
//                 SwapGlobal: false);
// 
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
//             var newQuestion = CreateQuestion(questionId: 200, chapterId: 5, difficulty: 2, status: "Active");
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId, _ct))
//                      .ReturnsAsync(newQuestion);
//             _repoMock.Setup(r => r.SwapPaperQuestionAsync(request.PaperId, request.OldQuestionId, request.NewQuestionId, _ct))
//                      .Returns(Task.CompletedTask);
// 
//             // Act
//             await _service.SwapPaperQuestionAsync(request, _ct);
// 
//             // Assert
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(request.PaperId, request.OldQuestionId, request.NewQuestionId, _ct), Times.Once);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID02 - Paper not found -> throw KeyNotFoundException")]
//         public async Task SwapPaperQuestionAsync_UTCID02_PaperNotFound_ShouldThrowKeyNotFoundException()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(999, 100, 200, false);
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync((Paper?)null);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.SwapPaperQuestionAsync(request, _ct));
// 
//             // Assert
//             Assert.Equal("Paper not found.", ex.Message);
//             _repoMock.Verify(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID03 - Old question not in paper -> throw ArgumentException")]
//         public async Task SwapPaperQuestionAsync_UTCID03_OldQuestionNotInPaper_ShouldThrowArgumentException()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(1, 999, 200, false);
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.SwapPaperQuestionAsync(request, _ct));
// 
//             // Assert
//             Assert.Equal("Old question not found in this paper.", ex.Message);
//             _repoMock.Verify(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID04 - New question not found -> throw KeyNotFoundException")]
//         public async Task SwapPaperQuestionAsync_UTCID04_NewQuestionNotFound_ShouldThrowKeyNotFoundException()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(1, 100, 999, false);
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId, _ct))
//                      .ReturnsAsync((Question?)null);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.SwapPaperQuestionAsync(request, _ct));
// 
//             // Assert
//             Assert.Equal("New question not found.", ex.Message);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID05 - New question inactive -> throw ArgumentException")]
//         public async Task SwapPaperQuestionAsync_UTCID05_NewQuestionInactive_ShouldThrowArgumentException()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(1, 100, 201, false);
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
//             var newQuestion = CreateQuestion(questionId: 201, chapterId: 5, difficulty: 2, status: "Draft");
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId, _ct))
//                      .ReturnsAsync(newQuestion);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.SwapPaperQuestionAsync(request, _ct));
// 
//             // Assert
//             Assert.Equal("New question is inactive.", ex.Message);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID06 - Difficulty mismatch -> throw ArgumentException")]
//         public async Task SwapPaperQuestionAsync_UTCID06_DifficultyMismatch_ShouldThrowArgumentException()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(1, 100, 202, false);
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
//             var newQuestion = CreateQuestion(questionId: 202, chapterId: 5, difficulty: 3, status: "Active");
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId, _ct))
//                      .ReturnsAsync(newQuestion);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.SwapPaperQuestionAsync(request, _ct));
// 
//             // Assert
//             Assert.Equal("Difficulty mismatch.", ex.Message);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID07 - Chapter mismatch -> throw ArgumentException")]
//         public async Task SwapPaperQuestionAsync_UTCID07_ChapterMismatch_ShouldThrowArgumentException()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(1, 100, 203, false);
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
//             var newQuestion = CreateQuestion(questionId: 203, chapterId: 6, difficulty: 2, status: "Active");
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId, _ct))
//                      .ReturnsAsync(newQuestion);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.SwapPaperQuestionAsync(request, _ct));
// 
//             // Assert
//             Assert.Equal("Chapter mismatch.", ex.Message);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID08 - SwapGlobal true -> swap globally successfully")]
//         public async Task SwapPaperQuestionAsync_UTCID08_SwapGlobalTrue_ShouldSwapGlobally()
//         {
//             // Arrange
//             var request = new Backend.DTOs.SwapQuestionRequestDto(
//                 PaperId: 1,
//                 OldQuestionId: 100,
//                 NewQuestionId: 200,
//                 SwapGlobal: true);
// 
//             var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
//             var newQuestion = CreateQuestion(questionId: 200, chapterId: 5, difficulty: 2, status: "Inprogress");
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId, _ct))
//                      .ReturnsAsync(newQuestion);
//             _repoMock.Setup(r => r.SwapExamQuestionGloballyAsync(paper.ExamId, request.OldQuestionId, request.NewQuestionId, _ct))
//                      .Returns(Task.CompletedTask);
// 
//             // Act
//             await _service.SwapPaperQuestionAsync(request, _ct);
// 
//             // Assert
//             _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(paper.ExamId, request.OldQuestionId, request.NewQuestionId, _ct), Times.Once);
//             _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.VerifyAll();
//         }
// 
//         private static Paper CreatePaperWithOldQuestion(int paperId, int examId, int oldQuestionId, int chapterId, int difficulty)
//         {
//             return new Paper
//             {
//                 PaperId = paperId,
//                 ExamId = examId,
//                 Code = 1,
//                 Exam = new Exam
//                 {
//                     ExamId = examId,
//                     TeacherId = 1,
//                     SubjectId = 5,
//                     Title = "Exam",
//                     Duration = 60,
//                     ShowScore = 1,
//                     ShowAnswer = 1,
//                     MaxAttempts = 1,
//                     AnswerTimingMode = 0,
//                     Status = 0,
//                     UpdatedAtUtc = DateTime.UtcNow,
//                     ConcurrencyStamp = Array.Empty<byte>()
//                 },
//                 Questions = new List<Question>
//                 {
//                     CreateQuestion(oldQuestionId, chapterId, difficulty, "Active"),
//                     CreateQuestion(oldQuestionId + 1, chapterId, difficulty, "Active")
//                 }
//             };
//         }
// 
//         private static Question CreateQuestion(int questionId, int chapterId, int difficulty, string status)
//         {
//             return new Question
//             {
//                 QuestionId = questionId,
//                 CreatedByUserId = 1,
//                 QuestionType = "MCQ",
//                 QuestionContent = $"Q{questionId}",
//                 ChapterId = chapterId,
//                 Difficulty = difficulty,
//                 UpdatedAtUtc = DateTime.UtcNow,
//                 Status = status,
//                 ConcurrencyStamp = Array.Empty<byte>(),
//                 CreatedByUser = new User
//                 {
//                     UserId = 1,
//                     Email = "teacher@example.com",
//                     ConcurrencyStamp = Array.Empty<byte>()
//                 },
//                 Chapter = new Chapter
//                 {
//                     ChapterId = chapterId,
//                     SubjectId = 5,
//                     Name = $"Chương {chapterId}"
//                 }
//             };
//         }
//     }
// }
// 
