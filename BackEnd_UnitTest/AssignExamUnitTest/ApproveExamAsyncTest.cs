// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Backend.Constants;
// using Backend.Repositories.Interfaces;
// using Backend.Services.Implements;
// using Moq;
// using Xunit;
// 
// namespace BackEnd_UnitTest.AssignExamUnitTest
// {
//     public class ApproveExamAsync_UTCID_Tests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public ApproveExamAsync_UTCID_Tests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         [Fact(DisplayName = "ApproveExamAsync - UTCID01 - Valid examId -> update status to Published")]
//         public async Task ApproveExamAsync_UTCID01_ValidExamId_ShouldUpdateStatusToPublished()
//         {
//             // Arrange
//             int examId = 1;
// 
//             _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
//                      .Returns(Task.CompletedTask);
// 
//             // Act
//             await _service.ApproveExamAsync(examId, _ct);
// 
//             // Assert
//             _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "ApproveExamAsync - UTCID02 - Exam not found -> propagate KeyNotFoundException")]
//         public async Task ApproveExamAsync_UTCID02_ExamNotFound_ShouldPropagateKeyNotFoundException()
//         {
//             // Arrange
//             int examId = 999;
// 
//             _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
//                      .ThrowsAsync(new KeyNotFoundException("Exam not found."));
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.ApproveExamAsync(examId, _ct));
// 
//             // Assert
//             Assert.Equal("Exam not found.", ex.Message);
//             _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "ApproveExamAsync - UTCID03 - ExamId = 0 -> still forward to repository")]
//         public async Task ApproveExamAsync_UTCID03_ExamIdZero_ShouldStillForwardToRepository()
//         {
//             // Arrange
//             int examId = 0;
// 
//             _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
//                      .Returns(Task.CompletedTask);
// 
//             // Act
//             await _service.ApproveExamAsync(examId, _ct);
// 
//             // Assert
//             _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
// 
//         [Fact(DisplayName = "ApproveExamAsync - UTCID04 - Repository throws exception -> propagate exception")]
//         public async Task ApproveExamAsync_UTCID04_RepositoryThrowsException_ShouldPropagateException()
//         {
//             // Arrange
//             int examId = 2;
// 
//             _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
//                      .ThrowsAsync(new Exception("Database error"));
// 
//             // Act
//             var ex = await Assert.ThrowsAsync<Exception>(() =>
//                 _service.ApproveExamAsync(examId, _ct));
// 
//             // Assert
//             Assert.Equal("Database error", ex.Message);
//             _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
//             _repoMock.VerifyAll();
//         }
//     }
// }
