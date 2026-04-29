// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Backend.DTOs;
// using Backend.Models;
// using Backend.Repositories.Interfaces;
// using Backend.Services.Implements;
// using Moq;
// using Xunit;
// 
// namespace Backend_UnitTest.AssignExamTests
// {
//     public class GetAlternativeQuestionsAsync_UTCID_Tests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public GetAlternativeQuestionsAsync_UTCID_Tests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         // UTCID01 – Paper và question hợp lệ, có câu hỏi thay thế -> trả list có dữ liệu
//         [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID01 - Paper và question hợp lệ -> trả danh sách thay thế có dữ liệu")]
//         public async Task GetAlternativeQuestionsAsync_UTCID01_ValidPaperAndQuestion_ShouldReturnAlternativeList()
//         {
//             // Arrange
//             int paperId = 1;
//             int questionId = 100;
// 
//             var paper = CreatePaperWithQuestions(paperId, 5, 100, 101);
//             var oldQuestion = CreateQuestion(questionId, chapterId: 10, difficulty: 2);
//             var expected = new List<QuestionListItemDto>
//             {
//                 new QuestionListItemDto(200, "MCQ", "Alternative 1", "MAE", 10, "Chương 1", 2),
//                 new QuestionListItemDto(201, "MCQ", "Alternative 2", "MAE", 10, "Chương 1", 2)
//             };
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(paperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(questionId, _ct))
//                      .ReturnsAsync(oldQuestion);
//             _repoMock.Setup(r => r.GetAlternativeQuestionsAsync(
//                     5,
//                     10,
//                     2,
//                     It.IsAny<string[]>(),
//                     It.Is<List<int>>(ids => ids.SequenceEqual(new List<int> { 100, 101 })),
//                     _ct))
//                      .ReturnsAsync(expected);
// 
//             // Act
//             var result = await _service.GetAlternativeQuestionsAsync(paperId, questionId, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(2, result.Count);
//             Assert.Equal(200, result[0].QuestionId);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID02 – Paper không tồn tại -> KeyNotFoundException
//         [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID02 - Paper không tồn tại -> KeyNotFoundException")]
//         public async Task GetAlternativeQuestionsAsync_UTCID02_PaperNotFound_ShouldThrowKeyNotFoundException()
//         {
//             // Arrange
//             int paperId = 999;
//             int questionId = 100;
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(paperId, _ct))
//                      .ReturnsAsync((Paper?)null);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.GetAlternativeQuestionsAsync(paperId, questionId, _ct));
// 
//             // Assert
//             Assert.Equal("Paper not found.", ex.Message);
//             _repoMock.Verify(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.GetAlternativeQuestionsAsync(
//                 It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
//                 It.IsAny<string[]>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID03 – Question cũ không tồn tại -> KeyNotFoundException
//         [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID03 - Question cần thay thế không tồn tại -> KeyNotFoundException")]
//         public async Task GetAlternativeQuestionsAsync_UTCID03_QuestionNotFound_ShouldThrowKeyNotFoundException()
//         {
//             // Arrange
//             int paperId = 1;
//             int questionId = 999;
// 
//             var paper = CreatePaperWithQuestions(paperId, 5, 100, 101);
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(paperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(questionId, _ct))
//                      .ReturnsAsync((Question?)null);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.GetAlternativeQuestionsAsync(paperId, questionId, _ct));
// 
//             // Assert
//             Assert.Equal("Question not found.", ex.Message);
//             _repoMock.Verify(r => r.GetAlternativeQuestionsAsync(
//                 It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
//                 It.IsAny<string[]>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID04 – Kiểm tra service truyền đúng subject/chapter/difficulty/excludeIds
//         [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID04 - Truyền đúng subject, chapter, difficulty và excludeIds")]
//         public async Task GetAlternativeQuestionsAsync_UTCID04_ShouldPassCorrectFilterArguments()
//         {
//             // Arrange
//             int paperId = 2;
//             int questionId = 150;
// 
//             var paper = CreatePaperWithQuestions(paperId, 7, 150, 151, 152);
//             var oldQuestion = CreateQuestion(questionId, chapterId: 20, difficulty: 3);
//             var expected = new List<QuestionListItemDto>
//             {
//                 new QuestionListItemDto(300, "MCQ", "Alternative", "PHY", 20, "Chương 2", 3)
//             };
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(paperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(questionId, _ct))
//                      .ReturnsAsync(oldQuestion);
//             _repoMock.Setup(r => r.GetAlternativeQuestionsAsync(
//                     7,
//                     20,
//                     3,
//                     It.Is<string[]>(statuses => statuses.Length == 2 && statuses.Contains("Active") && statuses.Contains("Inprogress")),
//                     It.Is<List<int>>(ids => ids.SequenceEqual(new List<int> { 150, 151, 152 })),
//                     _ct))
//                      .ReturnsAsync(expected);
// 
//             // Act
//             var result = await _service.GetAlternativeQuestionsAsync(paperId, questionId, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Single(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID05 – Không có câu hỏi thay thế phù hợp -> danh sách rỗng
//         [Fact(DisplayName = "GetAlternativeQuestionsAsync - UTCID05 - Không có câu hỏi thay thế phù hợp -> danh sách rỗng")]
//         public async Task GetAlternativeQuestionsAsync_UTCID05_NoAlternativeQuestion_ShouldReturnEmptyList()
//         {
//             // Arrange
//             int paperId = 3;
//             int questionId = 500;
// 
//             var paper = CreatePaperWithQuestions(paperId, 9, 500, 501);
//             var oldQuestion = CreateQuestion(questionId, chapterId: 30, difficulty: 4);
// 
//             _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(paperId, _ct))
//                      .ReturnsAsync(paper);
//             _repoMock.Setup(r => r.GetQuestionByIdAsync(questionId, _ct))
//                      .ReturnsAsync(oldQuestion);
//             _repoMock.Setup(r => r.GetAlternativeQuestionsAsync(
//                     9,
//                     30,
//                     4,
//                     It.IsAny<string[]>(),
//                     It.Is<List<int>>(ids => ids.SequenceEqual(new List<int> { 500, 501 })),
//                     _ct))
//                      .ReturnsAsync(new List<QuestionListItemDto>());
// 
//             // Act
//             var result = await _service.GetAlternativeQuestionsAsync(paperId, questionId, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Empty(result);
//             _repoMock.VerifyAll();
//         }
// 
//         private static Paper CreatePaperWithQuestions(int paperId, int subjectId, params int[] questionIds)
//         {
//             return new Paper
//             {
//                 PaperId = paperId,
//                 ExamId = 1,
//                 Code = 1,
//                 Exam = new Exam
//                 {
//                     ExamId = 1,
//                     SubjectId = subjectId,
//                     Title = "Exam",
//                     TeacherId = 1,
//                     Duration = 60,
//                     ShowScore = 1,
//                     ShowAnswer = 1,
//                     MaxAttempts = 1,
//                     AnswerTimingMode = 0,
//                     Status = 0,
//                     UpdatedAtUtc = DateTime.UtcNow,
//                     ConcurrencyStamp = Array.Empty<byte>()
//                 },
//                 Questions = questionIds.Select(id => new Question
//                 {
//                     QuestionId = id,
//                     CreatedByUserId = 1,
//                     QuestionType = "MCQ",
//                     QuestionContent = $"Q{id}",
//                     ChapterId = 1,
//                     Difficulty = 1,
//                     UpdatedAtUtc = DateTime.UtcNow,
//                     Status = "Active",
//                     ConcurrencyStamp = Array.Empty<byte>(),
//                     CreatedByUser = new User
//                     {
//                         UserId = 1,
//                         Email = "teacher@example.com",
//                         ConcurrencyStamp = Array.Empty<byte>()
//                     },
//                     Chapter = new Chapter
//                     {
//                         ChapterId = 1,
//                         SubjectId = subjectId,
//                         Name = "Chapter"
//                     }
//                 }).ToList()
//             };
//         }
// 
//         private static Question CreateQuestion(int questionId, int chapterId, int difficulty)
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
//                 Status = "Active",
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
//                     SubjectId = 1,
//                     Name = $"Chương {chapterId}"
//                 }
//             };
//         }
//     }
// }
// 
