// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Backend.DTOs;
// using Backend.Repositories.Interfaces;
// using Backend.Services.Implements;
// using Moq;
// using Xunit;
// 
// namespace Backend_UnitTest.AssignExamTests
// {
//     public class GetQuestionsAsync_UTCID_Tests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public GetQuestionsAsync_UTCID_Tests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         // UTCID01 – teacherId=1, subjectCode="MAE", chapterId=10, difficulty=2 -> list có d? li?u
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID01 - TeacherId=1, subject='MAE', chapter=10, diff=2 -> return list")]
//         public async Task UTCID01_TeacherActive_SubjectChapterDiffValid_ShouldReturnList()
//         {
//             // Arrange
//             int teacherId = 1;
//             string subjectCode = "MAE";
//             int chapterId = 10;
//             int difficulty = 2;
// 
//             var expected = new List<QuestionListItemDto>
//             {
//                 new QuestionListItemDto(1, "MCQ", "Q1", "MAE", 10, "Chapter 10", 2)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetQuestionsAsync(
//                     teacherId,
//                     "MAE",
//                     chapterId,
//                     difficulty,
//                     It.IsAny<string[]>(),
//                     _ct))
//                      .ReturnsAsync(expected);
// 
//             // Act
//             var result = await _service.GetQuestionsAsync(teacherId, subjectCode, chapterId, difficulty, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Single(result);
//             Assert.Equal(expected[0].QuestionId, result[0].QuestionId);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID02 – teacherId=1, subjectCode=null, chapterId=10, difficulty=2 -> list có d? li?u
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID02 - TeacherId=1, subject=null, chapter=10, diff=2 -> return list")]
//         public async Task UTCID02_TeacherActive_SubjectNull_ShouldReturnList()
//         {
//             // Arrange
//             int teacherId = 1;
//             int chapterId = 10;
//             int difficulty = 2;
// 
//             var expected = new List<QuestionListItemDto>
//             {
//                 new QuestionListItemDto(2, "MCQ", "Q2", "MAE", 10, "Chapter 10", 2)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetQuestionsAsync(
//                     teacherId,
//                     null,
//                     chapterId,
//                     difficulty,
//                     It.IsAny<string[]>(),
//                     _ct))
//                      .ReturnsAsync(expected);
// 
//             // Act
//             var result = await _service.GetQuestionsAsync(teacherId, null, chapterId, difficulty, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Single(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID03 – teacherId=1, subjectCode=" MAE" (có space), chapterId=10, difficulty=2 -> Trim, list có d? li?u
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID03 - TeacherId=1, subject=' MAE', chapter=10, diff=2 -> trim & return list")]
//         public async Task UTCID03_TeacherActive_SubjectWhitespace_ShouldTrimAndReturnList()
//         {
//             // Arrange
//             int teacherId = 1;
//             string subjectCode = " MAE";
//             int chapterId = 10;
//             int difficulty = 2;
// 
//             var expected = new List<QuestionListItemDto>
//             {
//                 new QuestionListItemDto(3, "MCQ", "Q3", "MAE", 10, "Chapter 10", 2)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             // Sau Trim: "MAE"
//             _repoMock.Setup(r => r.GetQuestionsAsync(
//                     teacherId,
//                     "MAE",
//                     chapterId,
//                     difficulty,
//                     It.IsAny<string[]>(),
//                     _ct))
//                      .ReturnsAsync(expected);
// 
//             // Act
//             var result = await _service.GetQuestionsAsync(teacherId, subjectCode, chapterId, difficulty, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Single(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID04 – teacherId=null, subjectCode="MAE", chapterId=10, difficulty=2 -> ArgumentException
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID04 - TeacherId=null -> ArgumentException")]
//         public async Task UTCID04_TeacherIdNull_ShouldThrowArgumentException()
//         {
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.GetQuestionsAsync(null, "MAE", 10, 2, _ct));
// 
//             // Assert
//             Assert.Equal("TeacherId is required.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.GetQuestionsAsync(
//                 It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(),
//                 It.IsAny<int?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
//                 Times.Never);
//         }
// 
//         // UTCID05 – teacherId=0, subjectCode="MAE", chapterId=10, difficulty=2 -> ArgumentException
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID05 - TeacherId=0 -> ArgumentException")]
//         public async Task UTCID05_TeacherIdZero_ShouldThrowArgumentException()
//         {
//             // Act
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.GetQuestionsAsync(0, "MAE", 10, 2, _ct));
// 
//             // Assert
//             Assert.Equal("TeacherId is required.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.GetQuestionsAsync(
//                 It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(),
//                 It.IsAny<int?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
//                 Times.Never);
//         }
// 
//         // UTCID06 – teacherId=999 (không t?n t?i/inactive), subjectCode="MAE", chapterId=10, difficulty=2 -> KeyNotFoundException
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID06 - TeacherId=999 inactive/not found -> KeyNotFoundException")]
//         public async Task UTCID06_TeacherInactive_ShouldThrowKeyNotFoundException()
//         {
//             // Arrange
//             int teacherId = 999;
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(false);
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.GetQuestionsAsync(teacherId, "MAE", 10, 2, _ct));
// 
//             // Assert
//             Assert.Equal($"User with Id {teacherId} not found or is inactive.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(teacherId, _ct), Times.Once);
//             _repoMock.Verify(r => r.GetQuestionsAsync(
//                 It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(),
//                 It.IsAny<int?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
//                 Times.Never);
//         }
// 
//         // UTCID07 – teacherId=1, subjectCode="MAE", chapterId=null, difficulty=null -> list có d? li?u
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID07 - TeacherId=1, subject='MAE', chapter=null, diff=null -> return list")]
//         public async Task UTCID07_TeacherActive_ChapterAndDiffNull_ShouldReturnList()
//         {
//             // Arrange
//             int teacherId = 1;
//             string subjectCode = "MAE";
// 
//             var expected = new List<QuestionListItemDto>
//             {
//                 new QuestionListItemDto(7, "MCQ", "Q7", "MAE", 0, "All chapters", 0)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetQuestionsAsync(
//                     teacherId,
//                     "MAE",
//                     null,
//                     null,
//                     It.IsAny<string[]>(),
//                     _ct))
//                      .ReturnsAsync(expected);
// 
//             // Act
//             var result = await _service.GetQuestionsAsync(teacherId, subjectCode, null, null, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Single(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID08 – teacherId=1, subjectCode="MAE", chapterId=10, difficulty=3 (không có data) -> danh sách r?ng
//         [Fact(DisplayName = "GetQuestionsAsync - UTCID08 - TeacherId=1, subject='MAE', chapter=10, diff=3 (no data) -> empty list")]
//         public async Task UTCID08_TeacherActive_NoMatchingQuestion_ShouldReturnEmptyList()
//         {
//             // Arrange
//             int teacherId = 1;
//             string subjectCode = "MAE";
//             int chapterId = 10;
//             int difficulty = 3; // không có data
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetQuestionsAsync(
//                     teacherId,
//                     "MAE",
//                     chapterId,
//                     difficulty,
//                     It.IsAny<string[]>(),
//                     _ct))
//                      .ReturnsAsync(new List<QuestionListItemDto>());
// 
//             // Act
//             var result = await _service.GetQuestionsAsync(teacherId, subjectCode, chapterId, difficulty, _ct);
// 
//             // Assert
//             Assert.NotNull(result);
//             Assert.Empty(result);
//             _repoMock.VerifyAll();
//         }
//     }
// }
